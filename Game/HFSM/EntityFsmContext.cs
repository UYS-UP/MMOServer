using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM
{
    public class EntityFsmContext
    {
        public EntityRuntime Entity;
        public bool LockMove;
        public bool LockTurn;

        public bool HasMoveInput => Entity.Kinematics.Direction.LengthSquared() < 0.001f;

        public bool DeathRequested;
        public bool HitRequested;


        public int CastSkillId;
        public bool CastRequested;

        public EntityFsmContext(EntityRuntime entity)
        {
            Entity = entity;
        }


        public void ConsumeOneFrameFlags()
        {
            HitRequested = false;
            DeathRequested = false;
            CastRequested = false;
        }
    }
}
