using System.Linq.Expressions;
using StockScanTool.Application.Repositories;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Services;

public abstract class CrudService<TEntity, TDto, TCreateRequest, TUpdateRequest>
    where TEntity : class
    where TDto : class
{
    protected readonly IRepository<TEntity> _repo;
    protected readonly IUnitOfWork _unitOfWork;

    protected CrudService(IRepository<TEntity> repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    protected abstract Expression<Func<TEntity, bool>> IdPredicate(int id);
    protected abstract TDto ToDto(TEntity entity);
    protected abstract TEntity ToEntity(TCreateRequest request);
    protected abstract void UpdateEntity(TEntity entity, TUpdateRequest request);

    public virtual async Task<List<TDto>> GetAllAsync()
    {
        var entities = await _repo.GetAllAsync();
        return entities.Select(ToDto).ToList();
    }

    public virtual async Task<PagedResult<TDto>> GetPagedAsync(PagedRequest request)
    {
        var allEntities = await _repo.GetAllAsync();
        var totalCount = allEntities.Count;

        var sorted = request.SortBy?.ToLower() switch
        {
            "name" => request.Descending
                ? allEntities.OrderByDescending(e => GetSortValue(e, "name")).ToList()
                : allEntities.OrderBy(e => GetSortValue(e, "name")).ToList(),
            _ => allEntities.OrderBy(e => GetSortValue(e, "id")).ToList()
        };

        var paged = sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<TDto>(paged.Select(ToDto).ToList(), totalCount, request.Page, request.PageSize);
    }

    public virtual async Task<TDto?> GetByIdAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return entity is null ? null : ToDto(entity);
    }

    public virtual async Task<TDto> CreateAsync(TCreateRequest request)
    {
        var entity = ToEntity(request);
        await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(entity);
    }

    public virtual async Task<TDto?> UpdateAsync(int id, TUpdateRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        UpdateEntity(entity, request);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(entity);
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;

        _repo.Remove(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static object GetSortValue(TEntity entity, string field)
    {
        var prop = typeof(TEntity).GetProperty(field switch
        {
            "name" => typeof(TEntity).GetProperties().FirstOrDefault(p =>
                p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("DeviceName", StringComparison.OrdinalIgnoreCase))?.Name ?? "Id",
            _ => "Id"
        });
        return prop?.GetValue(entity) ?? 0;
    }
}
