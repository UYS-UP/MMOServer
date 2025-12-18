using Server.Game.Actor.Core;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Actor
{

    public sealed record CharacterEntitySnapshot(
        string PlayerId, string CharacterId,
        string EntityId, string CharacterName,
        EntityType Type,
        int CharacterLevel, ProfessionType Profession,
        string RegionId, string DungeonId) : IActorMessage;

    public sealed record EntitySnapshot(
        string EntityId, string RegionId, string DungeonId, 
        Vector3 Pos, float Yaw, EntityType Type) : IActorMessage;

    public sealed record AISnapShot(
        EntityCombatSnapshot CombatSanpshot, float PerceptionRange,
        float FovAngle, float RepathCdLeft, Vector3 HomePos
        ) : IActorMessage;

    public sealed record EntityAOIUpdateEvent(string DungeonId, string EntityId, IReadOnlySet<string> VisibleEntities) : IActorMessage;

    public sealed record EntityCombatSnapshot(
        string EntityId, Vector3 Pos, float Yaw, EntityType Type,
        int Hp, int MaxHp, int Mp, int MaxMp, int Attack, int Defence,
        string RegionId, string DungeonId
        ) : IActorMessage;

    public sealed record RegionPlayerSnpot(IReadOnlyCollection<string> RegionPlayers) : IActorMessage;
    public sealed record TeamSnpot(int TeamId, string TeamName, TeamType Type, IReadOnlyList<string> Members) : IActorMessage;

    public record EntityPositionUpdateEvent(string EntityId, string DungeonId, Vector3 NewPos, float NewYaw) : IActorMessage;
}
