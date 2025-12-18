using NPOI.Util;
using Org.BouncyCastle.Ocsp;
using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Chat;
using Server.Game.Actor.Domain.Team;
using Server.Game.Actor.Network;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Utility;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;


namespace Server.Game.Actor.Domain.Player
{

    /// <summary>
    /// 主要处理单个玩家操作
    /// </summary>
    public class PlayerActor : ActorBase
    {
        private string playerId;

        private ActorEventBus bus;
        private CharacterEntitySnapshot character;
        private RegionPlayerSnpot regionPlayer;
        private TeamSnpot team;

        private readonly StorageManager storage;
        private readonly FriendManager friend;
        private readonly QuestManager quest;

        public PlayerActor(string actorId, string playerId, ActorEventBus bus) : base(actorId)
        {
            this.bus = bus;
            this.playerId = playerId;
            storage = new StorageManager();
            quest = new QuestManager();
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

        protected override void OnStart()
        {
            base.OnStart();
            quest.OnActivateNode += OnActivateNode;
            quest.OnQuestCompleted += OnQuestCompleted;
            bus.Subscribe<CharacterEntitySnapshot>(ActorId);
        }

        private async void OnActivateNode(QuestNode node)
        {
            await TellGateway(new SendToPlayer(playerId, Protocol.QuestAccept, node));
        }

        private async void OnQuestCompleted(string nodeId)
        {
            await TellGateway(new SendToPlayer(playerId, Protocol.QuestCompleted, nodeId));
        }

        protected override void OnStop()
        {
            quest.OnActivateNode -= OnActivateNode;
            // _ = DatabaseService.CharacterService.UpdateCharacterWorldPositionAsync(character.EntityId, character.RegionId, "main");
            base.OnStop();
        }


        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case PlayerEnterGameRequest playerEnterGame:
                    await HandlePlayerEnterGame(playerEnterGame);
                    break;
                case PlayerMoveRequest playerMove:
                    await HandlePlayerMove(playerMove);
                    break;
                case PlayerQueryInventoryRequest playerQueryInventory:
                    await HandlePlayerQueryInventory(playerQueryInventory);
                    break;
                case PlayerSwapStorageSlotRequest playerSwapInventorySlot:
                    await HandlePlayerSwapInventorySlot(playerSwapInventorySlot);
                    break;
                case PlayerCreateDungeonTeamRequest playerCreateDungeonTeam:
                    await HandlePlayerCreateDungeonTeam(playerCreateDungeonTeam);
                    break;
                case PlayerStartDungeonRequest playerStartDungeon:
                    await HandlePlayerStartDungeon(playerStartDungeon);
                    break;

                case PlayerInviteRegionCharacterRequest inviteRegionCharacter:
                    await HandlePlayerInviteRegionCharacter(inviteRegionCharacter);
                    break;
                case PlayerAcceptTeamInviteRequest acceptTeamInvite:
                    await HandlePlayerAcceptTeamInvite(acceptTeamInvite);
                    break;
                case PlayerSkillReleaseRequest playerSkillRelease:
                    await HandlePlayerSkillRelease(playerSkillRelease);
                    break;
                case PlayerDungeonLootChoiceRequest playerDungeonLootChoiceRequest:
                    await HandlePlayerDungeonLootChoice(playerDungeonLootChoiceRequest);
                    break;
                case PlayerAddFriendRequest playerAddFriend:
                    await HandlePlayerAddFriend(playerAddFriend);
                    break;
                case PlayerAddFriendGroupRequest playerAddFriendGroupRequest:
                    await HandlePlayerAddFriendGroup(playerAddFriendGroupRequest);
                    break;
                case PlayerAlterFriendGroupRequest playerAlterFriendGroupRequest:
                    await HandlePlayerAlterFriendGroup(playerAlterFriendGroupRequest);
                    break;
                case PlayerMoveFriendToGroupRequest playerMoveFriendToGroupRequest:
                    await HandlePlayerMoveFriendToGroup(playerMoveFriendToGroupRequest);
                    break;
                case PlayerHandleAddFriendRequest playerHandleAddFriendRequest:
                    await HandlePlayerHandleAddFriend(playerHandleAddFriendRequest);
                    break;
                case PlayerDeleteFriendRequest playerDeleteFriendRequest:
                    await HandleDeleteFriendRequest(playerDeleteFriendRequest);
                    break;
                case PlayerAlterFriendRemarkRequest playerAlterFriendRemarkRequest:
                    await HandlePlayerAlterFriendRemark(playerAlterFriendRemarkRequest);
                    break;
                case PlayerFriendChatRequest playerFriendChatRequest:
                    await HandlePlayerFriendChat(playerFriendChatRequest);
                    break;
                case PlayerInviteFriendRequest playerInviteFriendRequest:
                    await HandlePlayerInviteFriend(playerInviteFriendRequest);
                    break;

                // 进出副本/区域相关客户端请求
                case PlayerEnterRegionRequest playerEnterRegionRequest:
                    await HandlePlayerEnterRegion(playerEnterRegionRequest);
                    break;
                case PlayerEnterDungeonRequest playerEnterDungeonRequest:
                    await HandlePlayerEnterDungeon(playerEnterDungeonRequest);
                    break;
                case PlayerLevelRegionRequest playerLevelRegionRequest:
                    await HandlePlayerLevelRegion(playerLevelRegionRequest);
                    break;
                case PlayerLevelDungeonRequest playerLevelDungeonRequest:
                    await HandlePlayerLevelDungeon(playerLevelDungeonRequest);
                    break;

                // 进出副本/区域相关Actor通信
                case LevelDungeon levelDungeon:
                    await HandleLevelDungeon(levelDungeon);
                    break;
                case LoadDungeon levelRegion:
                    await HandleLoadDungeon(levelRegion);
                    break;
                case EnterDungeon enterDungeon:
                    await HandleEnterDungeon(enterDungeon);
                    break;

                case AddFriendRequest addFriendRequest:
                    await HandleAddFriendRequest(addFriendRequest);
                    break;
                case AddFriend addFriend:
                    await HandleAddFriend(addFriend);
                    break;
                case MonsterKiller monsterKiller:
                    await HandleMonsterKiller(monsterKiller);
                    break;
                case ItemAcquired itemAcquired:
                    await HandleItemAcquired(itemAcquired);
                    break;
                case ItemsAcquired itemsAcquired:
                    await HandlItemsAcquired(itemsAcquired);
                    break;


                case CharacterEntitySnapshot characterEntitySnpot:
                    character = characterEntitySnpot;
                    break;
                case RegionPlayerSnpot regionPlayerSnpot:
                    regionPlayer = regionPlayerSnpot;
                    break;
                case TeamSnpot teamSnpot:
                    team = teamSnpot;
                    break;
                
                



            }
        }





