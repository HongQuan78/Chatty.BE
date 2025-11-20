using System.Linq;
using AutoMapper;
using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Application.DTOs.ConversationParticipants;
using Chatty.BE.Application.DTOs.Conversations;
using Chatty.BE.Application.DTOs.MessageAttachments;
using Chatty.BE.Application.DTOs.MessageReceipts;
using Chatty.BE.Application.DTOs.Messages;
using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Infrastructure.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Users
        CreateMap<User, UserDto>()
            .ReverseMap()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

        CreateMap<User, UserResponse>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));

        CreateMap<CreateUserRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateUserProfileRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // Conversations
        CreateMap<Conversation, ConversationDto>()
            .ForMember(
                dest => dest.LastMessage,
                opt =>
                    opt.MapFrom(src =>
                        src.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()
                    )
            );

        CreateMap<ConversationDto, Conversation>()
            .ForMember(dest => dest.Participants, opt => opt.Ignore())
            .ForMember(dest => dest.Messages, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<Conversation, ConversationResponse>()
            .ForMember(dest => dest.Conversation, opt => opt.MapFrom(src => src));

        CreateMap<CreatePrivateConversationRequest, Conversation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsGroup, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.Name, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Participants, opt => opt.Ignore())
            .ForMember(dest => dest.Messages, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        CreateMap<CreateGroupConversationRequest, Conversation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsGroup, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.Participants, opt => opt.Ignore())
            .ForMember(dest => dest.Messages, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // Conversation Participants
        CreateMap<ConversationParticipant, ConversationParticipantDto>()
            .ReverseMap()
            .ForMember(dest => dest.Conversation, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());

        CreateMap<AddParticipantCommand, ConversationParticipant>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsAdmin, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Conversation, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // Messages
        CreateMap<Message, MessageDto>()
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments))
            .ForMember(dest => dest.Receipts, opt => opt.MapFrom(src => src.Receipts));

        CreateMap<MessageDto, Message>()
            .ForMember(dest => dest.Attachments, opt => opt.Ignore())
            .ForMember(dest => dest.Receipts, opt => opt.Ignore())
            .ForMember(dest => dest.Conversation, opt => opt.Ignore())
            .ForMember(dest => dest.Sender, opt => opt.Ignore());

        CreateMap<Message, MessageResponse>()
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src));

        CreateMap<SendMessageRequest, Message>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Attachments, opt => opt.Ignore())
            .ForMember(dest => dest.Receipts, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Conversation, opt => opt.Ignore())
            .ForMember(dest => dest.Sender, opt => opt.Ignore());

        CreateMap<SendMessageCommand, Message>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Attachments, opt => opt.Ignore())
            .ForMember(dest => dest.Receipts, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Conversation, opt => opt.Ignore())
            .ForMember(dest => dest.Sender, opt => opt.Ignore());

        // Message attachments
        CreateMap<MessageAttachment, MessageAttachmentDto>()
            .ReverseMap()
            .ForMember(dest => dest.Message, opt => opt.Ignore());

        CreateMap<CreateMessageAttachmentRequest, MessageAttachment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.MessageId, opt => opt.Ignore())
            .ForMember(dest => dest.Message, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // Message receipts
        CreateMap<MessageReceipt, MessageReceiptDto>()
            .ReverseMap()
            .ForMember(dest => dest.Message, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());

        CreateMap<UpdateReceiptStatusCommand, MessageReceipt>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Message, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeliveredAt, opt => opt.Ignore())
            .ForMember(dest => dest.ReadAt, opt => opt.Ignore());

        // Auth sessions
        CreateMap<RefreshToken, SessionDto>()
            .ForCtorParam("TokenId", opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("CreatedAt", opt => opt.MapFrom(src => src.CreatedAt))
            .ForCtorParam("ExpiresAt", opt => opt.MapFrom(src => src.ExpiresAt))
            .ForCtorParam("CreatedByIp", opt => opt.MapFrom(src => src.CreatedByIp))
            .ForCtorParam("IsRevoked", opt => opt.MapFrom(src => src.RevokedAt.HasValue))
            .ForCtorParam("IsReused", opt => opt.MapFrom(src => src.IsReusedToken));
    }
}
