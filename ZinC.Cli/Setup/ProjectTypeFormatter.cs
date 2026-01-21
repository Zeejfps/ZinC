namespace ZinC.Cli.Setup;

internal static class ProjectTypeFormatter
{
    public static string ToShortNameString(ProjectType projectType)
    {
        return projectType switch
        {
            ProjectType.Executable => "exe",
            ProjectType.StaticLibrary => "lib",
            ProjectType.DynamicLibrary => "dll",
            _ => throw new ArgumentOutOfRangeException(nameof(projectType), projectType, null)
        };
    }
    
    public static ProjectType FromShortNameString(string projectTypeString)
    {
        return projectTypeString switch
        {
            "exe" => ProjectType.Executable,
            "lib" => ProjectType.StaticLibrary,
            "dll" => ProjectType.DynamicLibrary,
            _ => throw new ArgumentException($"Invalid project type: {projectTypeString}")
        };
    }
}