using Server.DataBase.Entities;
using Server.Game.Contracts.Actor;
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

        // 玩家ID -> 该玩家对这件物品的选择
        public Dictionary<string, LootChoice> PlayerChoices { get; } = new();

        public List<int> RollPool { get; set; } = new(); // 预生成的无重复点数池
        private int nextIndex = 0;

        public int GetNextRoll() => RollPool[nextIndex++];

        public string WinnerPlayerId { get; set; }

        public bool AllPlayersDecided =>
            PlayerChoices.Values.All(c => c.ChoiceType != LootChoiceType.Pending);

        public bool HasAnyRoll =>
            PlayerChoices.Values.Any(c => c.ChoiceType == LootChoiceType.Rolled);
    }

    public class DungeonWorld : EntityWorld
    {
        private readonly List<DungeonLootEntry> pendingLoot = new();
        private float LimitTime;

        public DungeonWorld(EntityContext context, SkillSystem skill, BuffSystem buff, AreaBuffSystem areaBuff, AOIService aoi, NavVolumeService nav, AStarPathfind pathfinder) : base(context, skill, buff, areaBuff, aoi, nav, pathfinder)
        {
        }

        public override void OnTickUpdate(int tick, float deltaTime)
        {
            base.OnTickUpdate(tick, deltaTime);
            UpdateDungeonLimitTime(deltaTime);
        }

        protected override void HandleEntityDeath(string entityId, EntityType entityType)
        {
            if (!Context.TryGetEntity(entityId, out var entity)) return;

            var allPlayerIds = Context.Players;
            if (entity.Identity.Type == EntityType.Monster)
            {
                if (entity.TryGet<MonsterComponent>(out var monsterComp))
                {
                    if (monsterComp != null && monsterComp.Rank == MonsterRank.Boss)
                    {
                        var lootItems = GenerateDungeonLoot();

                        if (allPlayerIds.Count == 1)
                        {
                            var onlyPlayerId = allPlayerIds.First();
                            Console.WriteLine("只有一个玩家");
                            // 添加给玩家的背包
                            //var completedPayload = new ServerDungeonCompleted();
                            //regionState.GatewaySend.AddSendToPlayers(allPlayerIds, Protocol.DungeonCompleted, completedPayload);
                            Context.Actor.AddTell($"PlayerActor_{onlyPlayerId}", new ItemsAcquired(lootItems));
                            // 触发副本完成（注册销毁定时器）
                            HandleDungeonCompleted("副本通关了");
                        }
                        else if (allPlayerIds.Count > 1)
                        {
                            var entries = new List<DungeonLootEntry>();
                            foreach (var item in lootItems)
                            {
                                var entry = new DungeonLootEntry
                                {
                                    Item = item,
                                    RollPool = HelperUtility.ShuffleRollPool()
                                };

                                foreach (var playerId in allPlayerIds)
                                {
                                    entry.PlayerChoices[playerId] = new LootChoice();
                                }
                                entries.Add(entry);
                            }

                            pendingLoot.Clear();
                            pendingLoot.AddRange(entries);
                            Context.Gateway.AddSend(allPlayerIds, Protocol.DungeonLootInfo, lootItems);
                        }
                    }
                }
            }

            Context.RemoveEntity(entityId);
            Context.Gateway.AddSend(allPlayerIds, Protocol.EntityDespawn,
                new ServerEntityDespawn(Context.Tick, new HashSet<string> { entityId }));
        }

        public void HandleDungeonCompleted(string cause)
        {
            // 通知所有玩家离开副本
            foreach (var playerId in Context.Players)
            {

            }

            Context.WaitDestory.Add(Context.Id);
        }

        private void UpdateDungeonLimitTime(float deltaTime)
        {
            LimitTime -= deltaTime;
            if (LimitTime <= 0)
            {
                HandleDungeonCompleted("副本时间到了");
            }
        }

        private List<ItemData> GenerateDungeonLoot()
        {
            var item = new EquipData
            {
                ItemId = HelperUtility.GetKey(),
                ItemTemplateId = "0001",
                ItemName = "长剑",
                ItemType = ItemType.Equip,
                QuantityType = QuantityType.Common,
                Description = "这是一把长剑",
                Gold = 100,
                IsStack = false,
                ItemCount = 1,
                Health = 0,
                Mana = 0,
                AttackPower = 10,
                DefencePower = 0,
                SpellPower = 0,
                Level = 1,
                EquipType = EquipType.Weapon
            };
            return new List<ItemData> { item };
        }


        public void HandleLootChoice(string entityId, string itemId, bool isRoll)
        {
            var entry = pendingLoot.FirstOrDefault(l => l.Item.ItemId == itemId);
            if (entry == null) return;
            if (!Context.TryGetEntity(entityId, out var entity)) return;
            if (!entry.PlayerChoices.TryGetValue(entity.Profile.PlayerId, out var choice)) return;

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

            Context.Gateway.AddSend(
                Context.Players,
                Protocol.DungeonLootChoice,
                new ServerDungeonLootChoice
                {
                    EntityName = entity.Identity.Name,
                    LootChoiceType = choice.ChoiceType,
                    RollValue = choice.RollValue,
                    ItemId = itemId
                });

            if (entry.AllPlayersDecided)
            {
                ResolveSingleLoot(entry);
            }

            if (pendingLoot.All(l => l.WinnerPlayerId != null || !l.HasAnyRoll))
            {
                HandleDungeonCompleted("副本通关了");
            }
        }


        private void ResolveSingleLoot(DungeonLootEntry entry)
        {
            if (!entry.HasAnyRoll)
            {
                entry.WinnerPlayerId = string.Empty;
                return;
            }

            string winner = string.Empty;
            int bestRoll = int.MinValue;
            var allRolls = new Dictionary<string, int>();

            foreach (var (playerId, choice) in entry.PlayerChoices)
            {
                if (choice.ChoiceType != LootChoiceType.Rolled)
                    continue;

                allRolls[playerId] = choice.RollValue;

                if (choice.RollValue > bestRoll)
                {
                    bestRoll = choice.RollValue;
                    winner = playerId;
                }
            }

            entry.WinnerPlayerId = winner;

            if (!string.IsNullOrEmpty(winner))
            {
                Context.Actor.AddTell($"PlayerActor_{winner}", new ItemAcquired(entry.Item));
            }
        }

    }
}
