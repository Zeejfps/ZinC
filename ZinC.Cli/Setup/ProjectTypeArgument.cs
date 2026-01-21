using System.CommandLine;

namespace ZinC.Cli.Setup;

internal sealed class ProjectTypeArgument : Argument<ProjectType>
{
    public ProjectTypeArgument(string name) : base(name)
    {
        var projectTypes = Enum.GetValues<ProjectType>();
        var projectTypeNames = projectTypes.Select(ProjectTypeFormatter.ToShortNameString);
        this.AcceptOnlyFromAmong(projectTypeNames.ToArray());
        CustomParser = result => ProjectTypeFormatter.FromShortNameString(result.Tokens[0].Value);
    }
}