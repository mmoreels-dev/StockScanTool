using Microsoft.Extensions.DependencyInjection;
using StockScanTool.Application.Repositories;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private readonly IServiceProvider _serviceProvider;

    public UnitOfWork(AppDbContext db, IServiceProvider serviceProvider)
    {
        _db = db;
        _serviceProvider = serviceProvider;
    }

    public IRepository<T> Repository<T>() where T : class
        => _serviceProvider.GetRequiredService<IRepository<T>>();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public void Dispose() => _db.Dispose();

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