        #region ClientRequest


        private async Task HandlePlayerDungeonLootChoice(PlayerDungeonLootChoiceRequest message)
        {
            await TellAsync($"RegionActor_{character.RegionId}", new DungeonLootChoice(character.DungeonId, character.EntityId, message.ItemId, message.IsRoll));
        }

        private async Task HandlePlayerEnterGame(PlayerEnterGameRequest message)
        {
            Console.WriteLine("进入游戏");
            var groups = await DatabaseService.FriendService.GetFriendGroupListAsync(message.CharacterId);
            var friendRequests = await DatabaseService.FriendService.GetPendingRequestsAsync(message.CharacterId);
            var friends = await DatabaseService.FriendService.GetFriendListAsync(message.CharacterId);

            List<NetworkFriendGroupData> networkGroups = new List<NetworkFriendGroupData>();
            foreach (var g in groups)
            {
                networkGroups.Add(new NetworkFriendGroupData { GroupId = g.Id, GroupName = g.Name });
            }
            List<NetworkFriendRequestData> networkFriendRequests = new List<NetworkFriendRequestData>();
            foreach (var fr in friendRequests)
            {
                var fromCharacter = await DatabaseService.CharacterService.GetCharacterByIdAsync(fr.FromCharacterId);
                networkFriendRequests.Add(new NetworkFriendRequestData { RequestId = fr.Id, SenderName = fromCharacter.Data.CharacterName, Remark = fr.Message });
            }
            List<NetworkFriendData> networkFriends = new List<NetworkFriendData>();
            foreach (var f in friends)
            {
                var fromCharacter = await DatabaseService.CharacterService.GetCharacterByIdAsync(f.FriendCharacterId);
                networkFriends.Add(new NetworkFriendData { CharacterId = fromCharacter.Data.CharacterId, CharacterName = fromCharacter.Data.CharacterName, Type = fromCharacter.Data.Profession, Avatar = "", Level = fromCharacter.Data.Level, GroupId = f.FriendGroupId });
            }

            var resp = await DatabaseService.CharacterService.GetCharacterByIdAsync(message.CharacterId);
            if (resp.Code != StateCode.Success) return;
            var role = resp.Data;
            var skills = new Dictionary<int, SkillRuntime>();
            var skill1 = new SkillRuntime();
            skill1.AddComponent(new SkillMetaComponent
            {
                SkillId = 0,
                Name = "Attack01",
                Description = "普通攻击",
                CurrentLevel = 1,
                MaxLevel = 1,
                GrowthFactor = 1,
                Cooldown = 1f,
                CooldownRemaining = 0f,
                ManaCost = 0,
                Type = SkillType.AreaDamage
            });

            var skill2 = new SkillRuntime();
            skill2.AddComponent(new SkillMetaComponent
            {
                SkillId = 1,
                Name = "Attack02",
                Description = "普通攻击",
                CurrentLevel = 1,
                MaxLevel = 1,
                GrowthFactor = 1,
                Cooldown = 0.7f,
                CooldownRemaining = 0f,
                ManaCost = 0,
                Type = SkillType.AreaDamage
            });

            var skill3 = new SkillRuntime();
            skill3.AddComponent(new SkillMetaComponent
            {
                SkillId = 2,
                Name = "Attack03",
                Description = "普通攻击",
                CurrentLevel = 1,
                MaxLevel = 1,
                GrowthFactor = 1,
                Cooldown = 2.8f,
                CooldownRemaining = 0f,
                ManaCost = 0,
                Type = SkillType.AreaDamage
            });

            skills.Add(0, skill1);
            skills.Add(1, skill2);
            skills.Add(2, skill3);
            await TellGateway(new SendToPlayer(role.PlayerId, Protocol.FriendListSync, new ServerFriendListSync(networkGroups, networkFriendRequests, networkFriends)));
            await TellGateway(new SendToPlayer(role.PlayerId, Protocol.QuestListSync, new ServerQuestListSync(quest.GetActiveQuests())));
            await TellAsync($"RegionActor_{role.Entity.RegionId}", new CharacterSpawn(
                role.Entity.EntityId, role.Entity.EntityType, "", role.CharacterName,
                new Vector3(role.Entity.X, role.Entity.Y, role.Entity.Z), role.Entity.Yaw,
                5, role.Level, role.HP, role.MaxHp, role.MP, role.MaxMp, role.EX, role.MaxEx, 5, 5,
                1.5f, skills, role.Entity.RegionId, string.Empty, role.Profession,
                role.PlayerId, role.CharacterId));

            
        }

        
 
