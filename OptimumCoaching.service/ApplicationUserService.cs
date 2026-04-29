using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public class ApplicationUserService : IApplicationUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public ApplicationUserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<(bool Success, string Message, ApplicationUser? User)> RegisterAsync(
            string fullName, string email, string password, string roleName = "User")
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Full name is required", null);
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email is required", null);
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required", null);

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
                return (false, "Email already registered", null);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                UserName = email,
                Created = DateTime.UtcNow,
                Status = ApplicationUserStatus.Active,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Registration failed: {errors}", null);
            }

            if (!string.IsNullOrWhiteSpace(roleName) && await _roleManager.RoleExistsAsync(roleName))
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }

            return (true, "Registration successful", user);
        }

        public Task<ApplicationUser?> FindByEmailAsync(string email)
            => _userManager.FindByEmailAsync(email);

        public Task<ApplicationUser?> FindByIdAsync(Guid id)
            => _userManager.FindByIdAsync(id.ToString());

        public Task<IList<string>> GetRolesAsync(ApplicationUser user)
            => _userManager.GetRolesAsync(user);

        public async Task<(bool Success, string Message)> ChangePasswordAsync(
            Guid userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return (false, "User not found");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            user.LastPassChangeDate = DateTime.UtcNow;
            user.PasswordChangedCount++;
            await _userManager.UpdateAsync(user);

            return (true, "Password changed successfully");
        }

        public Task<IList<ApplicationUser>> GetAllAsync() =>
            _userManager.Users.OrderBy(u => u.FullName).ToListAsync()
                .ContinueWith(t => (IList<ApplicationUser>)t.Result);

        public async Task<(bool Success, string Message, ApplicationUser? User)> CreateAsync(
            string fullName, string email, string password, IEnumerable<string> roles)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Full name is required", null);
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email is required", null);
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required", null);

            if (await _userManager.FindByEmailAsync(email) != null)
                return (false, "Email already registered", null);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                UserName = email,
                Status = ApplicationUserStatus.Active,
                IsActive = true,
                Created = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);

            foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                if (await _roleManager.RoleExistsAsync(role))
                    await _userManager.AddToRoleAsync(user, role);
            }

            return (true, "User created", user);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            Guid userId, string fullName, bool isActive, ApplicationUserStatus status)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return (false, "User not found");

            user.FullName = fullName;
            user.IsActive = isActive;
            user.Status = status;
            user.LastModified = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? (true, "User updated")
                : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return (false, "User not found");

            user.IsDeleted = true;
            user.IsActive = false;
            user.Status = ApplicationUserStatus.Inactive;
            user.LastModified = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? (true, "User deactivated")
                : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<(bool Success, string Message)> SetRolesAsync(
            Guid userId, IEnumerable<string> roles)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return (false, "User not found");

            var current = await _userManager.GetRolesAsync(user);
            var desired = roles.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();

            var toRemove = current.Except(desired).ToList();
            var toAdd = desired.Except(current).Where(r =>
                _roleManager.RoleExistsAsync(r).GetAwaiter().GetResult()).ToList();

            if (toRemove.Any())
            {
                var r1 = await _userManager.RemoveFromRolesAsync(user, toRemove);
                if (!r1.Succeeded)
                    return (false, string.Join(", ", r1.Errors.Select(e => e.Description)));
            }
            if (toAdd.Any())
            {
                var r2 = await _userManager.AddToRolesAsync(user, toAdd);
                if (!r2.Succeeded)
                    return (false, string.Join(", ", r2.Errors.Select(e => e.Description)));
            }
            return (true, "Roles updated");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(
            Guid userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return (false, "User not found");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded
                ? (true, "Password reset")
                : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
