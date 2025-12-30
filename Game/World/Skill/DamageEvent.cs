
using NPOI.SS.Formula.Functions;
using Server.DataBase.Entities;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.World.Skill;
using Server.Utility;
using System.Numerics;

namespace Server.Game.World.Skill
{
    [JsonTypeAlias(nameof(CircleDamageEvent))]
    public class CircleDamageEvent : SkillEvent
    {

        public float Radius { get; set; }
        public float Angle { get; set; }
        public int Damage {  get; set; }
        private List<EntityDeath> deaths = new List<EntityDeath>();
        private List<EntityWound> wounds = new List<EntityWound>();

        public override void Execute(SkillInstance inst)
        {
            Console.WriteLine("触发CircleDamageEvent, Radius:" + Radius);
            var visibleEntities = inst.Combat.GetVisibleEntities(inst.Caster.EntityId);
            deaths.Clear();
            wounds.Clear();
            var forward = HelperUtility.YawToForward(inst.Caster.Kinematics.Yaw);
            foreach (var entityId in visibleEntities)
            {
                if (!inst.Combat.TryGetEntity(entityId, out var target)) { continue; }
                if (target.Identity.Type == inst.Caster.Identity.Type) { continue; }
                if (target.Kinematics.State == EntityState.Roll || 
                    target.Kinematics.State == EntityState.Dead) { continue; }
                var delta = target.Kinematics.Position - inst.Caster.Kinematics.Position;
                float distSquared = delta.X * delta.X + delta.Z * delta.Z;
                if (distSquared > Radius * Radius) continue;
                var direction = Vector3.Normalize(delta);
                float dot = Vector3.Dot(forward, direction);
                float angleCos = MathF.Cos(Angle * MathF.PI / 180f / 2f);
                if (dot < angleCos) continue;
                target.Stats.CurrentHp = Math.Max(target.Stats.CurrentHp - Damage, 0);
             
                if (target.Stats.CurrentHp <= 0)
                {
                    deaths.Add(new EntityDeath
                    {
                        DroppedItems = new List<ItemData>(),
                        Target = entityId,
                        Wound = Damage,
                    });
                    target.HFSM.Ctx.DeathRequested = true;
                }
                else
                {
                    wounds.Add(new EntityWound
                    {
                        CurrentHp = target.Stats.CurrentHp,
                        Target = entityId,
                        Wound = Damage,
                    });
                    target.HFSM.Ctx.HitRequested = true;
                }

            }
            if (deaths.Count == 0 && wounds.Count == 0) return;
            inst.Combat.EmitEvent(
                new DamageWorldEvent
                {
                    Deaths = deaths,
                    Wounds = wounds,
                    Source = inst.Caster.EntityId
                });
             }
        }
    

    public class RectangleDamageEvent: SkillEvent
    {
        public Vector3 CenterOffset { get; set; }
        // public DamageShape Shape { get; set; }
        public Vector2 Size { get; set; }
        public float Rotation { get; set; }

        public override void Execute(SkillInstance inst)
        {

        }
    }


    public class SingletonDamageEvent : SkillEvent
    {
        public int Damage { get; set; }

        public override void Execute(SkillInstance inst)
        {
    //        if (string.IsNullOrEmpty(inst.TargetEntityId)) return;

    //        if (!inst.Combat.TryGetEntity(inst.TargetEntityId, out var entity)) return;


    //        var visibleEntities = inst.Combat.GetVisibleEntities(inst.Caster.EntityId);
    //        var deaths = new List<EntityDeath>();
    //        var wounds = new List<EntityWound>();

    //        entity.Combat.ApplyDamage(Damage);

         
    //        if (entity.Combat.Hp <= 0)
    //        {
    //            deaths.Add(new EntityDeath
    //            {
    //                DroppedItems = new List<ItemData>(),
    //                Target = entity.EntityId,
    //                Wound = Damage,
    //            });
    //            entity.FSM.Action.RequestChange(ActionStateType.Death);
    //        }
    //        else
    //        {
    //            wounds.Add(new EntityWound
    //            {
    //                CurrentHp = entity.Combat.Hp,
    //                Target = entity.EntityId,
    //                Wound = Damage,
    //            });

    //            entity.FSM.Action.RequestChange(ActionStateType.Death);
    //        }

    //        if (deaths.Count == 0 && wounds.Count == 0) return;

    //        inst.Combat.EmitEvent(
    //new DamageWorldEvent
    //{
    //    Deaths = deaths,
    //    Wounds = wounds,
    //    Source = inst.Caster.EntityId
    //});
           
        }
    }


    // 1. 将区域和副本区分，取消掉数据库中的DungeonId √
    // 2. 将玩家切换区域 先离开原来的区域，修改数据库中的区域，然后进行加载 √
    // 3. 玩家进入副本，先离开原来的区域(不修改数据库中的RegionId)，然后将副本Id暂存在PlayerActor中，当玩家副本场景加载完毕之后将玩家加入到DungeonActor中去 √

    // 4. 完善AI(小怪AI，待机->追击->施法 √, BossAI， 待机->追击->施法->躲避)
    // 5. 完善Buff系统(完善区域Buff)
    // 6. 完善技能系统(实现4种技能Demo，瞬发技能，持续施法技能，蓄力技能，指向性技能，非指向性技能)

    // 7. 锁定：当客户端锁定了敌人的时候摄像机会在敌人和玩家之间，玩家的左右和往后移动切换为踱步状态，人物不会旋转
}
