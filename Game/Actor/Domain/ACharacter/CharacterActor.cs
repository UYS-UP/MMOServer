using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.ASession;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Utility;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;


namespace Server.Game.Actor.Domain.ACharacter
{

    /// <summary>
    /// 主要处理单个玩家操作
    /// </summary>
    public partial class CharacterActor : ActorBase
    {
        private PlayerActorState state;

        private ActorEventBus EventBus => System.EventBus;

        private readonly StorageManager storage;
        private readonly QuestManager quest;
        private readonly AttributeManager attribute;

        private class PlayerActorState
        {
            public string PlayerId { get; set; }
            public string CharacterId {  get; set; }
            public int EntityId { get; set; }
            public int MapId { get; set; }
            public int DungeonId { get; set; }

            public Vector3 Position { get; set; }
            public float Yaw { get; set; }
            public long Exp { get; set; }
            public float Hp { get; set; }

            public string Name { get; set; }
            public int Level { get; set; }

            public Dictionary<AttributeType, float> BaseAttributes { get; set; }
            public Dictionary<AttributeType, float> ExtraAttributes { get; set; }

            public PlayerActorState(Character dbChar)
            {
                PlayerId = dbChar.PlayerId;
                CharacterId = dbChar.CharacterId;
                Name = dbChar.Name;
                Level = dbChar.Level;
                Exp = dbChar.Exp;
                Hp = dbChar.Hp;
                BaseAttributes = dbChar.Attributes;
                EntityId = Counter.NextId();
                MapId = dbChar.MapId;
                Position = new Vector3(dbChar.X, dbChar.Y, dbChar.Z);
                Yaw = dbChar.Yaw;
                DungeonId = -1;

            }
            
        }

        public CharacterActor(string actorId, Character character) : base(actorId)
        {
        

            attribute = new AttributeManager();
            storage = new StorageManager();
            quest = new QuestManager();
            state = new PlayerActorState(character);
   
            state.ExtraAttributes = attribute.CalculateCharacterAttributes(character);


            var questNodes = new Dictionary<string, QuestNode>();
            questNodes.Add("001_01", new QuestNode
            {
                NodeId = "001_01",
                QuestName = "击杀怪物",
                Description = "击杀怪物1只怪物",
                NextNodeIds = new List<string> { "001_02" },
                Objectives = new List<QuestObjective>()
                {
                    new QuestObjective
                    {
                        Type = ObjectiveType.KillMonster,
                        TargetId = "monster_001",
                        RequireCount = 1,
                        CurrentCount = 0,

                    },
                },
            });
            questNodes.Add("001_02", new QuestNode
            {
                NodeId = "001_02",
                QuestName = "提交任务",
                Description = "与NPC交谈",
                Objectives = new List<QuestObjective>()
                {
                    new QuestObjective
                    {
                        Type = ObjectiveType.SubmitToNpc,
                        TargetId = "npc_001",
                        RequireCount = 1,
                        CurrentCount = 0,

                    },
                },
            });
            quest.LoadQuestChainConfig(questNodes);
            quest.AcceptChain("001_01");

        }

        protected override async Task OnStart()
        {
            await base.OnStart();
            quest.OnActivateNode += OnActivateNode;
            quest.OnQuestCompleted += OnQuestCompleted;

            var sessionActor = System.SessionRouter.GetByPlayerId(state.PlayerId);
            await TellAsync(sessionActor, new BindCharacterIdAndEntityId(state.CharacterId, state.EntityId));
            await TellGateway(state.PlayerId, 
                Protocol.SC_EnterGame, 
                new ServerEnterGame { MapId = state.MapId, CharacterId = state.CharacterId });
        }



        protected override async Task OnStop()
        {
            quest.OnActivateNode -= OnActivateNode;
            quest.OnQuestCompleted -= OnQuestCompleted;
         
            await base.OnStop();
        }


        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case CS_CharacterEnterRegion cs_CharacterEnterRegion:
                    await CS_HandleCharacterEnterRegion(cs_CharacterEnterRegion);
                    break;
                case CS_CharacterEnterDungeon cs_ChacaterEnterDungeon:
                    await CS_HandleCharacterEnterDungeon(cs_ChacaterEnterDungeon);
                    break;
                case CS_CharacterChangeRegion cs_CharacterChangeRegion:
                    await CS_HandleCharacterChangeRegion(cs_CharacterChangeRegion);
                    break;
                case CS_CharacterLevelDungeon cs_CharacterLevelDungeon:
                    await CS_HandleCharacterLevelDungeon(cs_CharacterLevelDungeon);
                    break;

                case CS_QueryInventory cs_QueryInventory:
                    await CS_HandleQueryInventory(cs_QueryInventory);
                    break;
                case CS_SwapStorageSlot cs_SwapInventorySlot:
                    await CS_HandleSwapStorageSlot(cs_SwapInventorySlot);
                    break;


                case GM_AddItem gm_AddItem:
                    await GM_HandleAddItem(gm_AddItem);
                    break;

                case A_LevelDungeon levelDungeon:
                    await HandleLevelDungeon(levelDungeon);
                    break;

                case A_MonsterKiller monsterKiller:
                    await HandleMonsterKiller(monsterKiller);
                    break;
                case A_ItemAcquired itemAcquired:
                    await HandleItemAcquired(itemAcquired);
                    break;
                case A_ItemsAcquired itemsAcquired:
                    await HandlItemsAcquired(itemsAcquired);
                    break;

  
                
             
            }
        }


        private async void OnActivateNode(QuestNode node)
        {
            await TellGateway(state.PlayerId, Protocol.SC_QuestAccepted, node);
        }

        private async void OnQuestCompleted(string nodeId)
        {
            await TellGateway(state.PlayerId, Protocol.SC_QuestCompleted, nodeId);
        }


        private async Task<EntityRuntime> CreateRuntimeFromState()
        {
            var entity = new EntityRuntime();
            var identity = new IdentityComponent
            {
                EntityId = state.EntityId,
                CharacterId = state.CharacterId,
                Name = state.Name,
                Type = EntityType.Character,
                TemplateId = string.Empty,
            };

            var kinematics = new KinematicsComponent
            {
                Position = state.Position,
                Yaw = state.Yaw,
                Direction = Vector3.Zero,
                Speed = 4,
                State = EntityState.Idle
            };

            

            var stats = new StatsComponent
            {
                Level = state.Level,
                CurrentHp = state.Hp,
                CurrentEx = state.Exp,
                
                BaseStats = new Dictionary<AttributeType, float>(state.BaseAttributes),
                ExtraAttributes = new Dictionary<AttributeType, float>(state.ExtraAttributes)
            };

            var skillBook = new SkillBookComponent();
            for (int i = 0; i < 3; i++)
            {
                skillBook.Skills[i] = new SkillRuntime();
            }

            var worldRef = new WorldRefComponent
            {
                MapId = state.MapId,
                DungeonId = state.DungeonId,
                SpawnPoint = Vector3.Zero
            };

            entity.Add(worldRef);
            entity.Add(skillBook);
            entity.Add(stats);
            entity.Add(kinematics);
            entity.Add(identity);
            var sessionActor = System.SessionRouter.GetByPlayerId(state.PlayerId);
           
            await TellAsync(sessionActor, new CharacterWorldSync(state.MapId, state.DungeonId));
            return entity;
        }
    }



}



