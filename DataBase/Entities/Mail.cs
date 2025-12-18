using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DataBase.Entities
{
    /// <summary>
    /// 游戏邮件
    /// </summary>
    [Table("mails")]
    public class Mail
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong Id { get; set; }

        [Required, MaxLength(100)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("content")]
        public string? Content { get; set; }

        [Required, MaxLength(50)]
        [Column("sender")]
        public string Sender { get; set; } = string.Empty;

        // 收件人，关联 characters.characterId
        [Required, MaxLength(255)]
        [Column("receiverCharacterId")]
        public string ReceiverCharacterId { get; set; } = default!;

        [Column("isRead", TypeName = "tinyint")]
        public bool IsRead { get; set; } = false;

        [Column("isAttachmentClaimed", TypeName = "tinyint")]
        public bool IsAttachmentClaimed { get; set; } = false;

        [Column("createTime")]
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        [Column("expireTime")]
        public DateTime? ExpireTime { get; set; }

        /// <summary>
        /// 导航属性：邮件附件列表
        /// </summary>
        public List<MailAttachment> Attachments { get; set; } = new();
    }



    /// <summary>
    /// 邮件附件
    /// </summary>
    [Table("mailattachments")]
    public class MailAttachment
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong Id { get; set; }

        [Column("mailId")]
        public ulong MailId { get; set; }

        [Required, MaxLength(60)]
        [Column("templateId")]
        public string TemplateId { get; set; } = default!;

        [Column("count")]
        public uint Count { get; set; } = 1;

        /// <summary>
        /// 导航属性：所属邮件
        /// </summary>
        [ForeignKey(nameof(MailId))]
        public Mail Mail { get; set; } = default!;
    }
}
