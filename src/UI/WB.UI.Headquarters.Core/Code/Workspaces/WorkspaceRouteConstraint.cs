using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WB.Core.BoundedContexts.Headquarters.Workspaces;
using WB.Core.BoundedContexts.Headquarters.Workspaces.Mappings;
using WB.Infrastructure.Native.Workspaces;

namespace WB.UI.Headquarters.Code.Workspaces
{
    public class WorkspaceRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (!values.ContainsKey("workspace")) return false;

            var workspace = values["workspace"].ToString();
            if (string.Equals(workspace, "graphql",
                StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            if (string.Equals(workspace, WorkspaceConstants.DefaultWorkspaceName, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            IWorkspacesService service = httpContext.RequestServices.GetRequiredService<IWorkspacesService>();
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<WorkspaceRouteConstraint>>();

            bool hasWorkspace = service.IsWorkspaceDefined(workspace);
            logger.LogTrace("Workspace constrained returned {result} for {workspace}", hasWorkspace, workspace);

            return hasWorkspace;
        }
    }
}