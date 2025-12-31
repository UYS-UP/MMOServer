using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Contracts.Server;
using System.Numerics;

namespace Server.Game.Actor.Domain.ACharacter
{
    public record class A_LevelDungeon(string Reason) : IActorMessage;


    public record class A_MonsterKiller(string EntityId, string MonsterTemplateId, IReadOnlyList<ItemData> DroppedItems) : IActorMessage, IQuestEvent
    {
        public bool Match(QuestObjective obj) => obj.Type switch
        {
            ObjectiveType.KillMonster => obj.TargetId == MonsterTemplateId,
            ObjectiveType.CollectItem => DroppedItems.Any(i => i.TemplateId == obj.TargetId),
            _ => false
        };
    }

    public record class A_ItemAcquired(ItemData Item) : IActorMessage, IQuestEvent
    {
        public bool Match(QuestObjective obj) => obj.Type switch
        {
            ObjectiveType.CollectItem => obj.TargetId == Item.TemplateId,
            _ => false
        };
    }

    public record class A_ItemsAcquired(IReadOnlyList<ItemData> Items) : IActorMessage, IQuestEvent
    {
        public bool Match(QuestObjective obj) => obj.Type switch
        {
            ObjectiveType.CollectItem => Items.Any(i => i.TemplateId == obj.TargetId),
            _ => false
        };
    }
}
