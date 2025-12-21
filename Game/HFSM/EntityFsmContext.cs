using Server.Game.Contracts.Server;
using Server.Game.World;
using Server.Game.World.Skill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM
{
    public class EntityFsmContext
    {
        public readonly EntityRuntime Entity;
        public readonly ICombatContext Combat;
        public bool LockMove;
        public bool LockTurn;

        public bool HasMoveInput => Entity.Kinematics.Direction.LengthSquared() > 0.001f;

        public bool DeathRequested;
        public bool HitRequested;


        public SkillCastData IncomingSkillRequest;
        public SkillCastData ComboBuffer;



        public EntityFsmContext(EntityRuntime entity, ICombatContext combat)
        {
            Entity = entity;
            Combat = combat;
        }

        public void OnReceiveSkillInput(SkillCastData input)
        {
            IncomingSkillRequest = input;
        }


        public void ConsumeOneFrameFlags()
        {
            HitRequested = false;
            DeathRequested = false;
        }

        public void ConsumeInput() => IncomingSkillRequest = null;
        public void ConsumeCombo() => ComboBuffer = null;
    }
}
