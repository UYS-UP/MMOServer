using Server.DataBase.Entities;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.Skill
{
    public class SkillCastData
    {
        public int SkillId;
        public SkillCastInputType InputType;
        public Vector3 TargetPosition;
        public string TargetEntityId;
        public Vector3 TargetDirection;

        public SkillCastData()
        {
        }

        public SkillCastData(
            int skillId, 
            SkillCastInputType inputType = SkillCastInputType.None)
        {
            SkillId = skillId;
            InputType = inputType;
        }

    }
}
