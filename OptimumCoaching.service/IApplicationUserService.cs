using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IApplicationUserService
    {
        Task<(bool Success, string Message, ApplicationUser? User)> RegisterAsync(
            string fullName, string email, string password, string roleName = "User");
        Task<ApplicationUser?> FindByEmailAsync(string email);
        Task<ApplicationUser?> FindByIdAsync(Guid id);
        Task<IList<string>> GetRolesAsync(ApplicationUser user);
        Task<(bool Success, string Message)> ChangePasswordAsync(
            Guid userId, string currentPassword, string newPassword);

        // Admin operations
        Task<IList<ApplicationUser>> GetAllAsync();
        Task<(bool Success, string Message, ApplicationUser? User)> CreateAsync(
            string fullName, string email, string password, IEnumerable<string> roles, string? imageUrl = null);
        Task<(bool Success, string Message)> UpdateAsync(
            Guid userId, string fullName, bool isActive, ApplicationUserStatus status, string? imageUrl = null);
        Task<(bool Success, string Message)> DeleteAsync(Guid userId);
        Task<(bool Success, string Message)> SetRolesAsync(Guid userId, IEnumerable<string> roles);
        Task<(bool Success, string Message)> ResetPasswordAsync(Guid userId, string newPassword);
    }
}
