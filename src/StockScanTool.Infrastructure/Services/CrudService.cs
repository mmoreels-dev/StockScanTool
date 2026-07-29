using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Contracts;

namespace StockScanTool.Infrastructure.Services;

public abstract class CrudService<TEntity, TDto, TCreateRequest, TUpdateRequest>
    where TEntity : class
    where TDto : class
{
    protected readonly IRepository<TEntity> _repo;
    protected readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<TCreateRequest>? _createValidator;
    private readonly IValidator<TUpdateRequest>? _updateValidator;

    protected CrudService(
        IRepository<TEntity> repo,
        IUnitOfWork unitOfWork,
        IValidator<TCreateRequest>? createValidator = null,
        IValidator<TUpdateRequest>? updateValidator = null)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    protected abstract Expression<Func<TEntity, bool>> IdPredicate(int id);
    protected abstract TDto ToDto(TEntity entity);
    protected abstract TEntity ToEntity(TCreateRequest request);
    protected abstract void UpdateEntity(TEntity entity, TUpdateRequest request);

    public virtual async Task<List<TDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repo.GetAllAsync();
        return entities.Select(ToDto).ToList();
    }

    public virtual async Task<PagedResult<TDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _repo.AsQueryable();
        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.Descending
                ? query.OrderByDescending(e => GetSortValue(e, "name"))
                : query.OrderBy(e => GetSortValue(e, "name")),
            _ => query.OrderBy(e => GetSortValue(e, "id"))
        };

        var paged = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TDto>(paged.Select(ToDto).ToList(), totalCount, request.Page, request.PageSize);
    }

    public virtual async Task<TDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id);
        return entity is null ? null : ToDto(entity);
    }

    public virtual async Task<TDto> CreateAsync(TCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (_createValidator is not null)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);
        }

        var entity = ToEntity(request);
        await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public virtual async Task<TDto?> UpdateAsync(int id, TUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (_updateValidator is not null)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);
        }

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        UpdateEntity(entity, request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;

        _repo.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
