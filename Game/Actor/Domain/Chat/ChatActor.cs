using MessagePack;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Team;
using Server.Game.Actor.Network;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Network;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public enum ChatType
{
    World,      // 世界聊天
    Region,     // 区域聊天
    Team,       // 队伍聊天
    System,     // 系统通知
    Private     // 私聊(好友聊天)
}

[MessagePackObject]
public class ChatMessageData
{
    [Key(0)] public string SenderId { get; set; }
    [Key(1)] public string SenderName { get; set; }
    [Key(2)] public ChatType Type { get; set; }
    [Key(3)] public string Content { get; set; }
    [Key(4)] public DateTime Timestamp { get; set; }
    [Key(5)] public string TargetId { get; set; }

    public ChatMessageData()
    {
        Timestamp = DateTime.UtcNow;
    }


}

namespace Server.Game.Actor.Domain.Chat
{
    public class ChatActor : ActorBase
    {
        private readonly Dictionary<ChatType, Queue<ChatMessageData>> chatHistory = new();

        private readonly Dictionary<string, HashSet<string>> RegionPlayers= new();
        private readonly Dictionary<int, HashSet<string>> TeamPlayers = new();

        private const int MaxHistorySize = 1000;

        private readonly Dictionary<string, DateTime> lastMessageTime = new Dictionary<string, DateTime>();
        private const double MinMessageInterval = 1.0f;

        public ChatActor(string actorId) : base(actorId)
        {
            chatHistory[ChatType.World] = new Queue<ChatMessageData>();
            chatHistory[ChatType.System] = new Queue<ChatMessageData>();
            chatHistory[ChatType.Team] = new Queue<ChatMessageData>();
            chatHistory[ChatType.Region] = new Queue<ChatMessageData>();
        }

        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case SendChatMessage sendChat:
                    await HandleSendChatMessage(sendChat);
                    break;
                case SystemNotification systemMessage:
                    await HandleSystemMessage(systemMessage);
                    break;

                case CharacterEnterRegion characterEnterRegion:
                    await HandleCharcaterEnterRegion(characterEnterRegion);
                    break;
                case CharacterLevelRegion characterLevelRegion:
                    await HandleCharacterLevelRegion(characterLevelRegion);
                    break;
                case CharacterEnterTeam characterEnterTeam:
                    await HandleCharacterEnterTeam(characterEnterTeam);
                    break;
                case CharacterLevelTeam characterLevelTeam:
                    await HandleCharacterLevelTeam(characterLevelTeam);
                    break;
            }
        }

        private Task HandleCharacterEnterTeam(CharacterEnterTeam message)
        {
            if (!TeamPlayers.TryGetValue(message.TeamId, out var value))
            {
                value = new HashSet<string>();
                TeamPlayers[message.TeamId] = value;
            }
            value.Add(message.PlayerId);
            return Task.CompletedTask;
        }

        private Task HandleCharacterLevelTeam(CharacterLevelTeam message)
        {
            if (TeamPlayers.TryGetValue(message.TeamId, out var value))
            {
                value.Remove(message.PlayerId);
            }
            return Task.CompletedTask;
        }

        private Task HandleCharcaterEnterRegion(CharacterEnterRegion message)
        {
            if (!RegionPlayers.TryGetValue(message.RegionId, out var value))
            {
                value = new HashSet<string>();
                RegionPlayers[message.RegionId] = value;
            }

            value.Add(message.PlayerId);
            return Task.CompletedTask;
        }

        private Task HandleCharacterLevelRegion(CharacterLevelRegion message)
        {
            if (RegionPlayers.TryGetValue(message.OldRegionId, out var oldPlayers))
            {
                oldPlayers.Remove(message.PlayerId);
                if (!RegionPlayers.TryGetValue(message.NewRegionId, out var newPlayers))
                {
                    newPlayers = new HashSet<string>();
                    RegionPlayers[message.NewRegionId] = newPlayers;
                }
                newPlayers.Add(message.PlayerId);
            }

            return Task.CompletedTask;
        }

        private async Task HandleSendChatMessage(SendChatMessage message)
        {
            if (!CheckMessageFrequency(message.PlayerId))
            {
                Console.WriteLine($"[ChatActor] 玩家 {message.PlayerId} 发送消息过于频繁");
                return;
            }
            var chatData = new ChatMessageData
            {
                SenderId = message.PlayerId,
                SenderName = message.CharacterName,
                Type = message.Type,
                Content = message.Content,
                TargetId = message.TargetId,
                Timestamp = DateTime.UtcNow
            };
            SaveToHistory(chatData);

            switch (message.Type)
            {
                case ChatType.World:
                    await RouteWorldChat(chatData);
                    break;

                case ChatType.Region:
                    await RouteRegionChat(chatData);
                    break;
                case ChatType.Team:
                    await RouteTeamChat(chatData);
                    break;
                case ChatType.Private:
                    await RoutePrivateChat(chatData);
                    break;
            }
        }

        private async Task HandleSystemMessage(SystemNotification notification)
        {
            var chatData = new ChatMessageData
            {
                SenderId = "System",
                SenderName = "System",
                Type = ChatType.System,
                Content = notification.Content,
                Timestamp = DateTime.UtcNow
            };
            SaveToHistory(chatData);
            if (notification.TargetPlayers != null && notification.TargetPlayers.Count > 0)
            {
                await TellAsync(nameof(NetworkGatewayActor), new SendToPlayers(notification.TargetPlayers, Protocol.ChatMessage, chatData));
            }
            else
            {
                await RouteWorldChat(chatData);
            }
              
        }

        private async  Task RouteWorldChat(ChatMessageData data)
        {
            foreach(var kv in RegionPlayers)
            {
                await TellGateway(new SendToPlayers(kv.Value, Protocol.ChatMessage, data));
            }
        }

        private async Task RouteRegionChat(ChatMessageData data)
        {
            if(!RegionPlayers.TryGetValue(data.TargetId, out var value)) return;
            await TellGateway(new SendToPlayers(value, Protocol.ChatMessage, data));

        }
        
        private async Task RouteTeamChat(ChatMessageData data)
        {
            if (!TeamPlayers.TryGetValue(int.Parse(data.TargetId), out var value)) return;
            await TellGateway(new SendToPlayers(value, Protocol.ChatMessage, data));
        }

        private async Task RoutePrivateChat(ChatMessageData data)
        {
            
        }

        private void SaveToHistory(ChatMessageData chatData)
        {

            if(chatHistory.TryGetValue(chatData.Type, out var history))
            {
                history.Enqueue(chatData);

                // 限制历史记录大小
                while (history.Count > MaxHistorySize)
                {
                    history.Dequeue();
                }
            }
            


        }

        private bool CheckMessageFrequency(string playerId)
        {
            var now = DateTime.UtcNow;

            if (lastMessageTime.TryGetValue(playerId, out var lastTime))
            {
                var interval = (now - lastTime).TotalSeconds;
                if (interval < MinMessageInterval)
                {
                    return false;
                }
            }

            lastMessageTime[playerId] = now;
            return true;
        }
    }
}
