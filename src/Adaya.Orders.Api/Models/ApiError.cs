using System.Diagnostics;
using Adaya.Orders.Api.Middlewares;

namespace Adaya.Orders.Api.Models;

public record ApiError(int Status, string Code, string Message,
    IDictionary<string, string[]>? Errors, string TraceId, string? CorrelationId)
{
    public static ApiError From(HttpContext ctx, int status, string code, string message,
        IDictionary<string, string[]>? errors = null)
        => new(status, code, message, errors,
            Activity.Current?.TraceId.ToString() ?? ctx.TraceIdentifier,
            ctx.Items[CorrelationIdMiddleware.HeaderName] as string);
}