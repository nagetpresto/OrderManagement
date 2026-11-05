using Serilog.Context;

namespace Adaya.Orders.Api.Middlewares;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var id = ctx.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100)
            id = Guid.NewGuid().ToString("N");

        ctx.Items[HeaderName] = id;
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers[HeaderName] = id;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await next(ctx);
        }
    }
}