using MessagePack;
using Server.DataBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Player
{
    public class StorageManager
    {
        private Dictionary<SlotKey, ItemData> storage = new Dictionary<SlotKey, ItemData>();
        private SlotKey currentLastItemSlot;
        private int maxInventorySize = 2400;
        private int maxQuickBarSize = 5;
        private int maxEquipmentSize = 6;

        public StorageManager()
        {
            currentLastItemSlot = new SlotKey(SlotContainerType.Inventory, 0);
        }

        public int MaxInventorySize => maxInventorySize;

        #region 获取物品相关方法

        public Dictionary<SlotKey, ItemData> GetAllItems()
        {
            return new Dictionary<SlotKey, ItemData>(storage);
        }

        public Dictionary<SlotKey, ItemData> GetAllItems(SlotContainerType containerType)
        {
            return storage.Where(x => x.Key.Container == containerType)
                         .ToDictionary(x => x.Key, x => x.Value);
        }

        public Dictionary<SlotKey, ItemData> GetRangeSlotItems(SlotContainerType containerType, int minSlot, int maxSlot)
        {
            return storage.Where(x => x.Key.Container == containerType &&
                                     x.Key.Index >= minSlot && x.Key.Index <= maxSlot)
                         .ToDictionary(x => x.Key, x => x.Value);
        }

        public bool TryGetItem(SlotKey slot, out ItemData item)
        {
            return storage.TryGetValue(slot, out item);
        }

        public int GetMaxOccupiedSlotIndex(SlotContainerType containerType)
        {
            return storage.Where(x => x.Key.Container == containerType)
                         .Select(x => x.Key.Index)
                         .DefaultIfEmpty(-1)
                         .Max();
        }

        public int GetContainerSize(SlotContainerType containerType)
        {
            return containerType switch
            {
                SlotContainerType.Inventory => maxInventorySize,
                SlotContainerType.QuickBar => maxQuickBarSize,
                SlotContainerType.Equipment => maxEquipmentSize,
                _ => 0
            };
        }

        #endregion


        #region 添加物品相关方法

        public bool AddItem(SlotKey slot, ItemData itemData)
        {
            if (!IsSlotValid(slot))
                return false;

            if (storage.TryGetValue(slot, out var existingItem))
            {
                if (CanStack(existingItem, itemData))
                {
                    existingItem.ItemCount += itemData.ItemCount;
                    return true;
                }
                return false; // 槽位被占用且不能堆叠
            }

            storage.Add(slot, itemData);
            UpdateLastItemSlot(slot);
            return true;
        }

        public bool AddItem(ItemData itemData, out SlotKey occupiedSlot)
        {
            // 首先尝试堆叠到现有物品
            var stackableSlot = FindStackableSlot(itemData);
            if (stackableSlot != SlotKey.Default)
            {
                storage[stackableSlot].ItemCount += itemData.ItemCount;
                occupiedSlot = stackableSlot;
                return true;
            }

            // 寻找空槽位
            var emptySlot = FindEmptySlot(SlotContainerType.Inventory);
            if (emptySlot != SlotKey.Default)
            {
                storage.Add(emptySlot, itemData);
                UpdateLastItemSlot(emptySlot);
                occupiedSlot = emptySlot;
                return true;
            }

            occupiedSlot = SlotKey.Default;
            return false;
        }

        private SlotKey FindStackableSlot(ItemData itemData)
        {
            if (!itemData.IsStack) return SlotKey.Default;

            return storage.FirstOrDefault(x =>
                x.Value.ItemId == itemData.ItemId &&
                x.Key.Container == SlotContainerType.Inventory)
                .Key;
        }

        private SlotKey FindEmptySlot(SlotContainerType containerType)
        {
            var maxSize = GetContainerSize(containerType);

            for (int i = 0; i < maxSize; i++)
            {
                var slot = new SlotKey(containerType, i);
                if (!storage.ContainsKey(slot))
                    return slot;
            }
            return SlotKey.Default;
        }

        #endregion


        #region 交换物品相关方法

        public bool SwapItems(SlotKey slot1, SlotKey slot2)
        {
            if (!IsSlotValid(slot1) || !IsSlotValid(slot2))
                return false;

            // 同容器内交换
            if (slot1.Container == slot2.Container)
            {
                return SwapWithinSameContainer(slot1, slot2);
            }

            // 跨容器交换
            return SwapBetweenContainers(slot1, slot2);
        }

        private bool SwapWithinSameContainer(SlotKey slot1, SlotKey slot2)
        {
            var hasItem1 = storage.TryGetValue(slot1, out var item1);
            var hasItem2 = storage.TryGetValue(slot2, out var item2);

            if (hasItem1 && hasItem2)
            {
                storage[slot1] = item2;
                storage[slot2] = item1;
            }
            else if (hasItem1 && !hasItem2)
            {
                storage.Remove(slot1);
                storage.Add(slot2, item1);
            }
            else if (!hasItem1 && hasItem2)
            {
                storage.Remove(slot2);
                storage.Add(slot1, item2);
            }

            UpdateLastItemSlot();
            return true;
        }

        private bool SwapBetweenContainers(SlotKey slot1, SlotKey slot2)
        {
            // 检查装备限制等逻辑
            if (!CanItemMoveToContainer(slot1, slot2.Container) ||
                !CanItemMoveToContainer(slot2, slot1.Container))
                return false;

            var hasItem1 = storage.TryGetValue(slot1, out var item1);
            var hasItem2 = storage.TryGetValue(slot2, out var item2);

            if (hasItem1) storage.Remove(slot1);
            if (hasItem2) storage.Remove(slot2);

            if (hasItem1) storage.Add(slot2, item1);
            if (hasItem2) storage.Add(slot1, item2);

            UpdateLastItemSlot();
            return true;
        }

        private bool CanItemMoveToContainer(SlotKey sourceSlot, SlotContainerType targetContainer)
        {
            if (!storage.TryGetValue(sourceSlot, out var item))
                return true; // 空槽位可以移动

            // 添加装备类型检查等逻辑
            // 例如：检查物品是否可以装备到目标容器
            return true;
        }

        #endregion


        #region 删除物品相关方法

        public bool RemoveItem(SlotKey slot)
        {
            if (storage.Remove(slot))
            {
                UpdateLastItemSlot();
                return true;
            }
            return false;
        }

        public bool RemoveItem(SlotKey slot, int count)
        {
            if (count <= 0 || !storage.TryGetValue(slot, out var item))
                return false;

            if (item.ItemCount <= count)
            {
                return RemoveItem(slot);
            }
            else
            {
                item.ItemCount -= count;
                return true;
            }
        }

        public int RemoveItemsByItemId(string itemId, int count)
        {
            int remainingCount = count;
            var itemsToRemove = storage.Where(x => x.Value.ItemId == itemId)
                                      .OrderBy(x => x.Key.Container) // 优先从特定容器移除
                                      .ThenBy(x => x.Key.Index)
                                      .ToList();

            foreach (var (slot, item) in itemsToRemove)
            {
                if (remainingCount <= 0) break;

                if (item.ItemCount <= remainingCount)
                {
                    remainingCount -= item.ItemCount;
                    storage.Remove(slot);
                }
                else
                {
                    item.ItemCount -= remainingCount;
                    remainingCount = 0;
                }
            }

            UpdateLastItemSlot();
            return count - remainingCount;
        }

        #endregion


        #region 辅助方法

        private bool IsSlotValid(SlotKey slot)
        {
            var maxSize = GetContainerSize(slot.Container);
            return slot.Index >= 0 && slot.Index < maxSize;
        }

        private bool CanStack(ItemData existing, ItemData newItem)
        {
            return existing.ItemId == newItem.ItemId &&
                   existing.IsStack &&
                   newItem.IsStack;
        }

        private void UpdateLastItemSlot(SlotKey? newSlot = null)
        {
            if (newSlot.HasValue && newSlot.Value.Container == SlotContainerType.Inventory)
            {
                if (newSlot.Value.Index >= currentLastItemSlot.Index)
                {
                    currentLastItemSlot = FindNextAvailableSlot();
                }
            }
            else
            {
                currentLastItemSlot = FindNextAvailableSlot();
            }
        }

        private SlotKey FindNextAvailableSlot()
        {
            for (int i = 0; i < maxInventorySize; i++)
            {
                var slot = new SlotKey(SlotContainerType.Inventory, i);
                if (!storage.ContainsKey(slot))
                    return slot;
            }
            return new SlotKey(SlotContainerType.Inventory, maxInventorySize);
        }

        public int GetItemCount(string itemId)
        {
            return storage.Where(x => x.Value.ItemId == itemId)
                         .Sum(x => x.Value.ItemCount);
        }

        public bool HasEmptySlot(SlotContainerType containerType)
        {
            var maxSize = GetContainerSize(containerType);
            return storage.Count(x => x.Key.Container == containerType) < maxSize;
        }

        #endregion
    }


    public enum SlotContainerType
    {
        None,
        Inventory,
        Equipment,
        QuickBar,
     
    }

    [MessagePackObject]
    public struct SlotKey : IEquatable<SlotKey>
    {
        [Key(0)] public SlotContainerType Container = SlotContainerType.None;
        [Key(1)] public int Index = -1;

        public static readonly SlotKey Default = new SlotKey
        {
            Container = SlotContainerType.None,
            Index = -1
        };

        public SlotKey()
        {
            Container = SlotContainerType.None;
            Index = -1;
        }

        public SlotKey(SlotContainerType container, int index)
        {
            Container = container;
            Index = index;
        }

        public override bool Equals(object obj)
        {
            return obj is SlotKey slot && Equals(slot);
        }

        public bool Equals(SlotKey other)
        {
            return Container == other.Container && Index == other.Index;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Container.GetHashCode();
                hash = hash * 23 + Index.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(SlotKey left, SlotKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SlotKey left, SlotKey right)
        {
            return !(left == right);
        }
    }
}
