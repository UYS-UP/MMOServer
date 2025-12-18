using Server.DataBase.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Player
{
    public class FriendManager
    {
        private readonly Dictionary<string, Friend> cacheFriends = new();
        private readonly Dictionary<string, bool> onlineStatus = new Dictionary<string, bool>();

        public IReadOnlyDictionary<string, Friend> Friends => cacheFriends;
        public IReadOnlyDictionary<string, bool> OnlineStatus => onlineStatus;

        public void LoadFriends(List<Friend> friends)
        {
            
            friends.Clear();
            onlineStatus.Clear();

            foreach (var f in friends)
            {
                cacheFriends[f.FriendCharacterId] = f;
                onlineStatus[f.FriendCharacterId] = false; // 默认离线
            }
        }

        public bool AddFriend(Friend friend)
        {
            if (friend == null) return false;
            if (cacheFriends.ContainsKey(friend.FriendCharacterId)) return false;

            cacheFriends[friend.FriendCharacterId] = friend;
            onlineStatus[friend.FriendCharacterId] = false;
            return true;
        }

        public bool RemoveFirend(string friendCharacterId)
        {
            if(string.IsNullOrEmpty(friendCharacterId)) return false;
            cacheFriends.Remove(friendCharacterId);
            onlineStatus.Remove(friendCharacterId);
            return true;
        }

        public bool UpdateRemark(string friendCharacterId, string remark)
        {
            if (!cacheFriends.TryGetValue(friendCharacterId, out var friend)) return false;

            friend.Remark = remark;
            return true;
        }

        public void SetOnlineStatus(string friendCharacterId, bool isOnline)
        {
            if (onlineStatus.ContainsKey(friendCharacterId))
            {
                onlineStatus[friendCharacterId] = isOnline;
            }
        }

        public List<Friend> GetOnlineFriends()
        {
            return cacheFriends.Values
                .Where(f => onlineStatus.GetValueOrDefault(f.FriendCharacterId, false))
                .ToList();
        }

        public List<Friend> GetOfflineFriends()
        {
            return cacheFriends.Values
                .Where(f => !onlineStatus.GetValueOrDefault(f.FriendCharacterId, false))
                .ToList();
        }

        public List<Friend> GetAllFriends()
        {
            return cacheFriends.Values.ToList();
        }

        public bool IsFriend(string characterId)
        {
            return cacheFriends.ContainsKey(characterId);
        }

        public int Count => cacheFriends.Count;

        public void Clear()
        {
            cacheFriends.Clear();
            onlineStatus.Clear();
        }

    }
}
