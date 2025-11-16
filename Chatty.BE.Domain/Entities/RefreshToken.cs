using System.ComponentModel.DataAnnotations;

namespace Chatty.BE.Domain.Entities;

public class RefreshToken : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    public virtual User User { get; set; } = default!;

    [Required]
    [MaxLength(256)]
    public string TokenHash { get; set; } = default!;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public virtual RefreshToken? ReplacedByToken { get; set; }

    [MaxLength(128)]
    public string? CreatedByIp { get; set; }

    [MaxLength(128)]
    public string? RevokedByIp { get; set; }

    [MaxLength(256)]
    public string? ReasonRevoked { get; set; }

    public bool IsReusedToken { get; set; }
}
