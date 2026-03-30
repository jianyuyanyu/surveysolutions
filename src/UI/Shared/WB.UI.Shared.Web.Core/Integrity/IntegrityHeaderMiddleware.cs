using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WB.UI.Shared.Web.Integrity;

public class IntegrityHeaderMiddleware
{
    private readonly RequestDelegate next;
    private const string HeaderValue = "773994826649214";

    private const string XSurveySolutions = "X-Survey-Solutions";
    public IntegrityHeaderMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[XSurveySolutions] = HeaderValue;
            return Task.CompletedTask;
        });

        await next(context);
    }
}
