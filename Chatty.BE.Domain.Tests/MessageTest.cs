using System;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums;
using Xunit;

namespace Chatty.BE.Domain.Tests.Entities
{
    public class MessageTests
    {
        [Fact]
        public void New_message_should_have_default_values()
        {
            // Arrange
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = Guid.NewGuid(),
                SenderId = Guid.NewGuid(),
                Content = "Hello world",
                Type = MessageType.Text,
                Status = MessageStatus.Sent,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Assert
            Assert.False(message.IsDeleted);
            Assert.Equal(MessageType.Text, message.Type);
            Assert.Equal(MessageStatus.Sent, message.Status);
            Assert.NotEqual(Guid.Empty, message.ConversationId);
            Assert.NotEqual(Guid.Empty, message.SenderId);
        }
    }
}