        private async Task HandlePlayerMove(PlayerMoveRequest message)
        {

            await TellAsync($"RegionActor_{character.RegionId}",new CharacterMove(
                message.ClientTick, character.EntityId,
                message.Position, message.Yaw, message.Direction,
                character.RegionId, character.DungeonId));
        }

        private async Task HandlePlayerQueryInventory(PlayerQueryInventoryRequest message)
        {
            var payload = new ServerQueryInventory(storage.MaxInventorySize, storage.GetRangeSlotItems(SlotContainerType.Inventory, message.StartSlot, message.EndSlot), storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory));
            await TellGateway(new SendToPlayer(character.PlayerId, Protocol.QueryInventory, payload));
        }

        private async Task HandlePlayerSwapInventorySlot(PlayerSwapStorageSlotRequest message)
        {
            var result = storage.SwapItems(message.Slot1, message.Slot2);
            ItemData item1 = null;
            ItemData item2 = null;
            if (!result)
            {
                storage.TryGetItem(message.Slot1, out item1);
                storage.TryGetItem(message.Slot2, out item2);
            }
            var payload = new ServerSwapStorageSlotResponse(message.ReqId, result, item1, item2);
            await TellGateway(new SendToPlayer(character.PlayerId, Protocol.SwapStorageSlot, payload));
        }


        private async Task HandlePlayerCreateDungeonTeam(PlayerCreateDungeonTeamRequest message)
        {
            await TellAsync(nameof(TeamActor), new CreateDungeonTeam(
                character.PlayerId, character.CharacterId, character.CharacterName,
                character.CharacterLevel, message.TemplateId, message.TeamName));
        }

        private async Task HandlePlayerStartDungeon(PlayerStartDungeonRequest message)
        {
            await TellAsync(nameof(TeamActor), new StartDungeon(message.TeamId));
        }

        private async Task HandlePlayerEnterDungeon(PlayerEnterDungeonRequest message)
        {
             await TellAsync(nameof(TeamActor), new LoadedDungeon(team.TeamId, playerId));
        }

        private async Task HandlePlayerEnterRegion(PlayerEnterRegionRequest message)
        {
            var resp = await DatabaseService.CharacterService.GetCharacterByIdAsync(character.CharacterId);
            if (resp.Code != StateCode.Success) return;
            var role = resp.Data;
            var skills = new Dictionary<int, SkillRuntime>();
            var skill1 = new SkillRuntime();
            skill1.AddComponent(new SkillMetaComponent
            {
                SkillId = 0,
                Name = "Attack",
                Description = "普通攻击",
                CurrentLevel = 1,
                MaxLevel = 1,
                GrowthFactor = 1,
                Cooldown = 2f,
                CooldownRemaining = 0f,
                ManaCost = 0,
                Type = SkillType.AreaDamage
            });
            var skill2 = new SkillRuntime();
            skill2.AddComponent(new SkillMetaComponent
            {
                SkillId = 1,
                Name = "Dash",
                Description = "冲刺",
                CurrentLevel = 1,
                MaxLevel = 1,
                GrowthFactor = 1,
                Cooldown = 1f,
                CooldownRemaining = 0f,
                ManaCost = 0,
                Type = SkillType.AreaHeal
            });

            var skill3 = new SkillRuntime();
            skill3.AddComponent(new SkillMetaComponent
            {
                SkillId = 3,
                Name = "点燃",
                Description = "对一个目标造成伤害",
                CurrentLevel = 1,
                MaxLevel = 1,
                GrowthFactor = 1,
                Cooldown = 10f,
                CooldownRemaining = 0f,
                ManaCost = 10,
                Type = SkillType.AreaHeal
            });

            skills.Add(0, skill1);

            skills.Add(1, skill2);
            skills.Add(3, skill3);
            await TellAsync($"RegionActor_{role.Entity.RegionId}", new CharacterSpawn(
                role.Entity.EntityId, role.Entity.EntityType, "", role.CharacterName,
                new Vector3(role.Entity.X, role.Entity.Y, role.Entity.Z), role.Entity.Yaw,
                5, role.Level, role.HP, role.MaxHp, role.MP, role.MaxMp, role.EX, role.MaxEx, 5, 5,
                1.5f, skills, role.Entity.RegionId, string.Empty, role.Profession,
                role.PlayerId, role.CharacterId));
        }


        private async Task HandlePlayerInviteRegionCharacter(PlayerInviteRegionCharacterRequest message)
        {
            foreach(var playerId in regionPlayer.RegionPlayers)
            {
                if(playerId == this.playerId) continue;
                await TellGateway(new SendToPlayer(playerId, Protocol.InvitePlayer, new ServerDungeonTeamInvite(team.TeamId,
                    $"[{team.TeamName}]{character.CharacterName}邀请你加入队伍")));
            }
        }

        private async Task HandlePlayerAcceptTeamInvite(PlayerAcceptTeamInviteRequest message)
        {
            Console.WriteLine("玩家接收了队伍邀请");
            await TellAsync(nameof(TeamActor),
                new EnterTeam(message.TeamId, character.CharacterName,
                character.PlayerId, character.CharacterId, character.CharacterLevel));
        }

        private async Task HandlePlayerSkillRelease(PlayerSkillReleaseRequest message)
        {
            var actor = $"RegionActor_{character.RegionId}";
            if (!string.IsNullOrEmpty(character.DungeonId)) actor = "DungeonActor";
            await TellAsync(actor, new CharacterSkillRelease(message.ClientTick, 
                message.SkillId, character.EntityId, character.DungeonId, message.InputType, message.TargetPosition, message.TargetDirection, message.TargetEntityId));
        }

        private async Task HandlePlayerAddFriend(PlayerAddFriendRequest message)
        {
            var result = await DatabaseService.CharacterService.GetCharacterByNameAsync(message.CharacterName);

            if (result.Code == StateCode.Success)
            {
                var actor = GameField.GetActor<PlayerActor>(result.Data.PlayerId);
                var (ok, requestId, msg) = await DatabaseService.FriendService.CreateFriendRequestAsync(character.CharacterId, result.Data.CharacterId, "你好!");
                await TellGateway(new SendToPlayer(character.CharacterId, Protocol.AddFriend, new ServerAddFriend(ok, msg)));
                if (System.IsActorAlive(actor))
                {
                    await TellAsync(actor, new AddFriendRequest(requestId, character.CharacterName, message.CharacterName, "你好!"));
                }
            }
            
        }

        private async Task HandlePlayerInviteFriend(PlayerInviteFriendRequest message)
        {
            throw new NotImplementedException();
        }

        private async Task HandlePlayerFriendChat(PlayerFriendChatRequest message)
        {
            throw new NotImplementedException();
        }

        private async Task HandlePlayerAlterFriendRemark(PlayerAlterFriendRemarkRequest message)
        {
            throw new NotImplementedException();
        }

        private async Task HandleDeleteFriendRequest(PlayerDeleteFriendRequest message)
        {
            throw new NotImplementedException();
        }

        private async Task HandlePlayerHandleAddFriend(PlayerHandleAddFriendRequest message)
        {
            var (ok, fromFriend, toFriend, fromCharacter, toCharacter) = await DatabaseService.FriendService.HandleFriendRequestAsync(message.RequestId, message.Accept);
            if (ok)
            {
                if (message.Accept)
                {
                    var a = new NetworkFriendData
                    {
                        Avatar = "",
                        CharacterId = fromCharacter.CharacterId,
                        CharacterName = fromCharacter.CharacterName,
                        Type = fromCharacter.Profession,
                        Level = fromCharacter.Level,
                        GroupId = fromFriend.FriendGroupId,
                    };

                    var b = new NetworkFriendData
                    {
                        Avatar = "",
                        CharacterId = toCharacter.CharacterId,
                        CharacterName = toCharacter.CharacterName,
                        Type = toCharacter.Profession,
                        Level = toCharacter.Level,
                        GroupId = toFriend.FriendGroupId,
                    };


                    await TellGateway(new SendToPlayer(character.PlayerId, Protocol.HandleFriendRequest, a));

                    if (System.IsActorAlive($"PlayerActor_{fromCharacter.PlayerId}"))
                    {
                        await TellAsync($"PlayerActor_{fromCharacter.PlayerId}", new AddFriend(character.CharacterId, b));
                    }
                }
            }
        }

        private async Task HandlePlayerMoveFriendToGroup(PlayerMoveFriendToGroupRequest message)
        {
            var reuslt = await DatabaseService.FriendService.MoveFriendToGroupAsync(character.CharacterId, message.CharacterId, message.GroupName);
        }

        private async Task HandlePlayerAlterFriendGroup(PlayerAlterFriendGroupRequest message)
        {
            var result = await DatabaseService.FriendService.RenameGroupAsync(message.GroupId, message.NewName);
        }

        private async Task HandlePlayerAddFriendGroup(PlayerAddFriendGroupRequest message)
        {
            var group = await DatabaseService.FriendService.CreateGroupAsync(character.CharacterId, message.GroupName);
            // 发送给玩家
        }
        #endregion


        private async Task HandleEnterDungeon(EnterDungeon message)
        {
            if(!RegionTemplateConfig.TryGetDungeonTemplateById(message.TemplateId, out var dungeonTemplate));
            var skills = new Dictionary<int, SkillRuntime>();
            var skill1 = new SkillRuntime();
            skill1.AddComponent(new SkillMetaComponent
            {
                SkillId = 0,
                Name = "Attack",
                Description = "普通攻击",
                CurrentLevel = 1,
                MaxLevel = 1,
                GrowthFactor = 1,
                Cooldown = 2f,
                CooldownRemaining = 0f,
                ManaCost = 0,
                Type = SkillType.AreaDamage
            });

            skills.Add(0, skill1);
            Console.WriteLine("玩家进入副本了");
            await TellAsync($"DungeonActor", new CharacterSpawn(
                character.EntityId, character.Type, "", character.CharacterName,
                dungeonTemplate.EntryPosition, 0,
                5, 1, 100, 100, 100, 100, 0, 100, 5, 5,
                1.5f, skills, character.RegionId, character.DungeonId, character.Profession,
                character.PlayerId, character.CharacterId));
        }

        private async Task HandlePlayerLevelDungeon(PlayerLevelDungeonRequest request)
        {
            character = character with { DungeonId = string.Empty };

            await TellGateway(new SendToPlayer(playerId, Protocol.LevelDungeon, new ServerLevelDungeon
            {
                Cause = string.Empty,
                RegionId = character.RegionId,
            }));
        }

        private async Task HandlePlayerLevelRegion(PlayerLevelRegionRequest request)
        {
            await TellAsync($"RegionActor_{character.RegionId}",
                new CharacterDespawn(character.EntityId, character.DungeonId));
            if (!RegionTemplateConfig.TryGetRegionTemplateById(request.RegionId, out var regionTemplate)) return;
            await DatabaseService.CharacterService.UpdateCharacterWorldPositionAsync(character.EntityId, request.RegionId, regionTemplate.EntryPosition);

            await TellGateway(new SendToPlayer(playerId, Protocol.LevelRegion,
                new ServerLevelRegion { RegionId = request.RegionId }));
        }

        private async Task HandleLevelDungeon(LevelDungeon message)
        {
            character = character with { DungeonId = string.Empty };
            await TellGateway(new SendToPlayer(playerId, Protocol.LevelDungeon, new ServerLevelDungeon
            {
                Cause = message.Cause,
                RegionId = character.RegionId,
            }));
        }

        private async Task HandleLoadDungeon(LoadDungeon message)
        {
            await TellAsync($"RegionActor_{character.RegionId}", 
                new CharacterDespawn(character.EntityId, character.DungeonId));

            character = character with { DungeonId = message.DungeonId };

            await TellGateway(new SendToPlayer(playerId, Protocol.LoadDungeon,
                new ServerLoadDungeon { TemplateId = message.TemplateId }));
        }


        private async Task HandleMonsterKiller(MonsterKiller message)
        {
            // 1. 加入背包
            Dictionary<SlotKey, ItemData> itmes = new Dictionary<SlotKey, ItemData>();
            foreach(var droppedItem in message.DroppedItems)
            {
                if (!storage.AddItem(droppedItem, out var slot)) continue;
                itmes[slot] = droppedItem;
            }
            await TellGateway(new SendToPlayer(character.PlayerId, Protocol.AddInventoryItem, new ServerAddItem { Items = itmes, MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory) }));

            // 2. 任务系统
            var progress = quest.OnEvent(message);
            if (progress.Count == 0) return;

            await TellGateway(new SendToPlayer(character.PlayerId, Protocol.QuestUpdated, new ServerQuestProgressUpdate { QuestUpdates = progress }));

        }

        private async Task HandlItemsAcquired(ItemsAcquired message)
        {
            Dictionary<SlotKey, ItemData> itmes = new Dictionary<SlotKey, ItemData>();
            foreach (var item in message.Items)
            {
                if (!storage.AddItem(item, out var slot)) continue;
                itmes[slot] = item;
            }

            if(itmes.Count > 0)
            {
                await TellGateway(new SendToPlayer(character.PlayerId, Protocol.AddInventoryItem, new ServerAddItem { Items = itmes, MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory) }));
            }

            var progress = quest.OnEvent(message);
            if (progress.Count == 0) return;

            await TellGateway(new SendToPlayer(character.PlayerId, Protocol.QuestUpdated, new ServerQuestProgressUpdate { QuestUpdates = progress }));

        }

        private async Task HandleItemAcquired(ItemAcquired message)
        {
            if (!storage.AddItem(message.Item, out var slot)) return;
            Dictionary<SlotKey, ItemData> itmes = new Dictionary<SlotKey, ItemData>
            {
                { slot, message.Item }
            };
            if (itmes.Count > 0)
            {
                await TellGateway(new SendToPlayer(character.PlayerId, Protocol.AddInventoryItem, new ServerAddItem { Items = itmes, MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory) }));
            }

            var progress = quest.OnEvent(message);
            if (progress.Count == 0) return;

            await TellGateway(new SendToPlayer(character.PlayerId, Protocol.QuestUpdated, new ServerQuestProgressUpdate { QuestUpdates = progress }));

        }


        private async Task HandleAddFriendRequest(AddFriendRequest message)
        {
            if (character != null && character.CharacterName == message.ReceiverCharacterName)
            {
                await TellGateway(new SendToPlayer(character.PlayerId, Protocol.AddFriendRequest, new NetworkFriendRequestData
                {
                    RequestId = message.RequestId,
                    SenderName = message.SenderCharacterName,
                    Remark = message.Remark,
                }));
            }
        }

        private async Task HandleAddFriend(AddFriend message)
        {
            if (character != null && character.CharacterId == message.Data.CharacterId)
            {
                await TellGateway(new SendToPlayer(character.PlayerId, Protocol.AddFriendRequest, message.Data));
            }
        }



    }



}



