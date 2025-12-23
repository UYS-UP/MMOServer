using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.Skill
{
    public class SkillTimelineRunner
    {
        public float CurrentTime { get; private set; }
        public bool IsFinished { get; private set; }

        private readonly float duration;
        private readonly List<SkillEvent> events;
        private int nextEventIndex;

        private readonly List<SkillPhase> phases;
        private int nextPhaseStartIndex;
        private readonly List<SkillPhase> activePhases = new();

        const float EPS = 0.000001f;

        public SkillTimelineRunner(float duration, IEnumerable<SkillEvent> events = null, IEnumerable<SkillPhase> phases = null)
        {
            this.duration = duration;
            CurrentTime = 0f;
            nextEventIndex = 0;
            IsFinished = false;
            if (events != null)
            {
                this.events = events.OrderBy(e => e.Time).ToList();
            }

            if (phases != null)
            {
                this.phases = phases.OrderBy(e => e.StartTime).ToList();
            }
        }

        public void Start(SkillInstance skillInstance)
        {
            FireDueEvents(skillInstance, 0f);
            StartDuePhases(skillInstance, 0f);
            // UpdateActivePhases(skillInstance, 0f);
            EndDuePhases(skillInstance, 0f);
        }

        public void Tick(SkillInstance instance, float dt)
        {
            if (IsFinished) return;

            CurrentTime += dt;
            FireDueEvents(instance, CurrentTime);

            StartDuePhases(instance, CurrentTime);
            UpdateActivePhases(instance, dt);
            EndDuePhases(instance, CurrentTime);

            if (CurrentTime >= duration)
                Finish(instance);
        }

        public void Interrupt(SkillInstance instance)
        {
            if (IsFinished) return;
            IsFinished = true;
            EndAllActivePhases(instance);
        }

        private void FireDueEvents(SkillInstance instance, float time)
        {
            // Console.WriteLine($"NextEventIndex:{nextEventIndex}");
            while (nextEventIndex < events.Count && events[nextEventIndex].Time <= time + EPS)
            {
                events[nextEventIndex].Execute(instance);
                nextEventIndex++;
            }
        }

        private void StartDuePhases(SkillInstance instance, float time)
        {
            while (nextPhaseStartIndex < phases.Count && phases[nextPhaseStartIndex].StartTime <= time + EPS)
            {
                var p = phases[nextPhaseStartIndex++];
 
                if (p.EndTime <= p.StartTime) continue;

                p.OnStart(instance);
                activePhases.Add(p);
            }
        }

        private void UpdateActivePhases(SkillInstance instance, float dt)
        {
            for (int i = 0; i < activePhases.Count; i++)
                activePhases[i].OnUpdate(instance, dt);
        }

        private void EndDuePhases(SkillInstance instance, float time)
        {
            for (int i = activePhases.Count - 1; i >= 0; i--)
            {
                if (activePhases[i].EndTime <= time + EPS)
                {
                    activePhases[i].OnExit(instance);
                    activePhases.RemoveAt(i);
                }
            }
        }

        private void EndAllActivePhases(SkillInstance instance)
        {
            for (int i = activePhases.Count - 1; i >= 0; i--)
                activePhases[i].OnExit(instance);
            activePhases.Clear();
        }

        private void Finish(SkillInstance instance)
        {
            if (IsFinished) return;
            IsFinished = true;
            EndAllActivePhases(instance);
        }
    }
}
