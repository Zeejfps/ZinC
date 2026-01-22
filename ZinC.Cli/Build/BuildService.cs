using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.FileSystemGlobbing;
using ZinC.Cli.Config;
using ZinC.Cli.Console;
using ZinC.Cli.Toolchains;

namespace ZinC.Cli.Build;

internal readonly struct CompilationUnit
{
    public required string SourceFile { get; init; }
    public required string ObjectFile { get; init; }
    public required string RelativePath { get; init; }
}

internal sealed class BuildService
{
    private readonly IConsole _console;

    public BuildService(IConsole console)
    {
        _console = console;
    }

    public async Task<BuildResult> BuildAsync(
        BuildContext context,
        bool generateCompileCommands = false,
        bool verbose = false,
        string? workingDir = null,
        CancellationToken cancellationToken = default)
    {
        workingDir ??= Directory.GetCurrentDirectory();

        var project = context.ProjectConfig;
        var projectPlatform = context.ProjectPlatformConfig;
        var toolchain = context.ToolchainConfig;
        var mode = context.ModeConfig;
        var platform = context.PlatformConfig;
        var artifactType = context.ArtifactTypeConfig;

        var srcDir = Path.Combine(workingDir, project.SrcDir ?? "src");
        var outDir = Path.Combine(workingDir, project.OutDir ?? "out");
        var objDir = Path.Combine(outDir, "obj");

        Directory.CreateDirectory(outDir);
        Directory.CreateDirectory(objDir);

        var sourceExtensions = toolchain.SourceExtensions ?? [".c"];
        var sourceFiles = sourceExtensions
            .SelectMany(ext => Directory.GetFiles(srcDir, $"*{ext}", SearchOption.AllDirectories))
            .Distinct()
            .ToArray();

        // Apply platform-specific source filtering if specified
        if (projectPlatform?.Sources is { Count: > 0 })
        {
            sourceFiles = FilterSourcesByPatterns(sourceFiles, srcDir, projectPlatform.Sources);
        }

        if (sourceFiles.Length == 0)
        {
            _console.WriteErrorLine($"No source files found in: {srcDir}");
            return new BuildResult(false, null, 1);
        }

        _console.WriteLine($"Found {sourceFiles.Length} source file(s)");

        // Collect flags from toolchain
        var compileFlags = CollectCompileFlags(toolchain, mode, platform, artifactType);

        // Collect include dirs from project (base + platform-specific)
        var includeDirs = Collect(project.IncludeDirs, projectPlatform?.IncludeDirs);
        var includeFlags = includeDirs.Select(dir => $"-I{Path.Combine(workingDir, dir)}");

        // Collect defines from both toolchain and project
        var defines = Collect(
            toolchain.Defines,
            mode.Defines,
            platform.Defines,
            project.Defines,
            projectPlatform?.Defines);
        var defineFlags = defines.Select(d => $"-D{d}");

        var allCompileFlags = compileFlags
            .Concat(includeFlags)
            .Concat(defineFlags)
            .ToList();

        var compilerExe = toolchain.CompilerExe!;
        var compileFlag = toolchain.CompileFlag!;
        var compileOutputFormat = toolchain.CompileOutputFormat!;
        var objectExtension = platform.ObjectExtension ?? ".o";

        var objectFiles = new List<string>();
        var compilationUnits = new List<CompilationUnit>();

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(srcDir, sourceFile);
            var objectFileName = Path.ChangeExtension(relativePath, objectExtension)
                .Replace(Path.DirectorySeparatorChar, '_');
            var objectFile = Path.Combine(objDir, objectFileName);
            compilationUnits.Add(new CompilationUnit
            {
                SourceFile = sourceFile,
                ObjectFile = objectFile,
                RelativePath = relativePath
            });
            objectFiles.Add(objectFile);
        }

