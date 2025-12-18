using MessagePack;
using NPOI.HSSF.Record.Chart;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Player;
using Server.Game.Contracts.Actor;
using System.Linq;

namespace Server.Game.Actor.Domain.Player
{
    [MessagePackObject]
    public class QuestNode
    {
        [Key(0)] public string NodeId { get; set; }                 // 唯一ID，如 "main_001_01"
        [Key(1)] public string QuestName { get; set; }
        [Key(2)] public string Description { get; set; }
        [Key(3)] public List<QuestObjective> Objectives { get; set; } = new(); // 当前节点目标
        [Key(4)] public List<string> NextNodeIds { get; set; } = new();        // 成功后可进入的节点
        [Key(5)] public List<string> FailNodeIds { get; set; } = new();         // 失败跳转（可选）
        [Key(6)] public bool IsBranchStart { get; set; } = false;              // 是否是分支起点
        [Key(7)] public bool AutoAccept { get; set; } = true;
        [Key(8)] public bool AutoSubmit { get; set; } = true;
    }

    [MessagePackObject]
    public class QuestObjective
    {
        [Key(0)] public ObjectiveType Type { get; set; }
        [Key(1)] public string TargetId { get; set; }
        [Key(2)] public int RequireCount { get; set; } = 1;
        [Key(3)] public int CurrentCount { get; set; } = 0;

        [IgnoreMember] public bool IsCompleted => CurrentCount >= RequireCount;
    }

    public enum ObjectiveType
    {
        KillMonster,
        CollectItem,
        TalkToNpc,
        EnterRegion,
        UseSkill,
        ReachLevel,
        SubmitToNpc,
        CustomEvent
    }


    public class QuestManager
    {
        private readonly Dictionary<string, QuestNode> allNodes = new();
        private readonly HashSet<string> completedNodes = new();
        private readonly Dictionary<string, QuestNode> activeNodes = new();
        private readonly Dictionary<string, HashSet<int>> dirtyObjectives = new();

        public event Action<QuestNode> OnActivateNode;
        public event Action<string> OnQuestCompleted;

        public void LoadQuestChainConfig(Dictionary<string, QuestNode> config)
        {
            allNodes.Clear();
            foreach (var kv in config)
                allNodes[kv.Key] = kv.Value;
        }

        // 接受任务链（通常是接受第一个节点）
        public void AcceptChain(string startNodeId)
        {
            if (allNodes.TryGetValue(startNodeId, out var node) && node.AutoAccept)
            {
                ActivateNode(startNodeId);
            }
        }

        private void ActivateNode(string nodeId)
        {
            if (completedNodes.Contains(nodeId) || activeNodes.ContainsKey(nodeId)) return;

            if (allNodes.TryGetValue(nodeId, out var node))
            {
                activeNodes[nodeId] = node;
                OnActivateNode?.Invoke(node);
                Console.WriteLine($"[任务] 激活节点: {node.QuestName}");
            }
        }

        // 事件入口（PlayerActor 调用）
        public List<QuestProgressUpdate> OnEvent(IQuestEvent e)
        {
            dirtyObjectives.Clear();
            bool changed = false;

            foreach (var kv in activeNodes.ToList())
            {
                var node = kv.Value;
                bool nodeCompletedBefore = node.Objectives.All(o => o.IsCompleted);
                for (int i = 0; i < node.Objectives.Count; ++i)
                { 
                    var obj = node.Objectives[i];
                    if (obj.IsCompleted) continue;
                    if (e.Match(obj))
                    {
                        obj.CurrentCount++;
                        if (!dirtyObjectives.ContainsKey(kv.Key))
                            dirtyObjectives[kv.Key] = new HashSet<int>();
                        dirtyObjectives[kv.Key].Add(i);
                        changed = true;
                    }
                }

                bool nodeCompletedNow = node.Objectives.All(o => o.IsCompleted);

                // 节点刚刚完成
                if (!nodeCompletedBefore && nodeCompletedNow)
                {
                    CompleteNode(kv.Key);
                    changed = true;
                }
            }

            if (changed)
                return GetBroadcastProgress();
            return new List<QuestProgressUpdate>(capacity: 0);
        }

        private void CompleteNode(string nodeId)
        {
            if (!activeNodes.TryGetValue(nodeId, out var node)) return;

            activeNodes.Remove(nodeId);
            completedNodes.Add(nodeId);
            OnQuestCompleted?.Invoke(nodeId);
            // 自动提交奖励
            GrantRewards(nodeId);

            // 自动激活后续节点
            foreach (var nextId in node.NextNodeIds)
            {
                ActivateNode(nextId);
            }

            Console.WriteLine($"[任务] 完成节点: {node.QuestName}");
        }

        private void GrantRewards(string nodeId)
        {
            // TODO: 发奖励（经验、金币、物品）
            // 可以走 BatchInterActorSend 或直接 TellGateway
        }

        public List<QuestProgressUpdate> GetBroadcastProgress()
        {
            var updates = new List<QuestProgressUpdate>(capacity: dirtyObjectives.Count);
            foreach(var kv in dirtyObjectives)
            {

                var objectives = new List<(int, QuestObjective)>(capacity: kv.Value.Count);
                foreach (var index in kv.Value)
                {
                    objectives.Add((index, allNodes[kv.Key].Objectives[index]));
                }

                var questProgress = new QuestProgressUpdate
                {
                    NodeId = kv.Key,
                    Objectives = objectives,
                    IsCompleted = completedNodes.Contains(kv.Key)
                };

                updates.Add(questProgress);
            }
            return updates;
        }

        public List<QuestNode> GetActiveQuests() => activeNodes.Values.ToList();
        public List<QuestNode> GetCompletedQuests() => completedNodes.Select(id => allNodes[id]).ToList();
    }

    public interface IQuestEvent
    {
        bool Match(QuestObjective objective);
    }

    [MessagePackObject]
    public class QuestProgressUpdate
    {
        [Key(0)] public string NodeId;
        [Key(1)] public bool IsCompleted;
        [Key(2)] public List<(int, QuestObjective)> Objectives;
    }
}
