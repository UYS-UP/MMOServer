using Server.Network;
using Server.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using System.Diagnostics;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Common;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.ASession;

namespace Server.Game.Actor.Domain.ATime
{
    /// <summary>
    /// TimeActor维护一个环形数组，每隔一个固定帧间隔指针就向前移动一格，检查当前位置的任务是否到期
    /// </summary>
    public class TimeActor : ActorBase
    {

        private const int WHEEL_SIZE = 512;         // 槽位数        
        private readonly List<TimerTask>[] wheel = new List<TimerTask>[WHEEL_SIZE];
        private int cursorTick = 0;  // 逻辑帧指针
        private Stopwatch stopwatch;
        private long lastMs;
        private long lastTickTime;

        private CancellationTokenSource loopCts;
        private ActorEventBus EventBus => System.EventBus;

        public TimeActor(string actorId) : base(actorId)
        {
       
        }


        protected override async Task OnStart()
        {
            await base.OnStart();
            for (int i = 0; i < WHEEL_SIZE; i++)
            {
                wheel[i] = new List<TimerTask>();
            }
            stopwatch = Stopwatch.StartNew();
            lastMs = stopwatch.ElapsedMilliseconds;
            loopCts = new CancellationTokenSource();
            _ = SelfLoopAsync(loopCts.Token);
        }

        private async Task SelfLoopAsync(CancellationToken token)
        {
            long next = stopwatch.ElapsedMilliseconds;
            while(!token.IsCancellationRequested)
            {
                next += GameField.TICK_INTERVAL_MS;
                var delay = next - stopwatch.ElapsedMilliseconds;
                if(delay > 0) await Task.Delay((int)delay, token);
                await HandleAdvanceTicks();
                
            }

        }

        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case HeartPing ping:
                    await HandleHeartPing(ping);
                    break;
                case RegisterTimer registerTimer:
                    await HandleRegisterTimer(registerTimer);
                    break;
                case CancelTimer cancelTimer:
                    await HandleCancelTimer(cancelTimer);
                    break;
            }
        }

        private async Task HandleHeartPing(HeartPing ping)
        {
            long serverUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pong = new ServerHeartPong(serverUtcMs, cursorTick, ping.ClientUtcMs);
            var sessionActor = GameField.GetActor<SessionActor>(ping.SessionId.ToString());
            await TellAsync(sessionActor, new SendTo(Protocol.Heart, MessagePackSerializer.Serialize(pong)));
        }

        private async Task HandleAdvanceTicks()
        {
            var now = stopwatch.ElapsedMilliseconds;
            var elapsed = now - lastMs;
            while (elapsed >= GameField.TICK_INTERVAL_MS)
            {
                elapsed -= GameField.TICK_INTERVAL_MS;
                cursorTick++;
                long currentTickTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long tickInterval = currentTickTime - lastTickTime;
                lastTickTime = currentTickTime;
                await Step(cursorTick);
                await EventBus.PublishAsync(new TickUpdateEvent(cursorTick, currentTickTime, tickInterval / 1000f));
            }
            lastMs = now - elapsed;
        }

        private Task HandleRegisterTimer(RegisterTimer registerTimer)
        {
            int delayTicks = registerTimer.DelayTicks > 0 ? 
                registerTimer.DelayTicks : 
                (int)Math.Ceiling(registerTimer.DelayMs / (double)GameField.TICK_INTERVAL_MS);
            var slot = cursorTick + delayTicks & WHEEL_SIZE - 1;
            var rounds = delayTicks / WHEEL_SIZE;
            wheel[slot].Add(new TimerTask(registerTimer.Key,
                registerTimer.Message, registerTimer.Repeat, registerTimer.PeriodTicks,
                registerTimer.PeriodMs, rounds, registerTimer.Sender));
            return Task.CompletedTask;
        }

        private Task HandleCancelTimer(CancelTimer cancelTimer)
        {
            foreach (var timerList in wheel)
            {
                timerList.RemoveAll(t => t.Key == cancelTimer.Key);
            }
            return Task.CompletedTask;
        }

        private async Task Step(int tick)
        {
            var slot = tick & WHEEL_SIZE - 1;
            var list = wheel[slot];

            for (int i = list.Count - 1; i >= 0 ; i--)
            {
                var task = list[i];
                if(task.Rounds > 0)
                {
                    task.Rounds--;
                    list[i] = task;
                    continue;
                };
                await TellAsync(ActorId, new TimerFired(task.Key, task.Message, tick));
                list.RemoveAt(i);

                if (task.Repeat)
                {
                    int periodTicks = task.PeriodTicks > 0 ? task.PeriodTicks : 
                        (int)Math.Ceiling(task.PeriodMs / (double)GameField.TICK_INTERVAL_MS);

                    var slot2 = tick + periodTicks & WHEEL_SIZE - 1;
                    var rounds2 = periodTicks / WHEEL_SIZE;
                        wheel[slot2].Add(new TimerTask(task.Key,
                        task.Message, task.Repeat, task.PeriodTicks,
                        task.PeriodMs, rounds2, task.ActorId));
                }

           
            }
        }
    }

    internal struct TimerTask
    {
        public string Key;
        public IActorMessage Message;   // 消息
        public bool Repeat;             // 是否是循环任务
        public int PeriodTicks;      // 帧数
        public float PeriodMs;            // 毫秒数
        public int Rounds;              // 需要循环多少轮
        public string ActorId;

        public TimerTask(string key, IActorMessage message, bool repeat, int periodTicks, float periodMS, int rounds, string actorId)
        {
            Key = key;
            Message = message;
            Repeat = repeat;
            PeriodTicks = periodTicks;
            PeriodMs = periodMS;
            Rounds = rounds;
            ActorId = actorId;
        }
    }
}