using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WB.Core.Infrastructure.HttpServices.Services;

namespace WB.UI.Shared.Web.Integrity;

public class IntegrityHeaderMiddleware
{
    private readonly RequestDelegate next;

    public IntegrityHeaderMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[IntegrityService.IntegrityHeaderName] = IntegrityService.IntegrityHeaderValue;
            return Task.CompletedTask;
        });

        await next(context);
    }
}
