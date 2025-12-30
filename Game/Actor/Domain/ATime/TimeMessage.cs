using Server.Game.Actor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.ATime
{
    /// <summary>
    /// 注册定时器
    /// </summary>
    /// <param name="Key">定时器的Key</param>
    /// <param name="Sender">注册者的ActorId</param>
    /// <param name="Message">定时器所携带的消息</param>
    /// <param name="DelayTicks">延迟多少Tick</param>
    /// <param name="DelayMs">延迟多少毫秒</param>
    /// <param name="PeriodTicks">在多少Tick的时候结束</param>
    /// <param name="PeriodMs">在多少毫秒的时候结束</param>
    /// <param name="Repeat">是否重复注册</param>
    public record class RegisterTimer(
        string Key, string Sender, IActorMessage Message, 
        int DelayTicks = 0, int DelayMs = 0, 
        int PeriodTicks = 0, float PeriodMs = 0,
        bool Repeat = false) : IActorMessage;

    /// <summary>
    /// 取消定时器
    /// </summary>
    /// <param name="Key">注册定时器时的Key值</param>
    public record class CancelTimer(string Key) : IActorMessage;

    /// <summary>
    /// 定时器到期
    /// </summary>
    /// <param name="Key">注册定时器时的Key值</param>
    /// <param name="Message">注册定时器时传递的消息</param>
    /// <param name="Tick">到期时的Tick</param>
    public record class TimerFired(string Key, IActorMessage Message, int Tick) : IActorMessage;

    /// <summary>
    /// 定时器事件(每帧推送一次)
    /// </summary>
    /// <param name="Tick">当前Tick</param>
    /// <param name="UtcMs">当前的时间戳</param>
    public record class TickUpdateEvent(int Tick, long UtcMs, float DeltaTime) : IActorMessage;

    /// <summary>
    /// 客户端Ping消息
    /// </summary>
    /// <param name="ClientUtcMs">客户端时间戳</param>
    /// <param name="SessionId">会话Id</param>
    public record class HeartPing(long ClientUtcMs, Guid SessionId) : IActorMessage;


    public record class A_DungeonDesotryTimer(int DungeonId) : IActorMessage;
}
