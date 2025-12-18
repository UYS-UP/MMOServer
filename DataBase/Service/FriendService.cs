using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.DataBase.Entities;
using Server.DataBase.Repositories;

namespace Server.Game.Service
{
    /// <summary>
    /// 好友系统服务（仓储版：EF Core + Dapper）
    /// - 分组：创建/重命名/删除/移动
    /// - 申请：发送/拉取/处理（支持离线）
    /// - 私聊：存储/拉取离线/标记送达与已读/会话历史
    /// - 好友：判断/互加/删除/备注
    /// </summary>
    public class FriendService
    {
        private readonly UnitOfWork uow;

        public FriendService(UnitOfWork uow)
        {
            this.uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        // ========== 分组 FriendGroups ==========

        public async Task<FriendGroup> CreateGroupAsync(string ownerCharacterId, string name, int sortOrder = 0)
        {
            // 唯一：同一角色下 Name 不重复
            var exists = await uow.FriendGroups.FirstOrDefaultAsync(g => g.OwnerCharacterId == ownerCharacterId && g.Name == name);
            if (exists != null) return exists;

            var group = new FriendGroup
            {
                OwnerCharacterId = ownerCharacterId,
                Name = name,
                SortOrder = sortOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await uow.FriendGroups.AddAsync(group);
            await uow.SaveChangesAsync();
            return group;
        }

        public async Task<bool> RenameGroupAsync(string groupId, string newName)
        {
            var group = await uow.FriendGroups.GetByIdAsync(groupId);
            if (group == null) return false;

            group.Name = newName;
            group.UpdatedAt = DateTime.UtcNow;
            uow.FriendGroups.Update(group);
            await uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteGroupAsync(string ownerCharacterId, string groupId, bool moveFriendsToDefault = true)
        {
            var group = await uow.FriendGroups.FirstOrDefaultAsync(g => g.Id == groupId && g.OwnerCharacterId == ownerCharacterId);
            if (group == null) return false;

            if (moveFriendsToDefault)
            {
                // 将组内好友移动到"无分组"（FriendGroupId = null）
                var inGroup = await uow.Friends.FindAsync(f => f.CharacterId == ownerCharacterId && f.FriendGroupId == groupId);
                foreach (var f in inGroup)
                {
                    f.FriendGroupId = null;
                    uow.Friends.Update(f);
                }
            }

            uow.FriendGroups.Delete(group);
            await uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MoveFriendToGroupAsync(string ownerCharacterId, string friendCharacterId, string? groupId)
        {
            // 只改"我这边"的那条好友记录
            var f = await uow.Friends.FirstOrDefaultAsync(x => x.CharacterId == ownerCharacterId && x.FriendCharacterId == friendCharacterId);
            if (f == null) return false;

            if (groupId != null)
            {
                // 组必须属于我
                var g = await uow.FriendGroups.FirstOrDefaultAsync(x => x.Id == groupId && x.OwnerCharacterId == ownerCharacterId);
                if (g == null) return false;
            }

            f.FriendGroupId = groupId;
            uow.Friends.Update(f);
            await uow.SaveChangesAsync();
            return true;
        }

        // ========== 好友申请 FriendRequests ==========

        /// <summary>
        /// 发送好友申请（带幂等/去重）。
        /// 优先用 Dapper 利用唯一索引 (fromCharacterId,toCharacterId,isPending) 去重，避免并发竞态。
        /// </summary>
        public async Task<(bool ok, string requestId, string message)> CreateFriendRequestAsync(
            string fromCharacterId, string toCharacterId, string? message, TimeSpan? ttl = null)
        {
            if (fromCharacterId == toCharacterId)
                return (false, null, "不能向自己发送好友申请");

            // 已是好友直接返回
            if (await IsFriendAsync(fromCharacterId, toCharacterId))
                return (false, null, "对方已是你的好友");

            var reqId = Guid.NewGuid().ToString();
            var expires = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : (DateTime?)null;

            // 依赖表 FriendRequests 的唯一键：ux_friendrequests_pending_dedupe
            const string sql = @"
                INSERT INTO `FriendRequests`
                (`id`,`fromCharacterId`,`toCharacterId`,`message`,`status`,`createdAt`,`expiresAt`)
                VALUES (@Id,@FromId,@ToId,@Msg,0,CURRENT_TIMESTAMP(6),@Expires);";

            try
            {
                await uow.FriendRequests.ExecuteAsync(sql, new
                {
                    Id = reqId,
                    FromId = fromCharacterId,
                    ToId = toCharacterId,
                    Msg = message,
                    Expires = expires
                });
                return (true, reqId, "已向对方发送申请");
            }
            catch (Exception ex)
            {
                // 唯一键冲突（已存在 Pending）
                if (ex.Message.IndexOf("ux_friendrequests_pending_dedupe", StringComparison.OrdinalIgnoreCase) >= 0)
                    return (false, null, "你已发送过好友申请，待对方处理");
                return (false, null, ex.Message);
            }
        }

        public async Task<IEnumerable<FriendRequest>> GetPendingRequestsAsync(string toCharacterId, int take = 50)
        {
            return await uow.FriendRequests.FindAsync(r => r.ToCharacterId == toCharacterId
                && r.Status == FriendRequestStatus.Pending
                && (r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow));
        }

        public async Task<(bool, Friend, Friend, Character, Character)> HandleFriendRequestAsync(string requestId, bool accept)
        {
            var req = await uow.FriendRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null || req.Status != FriendRequestStatus.Pending) return (false, null, null, null, null);

            req.Status = accept ? FriendRequestStatus.Accepted : FriendRequestStatus.Rejected;
            req.RespondedAt = DateTime.UtcNow;
            uow.FriendRequests.Update(req);

            if (!accept)
            {
                await uow.SaveChangesAsync();
                return (true, null, null, null, null);
            }
            var fromCharacter = await uow.Characters.GetByIdAsync(req.FromCharacterId);
            var toCharacter = await uow.Characters.GetByIdAsync(req.ToCharacterId);
            var fromGroup = await uow.FriendGroups.FirstOrDefaultAsync(g => g.OwnerCharacterId == fromCharacter.CharacterId);
            var toGroup = await uow.FriendGroups.FirstOrDefaultAsync(g => g.OwnerCharacterId == toCharacter.CharacterId);
            // 双向添加好友（最小可用：不含事务；如需强一致请用 UnitOfWork 事务接口包裹）
            // 这两条只写必要字段，其他可登录时补齐展示缓存（昵称等）
            var a2b = new Friend
            {
                FriendshipId = Guid.NewGuid().ToString(),
                CharacterId = fromCharacter.CharacterId,
                PlayerId = fromCharacter.PlayerId,
                FriendPlayerId = fromCharacter.PlayerId,
                FriendGroupId = fromGroup.Id,
                FriendCharacterId = toCharacter.CharacterId,
                CreateTime = DateTime.UtcNow
            };
            var b2a = new Friend
            {
                FriendshipId = Guid.NewGuid().ToString(),
                CharacterId = toCharacter.CharacterId,
                PlayerId = toCharacter.PlayerId,
                FriendPlayerId = toCharacter.PlayerId,
                FriendGroupId = toGroup.Id,
                FriendCharacterId = req.FromCharacterId,
                CreateTime = DateTime.UtcNow
            };

            await uow.Friends.AddAsync(a2b);
            await uow.Friends.AddAsync(b2a);
            await uow.SaveChangesAsync();
            return (true, a2b, b2a, fromCharacter, toCharacter);
        }

        // ========== 好友 Friend ==========

        public async Task<bool> IsFriendAsync(string aCharacterId, string bCharacterId)
        {
            return await uow.Friends.AnyAsync(f => f.CharacterId == aCharacterId && f.FriendCharacterId == bCharacterId);
        }

        /// <summary>直接互加好友（用于 GM 或已通过申请的场景）</summary>
        public async Task<bool> AddFriendPairAsync(Friend aToB, Friend bToA)
        {
            // 去重校验
            if (await IsFriendAsync(aToB.CharacterId, aToB.FriendCharacterId)) return true;
            if (await IsFriendAsync(bToA.CharacterId, bToA.FriendCharacterId)) return true;

            await uow.Friends.AddAsync(aToB);
            await uow.Friends.AddAsync(bToA);
            await uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFriendAsync(string ownerCharacterId, string friendCharacterId)
        {
            // 两条都删：A->B 与 B->A
            var list = await uow.Friends.FindAsync(f =>
                (f.CharacterId == ownerCharacterId && f.FriendCharacterId == friendCharacterId) ||
                (f.CharacterId == friendCharacterId && f.FriendCharacterId == ownerCharacterId));

            if (!list.Any()) return false;

            uow.Friends.DeleteRange(list);
            await uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetFriendRemarkAsync(string ownerCharacterId, string friendCharacterId, string remark)
        {
            var f = await uow.Friends.FirstOrDefaultAsync(x => x.CharacterId == ownerCharacterId && x.FriendCharacterId == friendCharacterId);
            if (f == null) return false;

            f.Remark = remark;
            uow.Friends.Update(f);
            await uow.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Friend>> GetFriendListAsync(string ownerCharacterId)
        {
            return await uow.Friends.FindAsync(f => f.CharacterId == ownerCharacterId);
        }

        public async Task<IEnumerable<FriendGroup>> GetFriendGroupListAsync(string ownerCharacterId)
        {
            return await uow.FriendGroups.FindAsync(f => f.OwnerCharacterId == ownerCharacterId);
        }

        // ========== 私聊 PrivateMessages ==========

        public async Task<long> StorePrivateMessageAsync(string senderCharacterId, string recipientCharacterId,
            PrivateMessageContentType contentType, string content, bool recipientOnline = false)
        {
            var key = MakeConversationKey(senderCharacterId, recipientCharacterId);
            var msg = new PrivateMessage
            {
                ConversationKey = key,
                SenderCharacterId = senderCharacterId,
                RecipientCharacterId = recipientCharacterId,
                ContentType = contentType,
                Content = content,
                SentAt = DateTime.UtcNow,
                Status = recipientOnline ? PrivateMessageStatus.Delivered : PrivateMessageStatus.Queued,
                DeliveredAt = recipientOnline ? DateTime.UtcNow : null
            };
            await uow.PrivateMessages.AddAsync(msg);
            await uow.SaveChangesAsync();
            return msg.Id;
        }

        public async Task<List<PrivateMessage>> PullQueuedMessagesAsync(string recipientCharacterId, int take = 200)
        {
            return (await uow.PrivateMessages.FindAsync(m => m.RecipientCharacterId == recipientCharacterId && m.Status == PrivateMessageStatus.Queued))
                .OrderBy(m => m.Id)
                .Take(take)
                .ToList();
        }

        public async Task SetDeliveredAsync(IEnumerable<long> messageIds)
        {
            var ids = messageIds?.ToArray() ?? Array.Empty<long>();
            if (ids.Length == 0) return;

            var msgs = await uow.PrivateMessages.FindAsync(m => ids.Contains(m.Id));
            foreach (var m in msgs)
            {
                if (m.Status == PrivateMessageStatus.Queued)
                {
                    m.Status = PrivateMessageStatus.Delivered;
                    m.DeliveredAt = DateTime.UtcNow;
                    uow.PrivateMessages.Update(m);
                }
            }
            await uow.SaveChangesAsync();
        }

        public async Task SetReadAsync(IEnumerable<long> messageIds)
        {
            var ids = messageIds?.ToArray() ?? Array.Empty<long>();
            if (ids.Length == 0) return;

            var msgs = await uow.PrivateMessages.FindAsync(m => ids.Contains(m.Id));
            foreach (var m in msgs)
            {
                m.Status = PrivateMessageStatus.Read;
                m.ReadAt = DateTime.UtcNow;
                uow.PrivateMessages.Update(m);
            }
            await uow.SaveChangesAsync();
        }

        public async Task<List<PrivateMessage>> GetConversationHistoryAsync(
            string aCharacterId, string bCharacterId, long? beforeId, int take = 50)
        {
            var key = MakeConversationKey(aCharacterId, bCharacterId);
            var messages = await uow.PrivateMessages.FindAsync(m => m.ConversationKey == key);

            var query = messages.AsQueryable();
            if (beforeId.HasValue)
                query = query.Where(m => m.Id < beforeId.Value);

            return query.OrderByDescending(m => m.Id)
                       .Take(take)
                       .ToList();
        }

        // ========== 工具 ==========

        private static string MakeConversationKey(string a, string b)
        {
            var arr = new[] { a, b };
            Array.Sort(arr, StringComparer.Ordinal);
            return $"{arr[0]}#{arr[1]}";
        }
    }
}