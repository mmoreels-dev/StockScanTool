namespace StockScanTool.Application.Repositories;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
