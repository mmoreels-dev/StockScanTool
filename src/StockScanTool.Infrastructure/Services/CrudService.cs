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
    protected readonly IValidator<TCreateRequest>? _createValidator;
    protected readonly IValidator<TUpdateRequest>? _updateValidator;

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

    protected abstract TDto ToDto(TEntity entity);
    protected abstract TEntity ToEntity(TCreateRequest request);
    protected abstract void UpdateEntity(TEntity entity, TUpdateRequest request);

    protected virtual IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query)
    {
        return query;
    }

    public virtual async Task<List<TDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = ApplyIncludes(_repo.AsQueryable());
        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(ToDto).ToList();
    }

    public virtual async Task<PagedResult<TDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = ApplyIncludes(_repo.AsQueryable());
        var totalCount = await query.CountAsync(cancellationToken);

        query = request.Descending
            ? query.OrderByDescending(e => EF.Property<object>(e, "Id"))
            : query.OrderBy(e => EF.Property<object>(e, "Id"));

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
}
