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

        public bool CastRequested;
        public bool RollRequested;
        public bool AttackRequested;


        public SkillCastData SkillRequestData;


        public EntityFsmContext(EntityRuntime entity, ICombatContext combat)
        {
            Entity = entity;
            Combat = combat;
        }


        public void ConsumeOneFrameFlags()
        {
            HitRequested = false;
            DeathRequested = false;
        }

        public void OnRequestSkill(SkillCastData castData)
        {
            if(castData.SkillId <= 3)
            {
                AttackRequested = true;
                SkillRequestData = castData;
            }else if(castData.SkillId >= 100 && castData.SkillId <= 103)
            {
                SkillRequestData = castData;
                RollRequested = true;
            }
            else
            {
                SkillRequestData = castData;
                CastRequested = true;
            }
        }
    }
}
