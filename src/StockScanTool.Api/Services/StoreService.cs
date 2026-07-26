using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Api.Services;

public class StoreService : CrudService<Store, StoreDto, CreateStoreRequest, UpdateStoreRequest>, IStoreService
{
    public StoreService(IStoreRepository repo, IUnitOfWork unitOfWork)
        : base(repo, unitOfWork) { }

    protected override System.Linq.Expressions.Expression<Func<Store, bool>> IdPredicate(int id)
        => s => s.Id == id;

    protected override StoreDto ToDto(Store s) => EntityMapper.ToDto(s);

    protected override Store ToEntity(CreateStoreRequest r)
        => new() { Name = r.Name, Address = r.Address, IsActive = r.IsActive };

    protected override void UpdateEntity(Store s, UpdateStoreRequest r)
    {
        s.Name = r.Name;
        s.Address = r.Address;
        s.IsActive = r.IsActive;
    }
}
