namespace Adaya.Orders.Application.Abstractions;

public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}