using System.CommandLine;
using System.Diagnostics;
using ZinC.Cli.Config;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Build;

internal sealed class BuildAction : ZincCommandAction
{
    public required Option<string> ModeOption { get; init; }
    public required Option<string> PlatformOption { get; init; }
    public required Option<string> ConfigOption { get; init; }

    public override Option[] Options => [ModeOption, PlatformOption, ConfigOption];

    public BuildAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var mode = parseResult.GetRequiredValue(ModeOption);
        var platform = parseResult.GetRequiredValue(PlatformOption);
        var configPath = parseResult.GetRequiredValue(ConfigOption);

        if (!configPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            configPath += ".json";
        }

        var configService = new ToolchainConfigService();
        var config = await configService.LoadAsync(configPath, cancellationToken: cancellationToken);
        if (config is null)
        {
            WriteErrorLine($"Config file not found: {configPath}");
            return 1;
        }

        if (!config.Modes.TryGetValue(mode, out var modeConfig))
        {
            WriteErrorLine($"Unknown mode: {mode}. Available: {string.Join(", ", config.Modes.Keys)}");
            return 1;
        }

        if (!config.Platforms.TryGetValue(platform, out var platformConfig))
        {
            WriteErrorLine($"Unknown platform: {platform}. Available: {string.Join(", ", config.Platforms.Keys)}");
            return 1;
        }

        if (!config.ArtifactTypes.TryGetValue(config.ArtifactType, out var artifactTypeConfig))
        {
            WriteErrorLine($"Unknown artifact type: {config.ArtifactType}. Available: {string.Join(", ", config.ArtifactTypes.Keys)}");
            return 1;
        }

        var workingDir = Directory.GetCurrentDirectory();
        var srcDir = Path.Combine(workingDir, config.SrcDir);
        var outDir = Path.Combine(workingDir, config.OutDir);
        var objDir = Path.Combine(outDir, "obj");

        Directory.CreateDirectory(outDir);
        Directory.CreateDirectory(objDir);

        var sourceFiles = Directory.GetFiles(srcDir, "*.c", SearchOption.AllDirectories);
        if (sourceFiles.Length == 0)
        {
            WriteErrorLine($"No source files found in: {srcDir}");
            return 1;
        }

        WriteLine($"Building {config.ArtifactName} ({mode}/{platform})");
        WriteLine($"Found {sourceFiles.Length} source file(s)");

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
            var objectFileName = Path.ChangeExtension(relativePath, platformConfig.ObjectExtension).Replace(Path.DirectorySeparatorChar, '_');
            var objectFile = Path.Combine(objDir, objectFileName);
            objectFiles.Add(objectFile);

            WriteLine($"  Compiling: {relativePath}");

            var compileArgs = new List<string> { "-c", sourceFile, "-o", objectFile };
            compileArgs.AddRange(allCompileFlags);

            var compileResult = await RunProcessAsync(config.CompilerExe, compileArgs, workingDir, cancellationToken);
            if (compileResult != 0)
            {
                WriteErrorLine($"Compilation failed for: {relativePath}");
                return compileResult;
            }
        }

        // Link phase
        if (artifactTypeConfig.UseArchiver)
        {
            return await ArchiveAsync(config, platformConfig, artifactTypeConfig, objectFiles, outDir, workingDir, cancellationToken);
        }

        return await LinkAsync(config, modeConfig, platformConfig, artifactTypeConfig, objectFiles, outDir, workingDir, cancellationToken);
    }

    private async Task<int> LinkAsync(
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

        WriteLine($"  Linking: {Path.GetFileName(artifactPath)}");

        var linkFlags = CollectLinkFlags(config, modeConfig, platformConfig, artifactTypeConfig);
        var libFlags = platformConfig.Libs.Select(lib => $"-l{lib}");

        var linkArgs = new List<string>();
        linkArgs.AddRange(objectFiles);
        linkArgs.Add("-o");
        linkArgs.Add(artifactPath);
        linkArgs.AddRange(linkFlags);
        linkArgs.AddRange(libFlags);

        var linkResult = await RunProcessAsync(config.CompilerExe, linkArgs, workingDir, cancellationToken);
        if (linkResult != 0)
        {
            WriteErrorLine("Linking failed");
            return linkResult;
        }

        WriteLine($"Built: {artifactPath}");
        return 0;
    }

    private async Task<int> ArchiveAsync(
        ToolchainConfig config,
        PlatformConfig platformConfig,
        ArtifactTypeConfig artifactTypeConfig,
        List<string> objectFiles,
        string outDir,
        string workingDir,
        CancellationToken cancellationToken)
    {
        var artifactPath = Path.Combine(outDir, "lib" + config.ArtifactName + platformConfig.StaticLibExtension);

        WriteLine($"  Archiving: {Path.GetFileName(artifactPath)}");

        var archiveArgs = new List<string>();
        archiveArgs.AddRange(artifactTypeConfig.ArchiverFlags);
        archiveArgs.Add(artifactPath);
        archiveArgs.AddRange(objectFiles);

        var archiveResult = await RunProcessAsync("ar", archiveArgs, workingDir, cancellationToken);
        if (archiveResult != 0)
        {
            WriteErrorLine("Archiving failed");
            return archiveResult;
        }

        WriteLine($"Built: {artifactPath}");
        return 0;
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
            artifactTypeConfig.LinkFlags
        );
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

    private async Task<int> RunProcessAsync(string fileName, List<string> arguments, string workingDir, CancellationToken cancellationToken)
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
            if (e.Data is not null) WriteLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) WriteErrorLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}