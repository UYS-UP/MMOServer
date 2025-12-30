using Server.Game.Actor.Core;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.ACharacter
{
    public record class CS_PlayerEnterGame(string CharacterId) : IActorMessage;
    public record class CS_CharacterEnterRegion(int RegionId) : IActorMessage;
    public record class CS_CharacterChangeRegion(int MapId) : IActorMessage;
    public record class CS_CharacterLevelDungeon(): IActorMessage;
    public record class CS_CharacterEnterDungeon(string DungeonTemplateId) : IActorMessage;
    public record class CS_CharacterMove(int ClientTick, Vector3 Position, float Yaw, Vector3 Direction) : IActorMessage;
    public record class CS_CharacterCastSkill(int ClientTick, int SkillId, SkillCastInputType InputType, Vector3 TargetPosition, Vector3 TargetDirection, string TargetEntityId) : IActorMessage;



    public record class CS_CreateTeam(string TeamName) : IActorMessage;
    public record class CS_TeamInvite(string PlayerId) : IActorMessage;

    public record class CS_StartDungeon(int TeamId, int TemplateId) : IActorMessage;
    public record class CS_DungeonLootChoice(string ItemId, bool IsRoll) : IActorMessage;



    public record class CS_QueryInventory(int StartSlot, int EndSlot): IActorMessage;
    public record class CS_SwapStorageSlot(int ReqId, SlotKey Slot1, SlotKey Slot2) : IActorMessage;

}