        // Generate compile_commands.json
        if (generateCompileCommands)
        {
            var compileCommands = new List<CompileCommand>();
            foreach (var unit in compilationUnits)
            {
                var args = new List<string> { compilerExe, compileFlag, unit.SourceFile };
                args.AddRange(FormatToArgs(compileOutputFormat, unit.ObjectFile));
                args.AddRange(allCompileFlags);

                compileCommands.Add(new CompileCommand
                {
                    Directory = workingDir,
                    Arguments = args,
                    File = unit.SourceFile
                });
            }

            var compileCommandsPath = Path.Combine(workingDir, "compile_commands.json");
            await using var stream = File.Create(compileCommandsPath);
            await JsonSerializer.SerializeAsync(stream, compileCommands, CompileCommandJsonContext.Default.ListCompileCommand, cancellationToken);
        }

        // Compile phase (parallel)
        var failedFile = new ConcurrentBag<string>();
        var firstError = 0;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cts.Token
        };

        try
        {
            await Parallel.ForEachAsync(compilationUnits, parallelOptions, async (unit, ct) =>
            {
                _console.WriteLine($"  Compiling: {unit.RelativePath}");

                var compileArgs = new List<string> { compileFlag, unit.SourceFile };
                compileArgs.AddRange(FormatToArgs(compileOutputFormat, unit.ObjectFile));
                compileArgs.AddRange(allCompileFlags);

                if (verbose)
                {
                    _console.WriteLine($"    {compilerExe} {string.Join(" ", compileArgs)}");
                }

                var compileResult = await RunProcessAsync(compilerExe, compileArgs, workingDir, ct);
                if (compileResult != 0)
                {
                    failedFile.Add(unit.RelativePath);
                    Interlocked.CompareExchange(ref firstError, compileResult, 0);
                    await cts.CancelAsync();
                }
            });
        }
        catch (OperationCanceledException) when (!failedFile.IsEmpty)
        {
            // Cancelled due to compilation failure
        }

        if (!failedFile.IsEmpty)
        {
            foreach (var file in failedFile)
            {
                _console.WriteErrorLine($"Compilation failed for: {file}");
            }
            return new BuildResult(false, null, firstError);
        }

        // Link/Archive phase
        if (artifactType.UseArchiver)
        {
            return await ArchiveAsync(context, objectFiles, outDir, workingDir, verbose, cancellationToken);
        }

        return await LinkAsync(context, objectFiles, outDir, workingDir, verbose, cancellationToken);
    }

    private async Task<BuildResult> LinkAsync(
        BuildContext context,
        List<string> objectFiles,
        string outDir,
        string workingDir,
        bool verbose,
        CancellationToken cancellationToken)
    {
        var project = context.ProjectConfig;
        var projectPlatform = context.ProjectPlatformConfig;
        var toolchain = context.ToolchainConfig;
        var mode = context.ModeConfig;
        var platform = context.PlatformConfig;
        var artifactType = context.ArtifactTypeConfig;

        var artifactTypeStr = project.ArtifactType ?? "executable";
        var extension = artifactTypeStr switch
        {
            "shared_library" => platform.SharedLibExtension ?? ".so",
            _ => platform.ArtifactExtension ?? ""
        };
        var artifactName = project.ArtifactName ?? "a";
        var artifactPath = Path.Combine(outDir, artifactName + extension);

        _console.WriteLine($"  Linking: {Path.GetFileName(artifactPath)}");

        var linkFlags = CollectLinkFlags(toolchain, mode, platform, artifactType);

        // Merge lib_dirs from toolchain platform and project
        var libDirs = Collect(platform.LibDirs, project.LibDirs, projectPlatform?.LibDirs);

        // Merge libs from toolchain platform and project
        var libs = Collect(platform.Libs, project.Libs, projectPlatform?.Libs);

        var libDirFormat = toolchain.LibDirFormat!;
        var libFormat = toolchain.LibFormat!;
        var linkOutputFormat = toolchain.LinkOutputFormat!;
        var compilerExe = toolchain.CompilerExe!;

        var libDirFlags = libDirs.Select(dir => string.Format(libDirFormat, Path.Combine(workingDir, dir)));
        var libFlags = libs.Select(lib => string.Format(libFormat, lib));

        var linkArgs = new List<string>();
        linkArgs.AddRange(objectFiles);
        linkArgs.AddRange(FormatToArgs(linkOutputFormat, artifactPath));
        linkArgs.AddRange(linkFlags);
        linkArgs.AddRange(libDirFlags);
        linkArgs.AddRange(libFlags);

        if (verbose)
        {
            _console.WriteLine($"    {compilerExe} {string.Join(" ", linkArgs)}");
        }

        var linkResult = await RunProcessAsync(compilerExe, linkArgs, workingDir, cancellationToken);
        if (linkResult != 0)
        {
            _console.WriteErrorLine("Linking failed");
            return new BuildResult(false, null, linkResult);
        }

        _console.WriteLine($"Built: {artifactPath}");
        return new BuildResult(true, artifactPath, 0);
    }

    private async Task<BuildResult> ArchiveAsync(
        BuildContext context,
        List<string> objectFiles,
        string outDir,
        string workingDir,
        bool verbose,
        CancellationToken cancellationToken)
    {
        var project = context.ProjectConfig;
        var platform = context.PlatformConfig;
        var artifactType = context.ArtifactTypeConfig;

        var artifactName = project.ArtifactName ?? "a";
        var staticLibExtension = platform.StaticLibExtension ?? ".a";
        var artifactPath = Path.Combine(outDir, "lib" + artifactName + staticLibExtension);

        _console.WriteLine($"  Archiving: {Path.GetFileName(artifactPath)}");

        var archiveArgs = new List<string>();
        if (artifactType.ArchiverFlags is not null)
            archiveArgs.AddRange(artifactType.ArchiverFlags);
        archiveArgs.Add(artifactPath);
        archiveArgs.AddRange(objectFiles);

        if (verbose)
        {
            _console.WriteLine($"    ar {string.Join(" ", archiveArgs)}");
        }

        var archiveResult = await RunProcessAsync("ar", archiveArgs, workingDir, cancellationToken);
        if (archiveResult != 0)
        {
            _console.WriteErrorLine("Archiving failed");
            return new BuildResult(false, null, archiveResult);
        }

        _console.WriteLine($"Built: {artifactPath}");
        return new BuildResult(true, artifactPath, 0);
    }

    private static List<string> CollectCompileFlags(
        ToolchainConfig toolchain,
        ModeConfig mode,
        PlatformConfig platform,
        ArtifactTypeConfig artifactType)
    {
        return Collect(
            toolchain.Flags,
            mode.Flags,
            platform.Flags,
            artifactType.Flags);
    }

    private static List<string> CollectLinkFlags(
        ToolchainConfig toolchain,
        ModeConfig mode,
        PlatformConfig platform,
        ArtifactTypeConfig artifactType)
    {
        return Collect(
            toolchain.LinkFlags,
            mode.LinkFlags,
            platform.LinkFlags,
            artifactType.LinkFlags);
    }

    private static string[] FilterSourcesByPatterns(string[] sourceFiles, string srcDir, List<string> patterns)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        foreach (var pattern in patterns)
        {
            matcher.AddInclude(pattern);
        }

        var relativePaths = sourceFiles
            .Select(file => Path.GetRelativePath(srcDir, file))
            .ToArray();

        var result = matcher.Match(relativePaths);

        return result.Files
            .Select(match => Path.Combine(srcDir, match.Path))
            .ToArray();
    }

    private static List<string> Collect(params IEnumerable<string>?[] sources)
    {
        var seen = new HashSet<string>();
        var result = new List<string>();

        foreach (var source in sources)
        {
            if (source is null) continue;
            foreach (var item in source)
            {
                if (seen.Add(item))
                {
                    result.Add(item);
                }
            }
        }

        return result;
    }

    private static IEnumerable<string> FormatToArgs(string format, string value)
    {
        var formatted = string.Format(format, value);
        return formatted.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private async Task<int> RunProcessAsync(
        string fileName,
        List<string> arguments,
        string workingDir,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) _console.WriteLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) _console.WriteErrorLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
