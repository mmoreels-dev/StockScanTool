using FluentValidation;
using Microsoft.EntityFrameworkCore;
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

    protected override StoreDto ToDto(Store s) => EntityMapper.ToDto(s);

    public override async Task<PagedResult<StoreDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _repo.AsQueryable();
        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.Descending
                ? query.OrderByDescending(s => s.Name)
                : query.OrderBy(s => s.Name),
            _ => query.OrderBy(s => s.Id)
        };

        var paged = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StoreDto>(paged.Select(ToDto).ToList(), totalCount, request.Page, request.PageSize);
    }

    protected override Store ToEntity(CreateStoreRequest r)
        => new() { Name = r.Name, Address = r.Address, IsActive = r.IsActive };

    protected override void UpdateEntity(Store s, UpdateStoreRequest r)
    {
        s.Name = r.Name;
        s.Address = r.Address;
        s.IsActive = r.IsActive;
    }
}
