using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
}
