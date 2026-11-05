using Adaya.Orders.Api.Models;
using Adaya.Orders.Application.Common;
using Adaya.Orders.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Adaya.Orders.Api.Middlewares;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, code, message, errors) = Map(ex);

        if (status >= 500)
            logger.LogError(ex, "Unhandled exception pada {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
        else
            logger.LogWarning("Request ditolak: {Code} ({Status}) - {Message}", code, status, message);

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(ApiError.From(ctx, status, code, message, errors), ct);
        return true;
    }

    private static (int Status, string Code, string Message, IDictionary<string, string[]>? Errors) Map(Exception ex)
        => ex switch
        {
            RequestValidationException v => (400, v.Code, v.Message, v.Errors),
            NotFoundException n => (404, n.Code, n.Message, null),
            ConcurrencyConflictException c => (409, c.Code, c.Message, null),
            InvalidStatusTransitionException t => (409, "invalid_status_transition", t.Message, null),
            InsufficientStockException s => (422, s.Code, s.Message, null),
            IdempotencyKeyReuseException i => (422, i.Code, i.Message, null),
            DomainException d => (422, "domain_error", d.Message, null),
            OperationCanceledException => (499, "client_closed_request", "Request dibatalkan oleh client.", null),
            _ => (500, "internal_error", "Terjadi kesalahan pada server.", null)
        };
}