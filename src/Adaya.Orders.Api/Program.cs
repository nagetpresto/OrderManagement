using Adaya.Orders.Api.Middlewares;
using Adaya.Orders.Api.Models;
using Adaya.Orders.Application;
using Adaya.Orders.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text.Json.Serialization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, lc) => lc
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "orders-api")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new CompactJsonFormatter(), "logs/orders-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)))
    .ConfigureApiBehaviorOptions(o =>
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors
                        .Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Nilai tidak valid." : x.ErrorMessage)
                        .ToArray());

            return new BadRequestObjectResult(ApiError.From(
                ctx.HttpContext, 400, "validation_error", "Satu atau lebih field tidak valid.", errors));
        });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

app.Run();

public partial class Program { }