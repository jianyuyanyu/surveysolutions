using System.Diagnostics;

#nullable enable
namespace WB.Infrastructure.Native.Workspaces
{
    [DebuggerDisplay("{Name} - {DisplayName}")]
    public class WorkspaceContext
    {
        public WorkspaceContext(string name, string displayName)
        {
            Name = name;
            DisplayName = displayName;
        }

        public string Name { get; }
        public string DisplayName { get; }
        public string? PathBase { get; set; }

        public string SchemaName => $"ws_{Name}";
    }
}