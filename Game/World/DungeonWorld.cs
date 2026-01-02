using MessagePack;
using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.ACharacter;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.World.AStar;
using Server.Game.World.Services;
using Server.Game.World.Skill;
using Server.Game.World.Skill.Buff;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World
{
    public enum LootChoiceType
    {
        Pending = 0,     // 未选择
        Pass = 1,        // 放弃
        Rolled = 2       // 已ROLL点
    }

    public class LootChoice
    {
        public LootChoiceType ChoiceType { get; set; } = LootChoiceType.Pending;
        public int RollValue { get; set; } = 0; // 1-100
    }

    public class DungeonLootEntry
    {
        public ItemData Item { get; set; }

        // CharacterId -> 该玩家对这件物品的选择
        public Dictionary<string, LootChoice> CharacterChoices { get; } = new();

        public List<int> RollPool { get; set; } = new(); // 预生成的无重复点数池
        private int nextIndex = 0;

        public int GetNextRoll() => RollPool[nextIndex++];

        public string WinnerCharacter { get; set; }

        public bool AllPlayersDecided =>
            CharacterChoices.Values.All(c => c.ChoiceType != LootChoiceType.Pending);

        public bool HasAnyRoll =>
            CharacterChoices.Values.Any(c => c.ChoiceType == LootChoiceType.Rolled);
    }

    public class DungeonWorld : EntityWorld
    {
        private readonly List<DungeonLootEntry> pendingLoot = new();
        private float LimitTime;

        public DungeonWorld(
            ActorBase actor, 
            EntityContext context, 
            SkillSystem skill, 
            BuffSystem buff, 
            AreaBuffSystem areaBuff, 
            AOIService aoi, 
            NavVolumeService nav, 
            AStarPathfind pathfinder, 
            float limitTime) : 
            base(actor, context, skill, buff, areaBuff, aoi, nav, pathfinder)
        {
            LimitTime = limitTime;
        }

        public override async Task OnTickUpdate(int tick, float deltaTime)
        {
            await base.OnTickUpdate(tick, deltaTime);
            await UpdateDungeonLimitTime(deltaTime);
        }

        protected override async Task HandleEntityDeath(int entityId, EntityType entityType)
        {
            if (!Context.TryGetEntity(entityId, out var entity)) return;
            var despawnPayload = new ServerEntityDespawn(Context.Tick, new HashSet<int> { entityId });
            var despawnBytes = MessagePackSerializer.Serialize(despawnPayload);
            var allCharacters = Context.Characters;
            if (entity.Identity.Type == EntityType.Monster)
            {
                if (entity.TryGet<MonsterComponent>(out var monsterComp))
                {
                    if (monsterComp != null && monsterComp.Rank == MonsterRank.Boss)
                    {
                        var lootItems = GenerateDungeonLoot();

                        if (allCharacters.Count == 1)
                        {
                            var characterId = allCharacters.First();
                            Console.WriteLine("只有一个玩家");
                            await Actor.TellAsync(GameField.GetActor<CharacterActor>(characterId), new A_ItemAcquired(null));
                            await Actor.TellGateway(characterId, Protocol.SC_EntityDespawn, despawnBytes);
                            await HandleDungeonCompleted("副本通关了");
                        }
                        else if (allCharacters.Count > 1)
                        {
                            var entries = new List<DungeonLootEntry>();
                            foreach (var item in lootItems)
                            {
                                var entry = new DungeonLootEntry
                                {
                                    Item = item,
                                    RollPool = HelperUtility.ShuffleRollPool()
                                };

                                foreach (var playerId in allCharacters)
                                {
                                    entry.CharacterChoices[playerId] = new LootChoice();
                                }
                                entries.Add(entry);
                            }

                            pendingLoot.Clear();
                            pendingLoot.AddRange(entries);
                            var lootItemsBytes = MessagePackSerializer.Serialize(lootItems);
                            foreach (var characterId in allCharacters)
                            {
                                await Actor.TellGateway(characterId, Protocol.SC_DungeonLootInfo, lootItemsBytes);
                                await Actor.TellGateway(characterId, Protocol.SC_EntityDespawn, despawnBytes);
                            }
                           
                        }
                    }
                }
            }
            Context.RemoveEntity(entityId);
        }

        public async Task HandleDungeonCompleted(string cause)
        {
            // 通知所有玩家离开副本
            foreach (var charcterId in Context.Characters)
            {
                await Actor.TellAsync(GameField.GetActor<CharacterActor>(charcterId), new A_LevelDungeon("副本时间到了"));
            }
            Console.WriteLine("副本时间到了");
            Context.WaitDestory.Add(Context.Id);
        }

        private async Task UpdateDungeonLimitTime(float deltaTime)
        {
            LimitTime -= deltaTime;
            if (LimitTime <= 0)
            {
                await HandleDungeonCompleted("副本时间到了");
            }
        }

        private List<ItemData> GenerateDungeonLoot()
        {
            var item = new EquipData
            {
                TemplateId = "0001",
                ItemName = "长剑",
                ItemType = ItemType.Equip,
                QuantityType = QualityType.Common,
                Description = "这是一把长剑",
                Price = 100,
                IsStack = false,
                ItemCount = 1,
                EquipType = EquipType.Weapon
            };
            return new List<ItemData> { item };
        }


        public async Task HandleLootChoice(string characterId, string itemId, bool isRoll)
        {
            var entry = pendingLoot.FirstOrDefault(l => l.Item.TemplateId == itemId);
            if (entry == null) return;
            if (!Context.TryGetEntityByCharacterId(characterId, out var entity)) return;
            if (!entry.CharacterChoices.TryGetValue(characterId, out var choice)) return;

            if (isRoll)
            {
                choice.ChoiceType = LootChoiceType.Rolled;
                choice.RollValue = entry.GetNextRoll();
            }
            else
            {
                choice.ChoiceType = LootChoiceType.Pass;
                choice.RollValue = 0;
            }
            var payload = new ServerDungeonLootChoice
            {
                EntityName = entity.Identity.Name,
                LootChoiceType = choice.ChoiceType,
                RollValue = choice.RollValue,
                ItemId = itemId
            };
            var bytes = MessagePackSerializer.Serialize(payload);
            await Actor.TellGateway(characterId, Protocol.SC_DungeonLootChoice, bytes);
            if (entry.AllPlayersDecided)
            {
                await ResolveSingleLoot(entry);
            }

            if (pendingLoot.All(l => l.WinnerCharacter != null || !l.HasAnyRoll))
            {
                await HandleDungeonCompleted("副本通关了");
            }
        }


        private async Task ResolveSingleLoot(DungeonLootEntry entry)
        {
            if (!entry.HasAnyRoll)
            {
                entry.WinnerCharacter = string.Empty;
                return;
            }

            string winner = string.Empty;
            int bestRoll = int.MinValue;
            var allRolls = new Dictionary<string, int>();

            foreach (var (characterId, choice) in entry.CharacterChoices)
            {
                if (choice.ChoiceType != LootChoiceType.Rolled)
                    continue;

                allRolls[characterId] = choice.RollValue;

                if (choice.RollValue > bestRoll)
                {
                    bestRoll = choice.RollValue;
                    winner = characterId;
                }
            }

            entry.WinnerCharacter = winner;

            if (!string.IsNullOrEmpty(winner))
            {
                await Actor.TellAsync(GameField.GetActor<CharacterActor>(winner), new A_ItemAcquired(entry.Item));
            }
        }

    }
}
