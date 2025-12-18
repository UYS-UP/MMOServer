using Server.Game.Actor.Domain.Region.AStar;
using Server.Game.Contracts.Network;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.Skill
{
    public class DashEvent : SkillEvent
    {
        public float Distance { get; set; }
        public float Duration { get; set; }

        public override void Execute(SkillInstance inst)
        {
            //var caster = inst.Caster;
            //var startPos = caster.Kinematics.Position;
            //var dir = HelperUtility.YawToForward(caster.Kinematics.Yaw);
            //var idealGoal = startPos + dir * Distance;

            //Vector3 endPos;
            //var path = inst.AStar.FindPath(idealGoal, startPos);

            //if (path != null && path.Count > 1)
            //{
            //    // 有路径：冲到路径末端（自动处理绕行/墙前停）
            //    endPos = path[^1];  // 最后一个点
            //    float pathLength = 0f;
            //    for (int i = 1; i < path.Count; i++)
            //        pathLength += Vector3.Distance(path[i - 1], path[i]);

            //    // 如果路径太绕（>5m * 1.2），视为不可达，原地
            //    if (pathLength > Distance * 1.2f)
            //    {
            //        endPos = startPos;  // 原地冲刺
            //    }
            //}
            //else if (inst.AStar.HasLineOfSight(startPos, idealGoal))
            //{
            //    // 直线无障碍：直接5m
            //    endPos = idealGoal;
            //}
            //else
            //{
            //    endPos = startPos;
            //}
            //caster.Kinematics.Position = endPos;
            //Console.WriteLine(startPos + "冲刺到:" + endPos);
            //var moveEvent = new ServerSkillTimelineEventMove
            //{
            //    EntityId = caster.EntityId,
            //    EndPos = endPos,
            //    Tick = inst.CombatContext.Tick,
            //    Time = Duration
            //};
            //inst.CombatContext.BroadcastEntities(caster.EntityId, Protocol.SkillTimelineEvent, moveEvent);
        }
    }
}
