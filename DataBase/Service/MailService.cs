using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.DataBase.Entities;
using Server.DataBase.Repositories;

namespace Server.Game.Service
{
    /// <summary>
    /// 邮件系统服务
    /// - 支持单发、批量、系统广播
    /// - 支持附件：操作前检查是否已领取
    /// - 支持离线存储、过期清理
    /// </summary>
    public class MailService
    {
        private readonly UnitOfWork uow;

        public MailService(UnitOfWork uow)
        {
            this.uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        // ========== 发邮件 ==========

        /// <summary>
        /// 发送邮件，可带附件
        /// </summary>
        public async Task<Mail> SendMailAsync(
            string sender,
            string receiverCharacterId,
            string title,
            string content,
            IEnumerable<(string templateId, uint count)>? attachments = null,
            DateTime? expireTime = null)
        {
            var mail = new Mail
            {
                Sender = sender,
                ReceiverCharacterId = receiverCharacterId,
                Title = title,
                Content = content,
                CreateTime = DateTime.UtcNow,
                ExpireTime = expireTime
            };

            await uow.Mails.AddAsync(mail);
            await uow.SaveChangesAsync();

            if (attachments != null)
            {
                foreach (var (templateId, count) in attachments)
                {
                    await uow.MailAttachments.AddAsync(new MailAttachment
                    {
                        MailId = mail.Id,
                        TemplateId = templateId,
                        Count = count
                    });
                }
                await uow.SaveChangesAsync();
            }

            return mail;
        }

        /// <summary>
        /// GM / 系统 广播邮件（发给所有角色）
        /// </summary>
        public async Task<int> SendMailToAllAsync(string sender, string title, string content,
            IEnumerable<(string templateId, uint count)>? attachments = null,
            DateTime? expire = null)
        {
            var characters = await uow.Characters.GetAllAsync();
            int count = 0;

            foreach (var c in characters)
            {
                await SendMailAsync(sender, c.CharacterId, title, content, attachments, expire);
                count++;
            }

            return count;
        }


        // ========== 查询 ==========

        public async Task<IEnumerable<Mail>> GetMailListAsync(string receiverCharacterId, int take = 50)
        {
            return await uow.Mails.FindAsync(m => m.ReceiverCharacterId == receiverCharacterId);
        }

        public async Task<int> GetUnreadCountAsync(string receiverCharacterId)
        {
            return await uow.Mails.CountAsync(m => m.ReceiverCharacterId == receiverCharacterId && !m.IsRead);
        }


        // ========== 状态修改 ==========

        public async Task<bool> MarkMailReadAsync(ulong mailId, string receiverCharacterId)
        {
            var mail = await uow.Mails.FirstOrDefaultAsync(m => m.Id == mailId && m.ReceiverCharacterId == receiverCharacterId);
            if (mail == null) return false;

            mail.IsRead = true;
            uow.Mails.Update(mail);
            await uow.SaveChangesAsync();
            return true;
        }


        // ========== 领取附件 ==========

        /// <summary>
        /// 领取附件：返回附件列表（由调用方发物品）
        /// </summary>
        public async Task<(bool ok, List<MailAttachment> items, string msg)> ClaimAttachmentsAsync(
            ulong mailId)
        {
            var mail = await uow.Mails.GetByIdAsync(mailId);
            
            if (mail == null) return (false, null, "邮件不存在");
            if (mail.IsAttachmentClaimed) return (false, null, "附件已领取");

            mail.IsAttachmentClaimed = true;
            mail.IsRead = true; // 领取即视为已读
            uow.Mails.Update(mail);
            await uow.SaveChangesAsync();

            return (true, mail.Attachments.ToList(), "领取成功");
        }


        // ========== 删除 ==========

        public async Task<bool> DeleteMailAsync(ulong mailId, string receiverCharacterId)
        {
            var mail = await uow.Mails.FirstOrDefaultAsync(m => m.Id == mailId && m.ReceiverCharacterId == receiverCharacterId);
            if (mail == null) return false;

            uow.Mails.Delete(mail);
            await uow.SaveChangesAsync();
            return true;
        }


        // ========== 清理过期 ==========

        public async Task<int> CleanupExpiredMailsAsync()
        {
            var now = DateTime.UtcNow;
            var expired = await uow.Mails.FindAsync(m => m.ExpireTime != null && m.ExpireTime < now);

            var list = expired.ToList();
            if (list.Any())
            {
                uow.Mails.DeleteRange(list);
                await uow.SaveChangesAsync();
            }

            return list.Count;
        }
    }
}
