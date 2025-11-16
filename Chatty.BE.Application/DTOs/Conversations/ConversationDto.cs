using Chatty.BE.Application.DTOs.ConversationParticipants;
using Chatty.BE.Application.DTOs.Messages;
using Chatty.BE.Application.DTOs.Users;

namespace Chatty.BE.Application.DTOs.Conversations;

public class ConversationDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public bool IsGroup { get; set; }
    public Guid? OwnerId { get; set; }
    public UserDto? Owner { get; set; }
    public IReadOnlyList<ConversationParticipantDto>? Participants { get; set; }
    public MessageDto? LastMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
