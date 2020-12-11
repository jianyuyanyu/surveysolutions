#nullable enable
using System.Collections.Generic;
using WB.Infrastructure.Native.Workspaces;

namespace WB.Core.BoundedContexts.Headquarters.Workspaces
{
    public class Workspace
    {
        protected Workspace()
        {
            Name = string.Empty;
            DisplayName = string.Empty;
        }

        public Workspace(string name, string displayName)
        {
            Name = name;
            DisplayName = displayName;
            
        }

        public virtual string Name { get; set; }
        
        public virtual string DisplayName { get; set; }
        
        public static Workspace Default { get; set; } = new Workspace("primary", "Default Space");
        public virtual ISet<WorkspacesUsers> Users { get; set; } = new HashSet<WorkspacesUsers>();
        
        protected bool Equals(Workspace other)
        {
            return Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Workspace) obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        
        public virtual WorkspaceContext AsContext() => new WorkspaceContext(Name, DisplayName);
    }
}