using Server.DataBase.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Network
{
    public class SessionRouter : ISessionRouter
    {
        private readonly ConcurrentDictionary<string, string> playerIdRouter = new();
        private readonly ConcurrentDictionary<string, string> characterIdRouter = new();

        public void RegisterAccount(string playerId, string sessionActorId)
        {
            playerIdRouter[playerId] = sessionActorId;
        }

        public void RegisterCharacter(string characterId, string sessionActorId)
        {
            characterIdRouter[characterId] = sessionActorId;
        }

        public void UnregisterAccount(string playerId)
        {
            playerIdRouter.TryRemove(playerId, out _);
        }

        public void UnregisterCharacter(string characterId)
        {
            characterIdRouter.TryRemove(characterId, out _);
        }

        public string GetByPlayerId(string playerId)
        {
            if (playerIdRouter.TryGetValue(playerId, out var actorId))
            {
                return actorId;
            }
            return null;
        }

        public string GetByCharacterId(string characterId)
        {
            if (characterIdRouter.TryGetValue(characterId, out var actorId))
            {
                return actorId;
            }
            return null;
        }

        public bool IsOnline(string playerId)
        {
            return playerIdRouter.ContainsKey(playerId);
        }
    }
}
