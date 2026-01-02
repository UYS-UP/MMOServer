using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Contracts.Server;
using System.Numerics;

namespace Server.Game.World
{
    public record class A_CharacterSpawn(EntityRuntime Runtime) : IActorMessage;
    public record class A_CharacterDespawn(int EntityId, int DungeonId = -1) : IActorMessage;
    public record class A_CreateDungeon(int TemplateId, IReadOnlyList<string> Members) : IActorMessage;
}
