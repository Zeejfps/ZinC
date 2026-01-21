using System.Diagnostics;
using ZinC.Cli.Config;
using ZinC.Cli.Console;

namespace ZinC.Cli.Build;

internal sealed class BuildService
{
    private readonly IConsole _console;

    public BuildService(IConsole console)
    {
        _console = console;
    }

    public async Task<BuildResult> BuildAsync(
        ToolchainConfig config,
        ModeConfig modeConfig,
        PlatformConfig platformConfig,
        ArtifactTypeConfig artifactTypeConfig,
        string? workingDir = null,
        CancellationToken cancellationToken = default)
    {
        workingDir ??= Directory.GetCurrentDirectory();

        var srcDir = Path.Combine(workingDir, config.SrcDir);
        var outDir = Path.Combine(workingDir, config.OutDir);
        var objDir = Path.Combine(outDir, "obj");

        Directory.CreateDirectory(outDir);
        Directory.CreateDirectory(objDir);

        var sourceFiles = Directory.GetFiles(srcDir, "*.c", SearchOption.AllDirectories);
        if (sourceFiles.Length == 0)
        {
            _console.WriteErrorLine($"No source files found in: {srcDir}");
            return new BuildResult(false, null, 1);
        }

        _console.WriteLine($"Found {sourceFiles.Length} source file(s)");

        // Collect flags
        var compileFlags = CollectCompileFlags(config, modeConfig, platformConfig, artifactTypeConfig);
        var includeFlags = config.IncludeDirs.Select(dir => $"-I{Path.Combine(workingDir, dir)}");
        var defineFlags = CollectDefines(config, modeConfig, platformConfig).Select(d => $"-D{d}");

        var allCompileFlags = compileFlags
            .Concat(includeFlags)
            .Concat(defineFlags)
            .ToList();

        // Compile phase
        var objectFiles = new List<string>();
        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(srcDir, sourceFile);
            var objectFileName = Path.ChangeExtension(relativePath, platformConfig.ObjectExtension)
                .Replace(Path.DirectorySeparatorChar, '_');
            var objectFile = Path.Combine(objDir, objectFileName);
            objectFiles.Add(objectFile);

            _console.WriteLine($"  Compiling: {relativePath}");

            var compileArgs = new List<string> { config.CompileFlag, sourceFile };
            compileArgs.AddRange(FormatToArgs(config.CompileOutputFormat, objectFile));
            compileArgs.AddRange(allCompileFlags);

            var compileResult = await RunProcessAsync(config.CompilerExe, compileArgs, workingDir, cancellationToken);
            if (compileResult != 0)
            {
                _console.WriteErrorLine($"Compilation failed for: {relativePath}");
                return new BuildResult(false, null, compileResult);
            }
        }

        // Link/Archive phase
        if (artifactTypeConfig.UseArchiver)
        {
            return await ArchiveAsync(config, platformConfig, artifactTypeConfig, objectFiles, outDir, workingDir, cancellationToken);
        }

        return await LinkAsync(config, modeConfig, platformConfig, artifactTypeConfig, objectFiles, outDir, workingDir, cancellationToken);
    }

    private async Task<BuildResult> LinkAsync(
        ToolchainConfig config,
        ModeConfig modeConfig,
        PlatformConfig platformConfig,
        ArtifactTypeConfig artifactTypeConfig,
        List<string> objectFiles,
        string outDir,
        string workingDir,
        CancellationToken cancellationToken)
    {
        var extension = config.ArtifactType switch
        {
            "shared_library" => platformConfig.SharedLibExtension,
            _ => platformConfig.ArtifactExtension
        };
        var artifactPath = Path.Combine(outDir, config.ArtifactName + extension);

        _console.WriteLine($"  Linking: {Path.GetFileName(artifactPath)}");

        var linkFlags = CollectLinkFlags(config, modeConfig, platformConfig, artifactTypeConfig);
        var libDirs = platformConfig.LibDirs ?? [];
        var libDirFlags = libDirs.Select(dir => string.Format(config.LibDirFormat, Path.Combine(workingDir, dir)));
        var libFlags = platformConfig.Libs.Select(lib => string.Format(config.LibFormat, lib));

        var linkArgs = new List<string>();
        linkArgs.AddRange(objectFiles);
        linkArgs.AddRange(FormatToArgs(config.LinkOutputFormat, artifactPath));
        linkArgs.AddRange(linkFlags);
        linkArgs.AddRange(libDirFlags);
        linkArgs.AddRange(libFlags);

        var linkResult = await RunProcessAsync(config.CompilerExe, linkArgs, workingDir, cancellationToken);
        if (linkResult != 0)
        {
            _console.WriteErrorLine("Linking failed");
            return new BuildResult(false, null, linkResult);
        }

        _console.WriteLine($"Built: {artifactPath}");
        return new BuildResult(true, artifactPath, 0);
    }

    private async Task<BuildResult> ArchiveAsync(
        ToolchainConfig config,
        PlatformConfig platformConfig,
        ArtifactTypeConfig artifactTypeConfig,
        List<string> objectFiles,
        string outDir,
        string workingDir,
        CancellationToken cancellationToken)
    {
        var artifactPath = Path.Combine(outDir, "lib" + config.ArtifactName + platformConfig.StaticLibExtension);

        _console.WriteLine($"  Archiving: {Path.GetFileName(artifactPath)}");

        var archiveArgs = new List<string>();
        if (artifactTypeConfig.ArchiverFlags is not null)
            archiveArgs.AddRange(artifactTypeConfig.ArchiverFlags);
        archiveArgs.Add(artifactPath);
        archiveArgs.AddRange(objectFiles);

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
        ToolchainConfig config,
        ModeConfig modeConfig,
        PlatformConfig platformConfig,
        ArtifactTypeConfig artifactTypeConfig)
    {
        return Collect(
            config.Flags,
            modeConfig.Flags,
            platformConfig.Flags,
            artifactTypeConfig.Flags);
    }

    private static List<string> CollectDefines(
        ToolchainConfig config,
        ModeConfig modeConfig,
        PlatformConfig platformConfig)
    {
        return Collect(
            config.Defines,
            modeConfig.Defines,
            platformConfig.Defines);
    }

    private static List<string> CollectLinkFlags(
        ToolchainConfig config,
        ModeConfig modeConfig,
        PlatformConfig platformConfig,
        ArtifactTypeConfig artifactTypeConfig)
    {
        return Collect(
            config.LinkFlags,
            modeConfig.LinkFlags,
            platformConfig.LinkFlags,
            artifactTypeConfig.LinkFlags);
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
