using Microsoft.AspNetCore.SignalR;

namespace Chatty.BE.Infrastructure.SignalR;

public class ChatHub : Hub<IChatClient>
{
    // Gợi ý: nhóm theo UserId để gửi riêng từng user
    public override async Task OnConnectedAsync()
    {
        // Ví dụ: lấy userId từ Claims hoặc query string
        var userId =
            Context.User?.FindFirst("sub")?.Value
            ?? Context.GetHttpContext()?.Request.Query["userId"].ToString();

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Có thể remove khỏi group nếu muốn, tuỳ logic
        await base.OnDisconnectedAsync(exception);
    }
}
