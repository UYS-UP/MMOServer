using NPOI.HPSF;
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
    public class BatchActorSend
    {
        public List<(string TargetActorId, IActorMessage Message)> Commnads { get; }

        public BatchActorSend()
        {
            Commnads = new List<(string TargetActorId, IActorMessage Message)> ();
        }

        public void AddTell(string targetActorId, IActorMessage msg)
        {
            Commnads.Add((targetActorId, msg));
        }

        public void ClearSend()
        {
            Commnads.Clear();
        }
    }

    public sealed record CombatEntitySnpot : IActorMessage;

    public record class CharacterMove(
        int ClientTick, string EntityId, Vector3 Position,
        float Yaw, Vector3 Direction, string RegionId, string DungeonId) : IActorMessage;

    public record class CreateDungeonInstance(string DungeonId, string TemplateId, int TeamId, Vector3 EntryPos) : IActorMessage;

    public record class CharacterChangeDungeon(string RegionId, string DungeonId, string TemplateId, Vector3 EntryPos) : IActorMessage;

    public record class CharacterDungeonCompleted();

    public record class CharacterDespawn(string EntityId, string DungeonId) : IActorMessage;

    public record class CharacterSkillRelease(int ClientTick, int SkillId, string EntityId, string DungeonId, SkillCastInputType InputType, Vector3 TargetPosition, Vector3 TargetDirection, string TargetEntityId) : IActorMessage;

    public record class SkillReleaseCommand(int SkillId, string CasterId, string DungeonId) : IActorMessage;

    public record class SetSkillBusyCommand(string EntityId, bool IsBusy) : IActorMessage;

    public record class InitDungeonAI(string DungeonId, string NavFilePath, IReadOnlyCollection<AISnapShot> AISnapShots) : IActorMessage;

    public record class DungeonLootChoice(string DungeonId, string EntityId, string ItemId, bool IsRoll) : IActorMessage;


}
