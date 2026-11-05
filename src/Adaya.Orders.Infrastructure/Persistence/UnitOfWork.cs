using Adaya.Orders.Application.Abstractions;
using Adaya.Orders.Application.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Adaya.Orders.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var result = await action(ct);
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // scenario cincurrency update
            throw new ConcurrencyConflictException();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Orders_CustomerId_IdempotencyKey"
        })
        {
            // scenario duplikat idepotencykey
            throw new DuplicateIdempotencyKeyException();
        }
    }
}