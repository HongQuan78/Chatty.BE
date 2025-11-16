using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IAuthService
{
    /// <summary>
    /// Đăng ký user mới. Ném exception hoặc trả false nếu email/username đã tồn tại.
    /// </summary>
    Task<User> RegisterAsync(
        string userName,
        string email,
        string password,
        CancellationToken ct = default
    );

    /// <summary>
    /// Đăng nhập bằng email hoặc username + password.
    /// Trả về user + accessToken (JWT) hoặc ném exception nếu sai.
    /// </summary>
    Task<(User user, string accessToken)> LoginAsync(
        string userNameOrEmail,
        string password,
        CancellationToken ct = default
    );

    /// <summary>
    /// Đổi mật khẩu hiện tại.
    /// </summary>
    Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default
    );
}
