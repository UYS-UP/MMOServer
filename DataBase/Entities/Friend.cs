using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.DataBase.Entities
{
    /// <summary>
    /// 好友关系（双向：A->B、B->A 各一条）
    /// 说明：保留 GroupName 兼容；新增 FriendGroupId（对应 friendGroupId）
    /// </summary>
    [Table("friends")]
    public class Friend
    {
        [Key]
        [Column("friendshipId")]
        public string FriendshipId { get; set; } = Guid.NewGuid().ToString();

        [Required, MaxLength(64)]
        [Column("characterId")]
        public string CharacterId { get; set; } = default!;

        [Required, MaxLength(64)]
        [Column("playerId")]
        public string PlayerId { get; set; } = default!;

        [Required, MaxLength(64)]
        [Column("friendCharacterId")]
        public string FriendCharacterId { get; set; } = default!; // 注意：与你原表字段一致（Firend）

        [Required, MaxLength(64)]
        [Column("friendPlayerId")]
        public string FriendPlayerId { get; set; } = default!;

        [MaxLength(64)]
        [Column("friendCharacterName")]
        public string FriendCharacterName { get; set; } = string.Empty;

        [MaxLength(128)]
        [Column("remark")]
        public string? Remark { get; set; }

        [Column("intimacy")]
        public int Intimacy { get; set; } = 0;

        [Column("friendGroupId")]
        public string? FriendGroupId { get; set; }

        [Column("createTime")]
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    }

    /// <summary>好友分组</summary>
    [Table("friendgroups")]
    public class FriendGroup
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required, MaxLength(64)]
        [Column("ownerCharacterId")]
        public string OwnerCharacterId { get; set; } = default!;

        [Required, MaxLength(64)]
        [Column("name")]
        public string Name { get; set; } = default!;

        [Column("sortOrder")]
        public int SortOrder { get; set; } = 0;

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum FriendRequestStatus : byte
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Expired = 3
    }

    /// <summary>好友申请（离线存储）</summary>
    [Table("friendrequests")]
    public class FriendRequest
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required, MaxLength(64)]
        [Column("fromCharacterId")]
        public string FromCharacterId { get; set; } = default!;

        [Required, MaxLength(64)]
        [Column("toCharacterId")]
        public string ToCharacterId { get; set; } = default!;

        [MaxLength(256)]
        [Column("message")]
        public string? Message { get; set; }

        [Column("status", TypeName = "tinyint")]
        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("respondedAt")]
        public DateTime? RespondedAt { get; set; }

        [Column("expiresAt")]
        public DateTime? ExpiresAt { get; set; }

        // 生成列 isPending 不必在 EF 中映射（只在 DB 层存在），如需可加只读属性
    }

    public enum PrivateMessageStatus : byte
    {
        Queued = 0,
        Delivered = 1,
        Read = 2
    }

    public enum PrivateMessageContentType : byte
    {
        Text = 0,
        Emoji = 1,
        ItemLink = 2
    }

    /// <summary>私聊消息（离线/历史）</summary>
    [Table("privatemessages")]
    public class PrivateMessage
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required, MaxLength(130)]
        [Column("conversationKey")]
        public string ConversationKey { get; set; } = default!;

        [Required, MaxLength(64)]
        [Column("senderCharacterId")]
        public string SenderCharacterId { get; set; } = default!;

        [Required, MaxLength(64)]
        [Column("recipientCharacterId")]
        public string RecipientCharacterId { get; set; } = default!;

        [Column("contentType", TypeName = "tinyint")]
        public PrivateMessageContentType ContentType { get; set; } = PrivateMessageContentType.Text;

        [Required]
        [Column("content")]
        public string Content { get; set; } = default!;

        [Column("sentAt")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Column("deliveredAt")]
        public DateTime? DeliveredAt { get; set; }

        [Column("readAt")]
        public DateTime? ReadAt { get; set; }

        [Column("status", TypeName = "tinyint")]
        public PrivateMessageStatus Status { get; set; } = PrivateMessageStatus.Queued;
    }
}
