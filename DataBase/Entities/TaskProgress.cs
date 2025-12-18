using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.DataBase.Entities
{
    public enum TaskStatusType
    {
        NotStarted,   // 未开始（已接受但未进行）
        InProgress,   // 进行中（有未完成目标）
        Completed,    // 已完成（所有目标完成，但未领取奖励）
        Rewarded,     // 已领取奖励
        Failed,       // 已失败（如限时任务超时）
        Abandoned,    // 已放弃
        Locked        // 已锁定（不满足前置条件）
    }


    public enum TaskType
    {
        Quest,        // 普通任务
        Daily,        // 日常任务（每天重置）
        Weekly,       // 周常任务（每周重置）
        Achievement,  // 成就任务（一次性永久）
        Dungeon,      // 地下城任务
        Raid,         // 团队副本任务
        Event         // 活动限定任务
    }


    public class GameTask
    {
        public string TaskId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public string[] PrerequsiteTasks { get; set; }
        public Dictionary<string, int> Objectives { get; set; }
        public Dictionary<string, int> Rewards { get; set; }
        public string StartNpcId { get; set; }
        public string EndNpcId { get; set;}
        public TaskType Type { get; set; }
        public bool IsRepeateable { get; set; }
        public DateTime RepeateCooldown { get; set; }
        public DateTime TimeLimit  { get; set; }
        public string[] Tags { get; set; }
        public string RegionId { get; set; }

        public GameTask()
        {
            Objectives = new Dictionary<string, int>();
            Rewards = new Dictionary<string, int>();
            PrerequsiteTasks = Array.Empty<string>();
            Tags = Array.Empty<string>();
        }
    }


    public class RoleTaskState
    {
        public string CharacterId { get; set; }
        public string TaskId { get; set; }
        public TaskStatusType Status { get; set; }
        public Dictionary<string, int> Progress { get; set; }
        public DateTime AcceptTime { get; set; }
        public DateTime CompleteTime { get; set; }
        public DateTime RewardClaimedTime { get; set; }
        public DateTime FailedTime { get; set; }
        public int CompletionCount { get; set; }
        public DateTime LastCompletionTime { get; set; }


        public RoleTaskState()
        {
            Progress = new Dictionary<string, int>();
        }
    }


}
