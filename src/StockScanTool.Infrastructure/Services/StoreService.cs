using System.Linq.Expressions;
using FluentValidation;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class StoreService : CrudService<Store, StoreDto, CreateStoreRequest, UpdateStoreRequest>, IStoreService
{
    public StoreService(
        IStoreRepository repo,
        IUnitOfWork unitOfWork,
        IValidator<CreateStoreRequest> createValidator,
        IValidator<UpdateStoreRequest> updateValidator)
        : base(repo, unitOfWork, createValidator, updateValidator) { }

    protected override Expression<Func<Store, bool>> IdPredicate(int id)
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
