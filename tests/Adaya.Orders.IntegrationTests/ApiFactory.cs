using Adaya.Orders.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Adaya.Orders.IntegrationTests;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await _postgres.DisposeAsync();
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFactory> { }
