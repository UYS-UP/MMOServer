using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Network
{
    public interface ISessionRouter
    {
        // 账号登录时调用
        void RegisterAccount(string playerId, string sessionActorId);
        // 角色进入游戏时调用 【新增】
        void RegisterCharacter(string characterId, string sessionActorId);

        void UnregisterAccount(string playerId);
        void UnregisterCharacter(string characterId);

        // 两个获取接口
        string GetByPlayerId(string playerId);
        string GetByCharacterId(string characterId);
    }
}
