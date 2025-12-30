using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Contracts.Server;
using System.Numerics;

namespace Server.Game.World
{
    public record class A_CharacterSpawn(EntityRuntime Runtime) : IActorMessage;
    public record class A_CharacterDespawn(int EntityId, int DungeonId = -1) : IActorMessage;
    public record class A_CharacterCastSkill(int ClientTick, int SkillId, int EntityId, int DungeonId, SkillCastInputType InputType, Vector3 TargetPosition, Vector3 TargetDirection, string TargetEntityId) : IActorMessage;
    
    public record class A_CharacterMove(
    int ClientTick, int EntityId, Vector3 Position,
    float Yaw, Vector3 Direction, int MapId, int DungeonId) : IActorMessage;


    public record class A_CreateDungeon(int TemplateId, IReadOnlyList<string> Members) : IActorMessage;
    public record class A_DungeonLootChoice(int DungeonId, int EntityId, string ItemId, bool IsRoll) : IActorMessage;
}
