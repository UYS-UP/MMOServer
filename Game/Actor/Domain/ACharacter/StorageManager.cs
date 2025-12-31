using MessagePack;
using Server.DataBase.Entities;
using Server.Game.Contracts.Server;
using Server.Utility;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.ACharacter
{
    public class StorageManager
    {
        private Dictionary<SlotKey, ItemData> storage = new Dictionary<SlotKey, ItemData>();

        private readonly Dictionary<SlotContainerType, int> containerSizes = new()
        {
            { SlotContainerType.Inventory, 2400 },
            { SlotContainerType.Equipment, 8 },
            { SlotContainerType.QuickBar, 3 }
        };

        private readonly Dictionary<int, EquipType> equipSlotMapping = new()
        {
            { 0, EquipType.Weapon },
            { 1, EquipType.Helmet },
            { 2, EquipType.Clothes },
            { 3, EquipType.Pants },
            { 4, EquipType.Shoes },
            { 5, EquipType.Necklace },
        };

        public bool TryGetItem(SlotKey key, out ItemData item) => storage.TryGetValue(key, out item);
        public Dictionary<SlotKey, ItemData> GetAllItems() => new Dictionary<SlotKey, ItemData>(storage);
        public int GetMaxContainerSize(SlotContainerType container) => containerSizes[container];
        public int GetMaxOccupiedSlotIndex(SlotContainerType container)
        {
            return storage.Where(x => x.Key.Container == container)
                           .Select(x => x.Key.Index)
                           .DefaultIfEmpty(-1)
                           .Max();
        }

        public Dictionary<SlotKey, ItemData> GetItemsInRange(SlotContainerType container, int startSlot, int endSlot)
        {
            var result = new Dictionary<SlotKey, ItemData>();
            if (startSlot < 0) startSlot = 0;
            for (int i = startSlot; i <= endSlot; i++)
            {
                var key = new SlotKey(container, i);
                if (storage.TryGetValue(key, out var item))
                {
                    result[key] = item;
                }
            }

            return result;
        }


        public int AddItem(ItemData itemToAdd, out List<SlotKey> changedSlots)
        {
            changedSlots = new List<SlotKey>();
            int remainingCount = itemToAdd.ItemCount;
            int maxStack = itemToAdd.ItemType == ItemType.Equip ? 1 : 99;

            if(maxStack > 1)
            {
                var stackableSlots = storage.Where(x => 
                    x.Key.Container == SlotContainerType.Inventory &&
                    x.Value.TemplateId == itemToAdd.TemplateId &&
                    x.Value.ItemCount < maxStack)
                    .OrderBy(x => x.Value.ItemCount).ToList();

                foreach(var kvp in stackableSlots)
                {
                    if (remainingCount <= 0) break;
                    int space = maxStack - kvp.Value.ItemCount;
                    int toAdd = Math.Min(space, remainingCount);

                    kvp.Value.ItemCount += toAdd;
                    remainingCount -= toAdd;

                    if(!changedSlots.Contains(kvp.Key)) changedSlots.Add(kvp.Key);
                }
            }

            while(remainingCount > 0)
            {
                var emptySlot = FindEmptySlot(SlotContainerType.Inventory);
                if (emptySlot == SlotKey.Default)
                {
                    break;
                }
                int countThisSlot = Math.Min(remainingCount, maxStack);

                var newItem = DeepClone(itemToAdd);
                newItem.ItemCount = countThisSlot;
                if (newItem is EquipData && string.IsNullOrEmpty(newItem.InstanceId))
                {
                    newItem.InstanceId = Guid.NewGuid().ToString();
                }

                storage.Add(emptySlot, newItem);
                changedSlots.Add(emptySlot);

                remainingCount -= countThisSlot;
            }

            return remainingCount;
        
        }

        public bool SwapItem(SlotKey src, SlotKey dest, out List<SlotKey> changedSlots)
        {
            changedSlots = new List<SlotKey>();

            if (!IsValidSlot(src) || !IsValidSlot(dest)) return false;
            if (src == dest) return false;

            bool hasSrc = storage.TryGetValue(src, out var srcItem);
            bool hasDest = storage.TryGetValue(dest, out var destItem);

            if (!hasSrc && !hasDest) return false;

            if (hasSrc && !CanPlaceItemInContainer(dest, srcItem)) return false;
            if (hasDest && !CanPlaceItemInContainer(src, destItem)) return false;

            if (hasSrc) storage.Remove(src);
            if (hasDest) storage.Remove(dest);

            if (hasSrc)
            {
                storage.Add(dest, srcItem);
                changedSlots.Add(dest);
            }
            if (hasDest)
            {
                storage.Add(src, destItem);
                changedSlots.Add(src);
            }

            if (!hasSrc && hasDest) changedSlots.Add(src); 
            if (hasSrc && !hasDest) changedSlots.Add(dest);

            return true;
        }

        public bool SplitItem(SlotKey src, SlotKey dest, int count, out List<SlotKey> changedSlots)
        {
            changedSlots = new List<SlotKey>();
            if (!storage.TryGetValue(src, out var srcItem)) return false;
            if (storage.ContainsKey(dest)) return false; // 目标必须为空
            if (count >= srcItem.ItemCount || count <= 0) return false; // 拆分数量不合法
            if (!CanPlaceItemInContainer(dest, srcItem)) return false; // 目标容器不支持

            srcItem.ItemCount -= count;

            var newItem = DeepClone(srcItem);
            newItem.ItemCount = count;

            if (newItem is EquipData)
            {
                newItem.InstanceId = HelperUtility.GetKey();
            }

            storage.Add(dest, newItem);

            changedSlots.Add(src);
            changedSlots.Add(dest);
            return true;
        }

        public bool RemoveItem(SlotKey key, int count, out List<SlotKey> changedSlots)
        {
            changedSlots = new List<SlotKey>();
            if (!storage.TryGetValue(key, out var item)) return false;

            if (item.ItemCount <= count)
            {
                storage.Remove(key);
            }
            else
            {
                item.ItemCount -= count;
            }
            changedSlots.Add(key);
            return true;
        }


        private bool CanPlaceItemInContainer(SlotKey targetSlot, ItemData item)
        {
            if(targetSlot.Container == SlotContainerType.Inventory) return true;
            if(targetSlot.Container == SlotContainerType.Equipment)
            {
                if (item is not EquipData equip) return false;
                if(equipSlotMapping.TryGetValue(targetSlot.Index, out var requiredType))
                {
                    return requiredType == equip.EquipType;
                }
                return false;
            }
            return false;
        }



        private SlotKey FindEmptySlot(SlotContainerType container)
        {
            int max = containerSizes.GetValueOrDefault(container, 0);
            for (int i = 0; i < max; i++)
            {
                var key = new SlotKey(container, i);
                if (!storage.ContainsKey(key)) return key;
            }
            return SlotKey.Default;
        }

        private bool IsValidSlot(SlotKey key)
        {
            int max = containerSizes.GetValueOrDefault(key.Container, 0);
            return key.Index >= 0 && key.Index < max;
        }


        private ItemData DeepClone(ItemData source)
        {
            var bytes = MessagePackSerializer.Serialize(source);
            return MessagePackSerializer.Deserialize<ItemData>(bytes);
        }



        //    private SlotKey currentLastItemSlot;
        //    private int maxInventorySize = 2400;
        //    private int maxQuickBarSize = 5;
        //    private int maxEquipmentSize = 6;

        //    public StorageManager()
        //    {
        //        currentLastItemSlot = new SlotKey(SlotContainerType.Inventory, 0);
        //    }

        //    public int MaxInventorySize => maxInventorySize;

        //    #region 获取物品相关方法

        //    public Dictionary<SlotKey, ItemData> GetAllItems()
        //    {
        //        return new Dictionary<SlotKey, ItemData>(storage);
        //    }

        //    public Dictionary<SlotKey, ItemData> GetAllItems(SlotContainerType containerType)
        //    {
        //        return storage.Where(x => x.Key.Container == containerType)
        //                     .ToDictionary(x => x.Key, x => x.Value);
        //    }

        //    public Dictionary<SlotKey, ItemData> GetRangeSlotItems(SlotContainerType containerType, int minSlot, int maxSlot)
        //    {
        //        return storage.Where(x => x.Key.Container == containerType &&
        //                                 x.Key.Index >= minSlot && x.Key.Index <= maxSlot)
        //                     .ToDictionary(x => x.Key, x => x.Value);
        //    }

        //    public bool TryGetItem(SlotKey slot, out ItemData item)
        //    {
        //        return storage.TryGetValue(slot, out item);
        //    }

        //    public int GetMaxOccupiedSlotIndex(SlotContainerType containerType)
        //    {
        //        return storage.Where(x => x.Key.Container == containerType)
        //                     .Select(x => x.Key.Index)
        //                     .DefaultIfEmpty(-1)
        //                     .Max();
        //    }

        //    public int GetContainerSize(SlotContainerType containerType)
        //    {
        //        return containerType switch
        //        {
        //            SlotContainerType.Inventory => maxInventorySize,
        //            SlotContainerType.QuickBar => maxQuickBarSize,
        //            SlotContainerType.Equipment => maxEquipmentSize,
        //            _ => 0
        //        };
        //    }

        //    #endregion


        //    #region 添加物品相关方法

        //    public bool AddItem(SlotKey slot, ItemData itemData)
        //    {
        //        if (!IsSlotValid(slot))
        //            return false;

        //        if (storage.TryGetValue(slot, out var existingItem))
        //        {
        //            if (CanStack(existingItem, itemData))
        //            {
        //                existingItem.ItemCount += itemData.ItemCount;
        //                return true;
        //            }
        //            return false; // 槽位被占用且不能堆叠
        //        }

        //        storage.Add(slot, itemData);
        //        UpdateLastItemSlot(slot);
        //        return true;
        //    }

        //    public bool AddItem(ItemData itemData, out SlotKey occupiedSlot)
        //    {
        //        // 首先尝试堆叠到现有物品
        //        var stackableSlot = FindStackableSlot(itemData);
        //        if (stackableSlot != SlotKey.Default)
        //        {
        //            storage[stackableSlot].ItemCount += itemData.ItemCount;
        //            occupiedSlot = stackableSlot;
        //            return true;
        //        }

        //        // 寻找空槽位
        //        var emptySlot = FindEmptySlot(SlotContainerType.Inventory);
        //        if (emptySlot != SlotKey.Default)
        //        {
        //            storage.Add(emptySlot, itemData);
        //            UpdateLastItemSlot(emptySlot);
        //            occupiedSlot = emptySlot;
        //            return true;
        //        }

        //        occupiedSlot = SlotKey.Default;
        //        return false;
        //    }

        //    private SlotKey FindStackableSlot(ItemData itemData)
        //    {
        //        if (!itemData.IsStack) return SlotKey.Default;

        //        return storage.FirstOrDefault(x =>
        //            x.Value.ItemTemplateId == itemData.ItemTemplateId &&
        //            x.Key.Container == SlotContainerType.Inventory)
        //            .Key;
        //    }

        //    private SlotKey FindEmptySlot(SlotContainerType containerType)
        //    {
        //        var maxSize = GetContainerSize(containerType);

        //        for (int i = 0; i < maxSize; i++)
        //        {
        //            var slot = new SlotKey(containerType, i);
        //            if (!storage.ContainsKey(slot))
        //                return slot;
        //        }
        //        return SlotKey.Default;
        //    }

        //    #endregion


        //    #region 交换物品相关方法

        //    public bool SwapItems(SlotKey slot1, SlotKey slot2)
        //    {
        //        if (!IsSlotValid(slot1) || !IsSlotValid(slot2))
        //            return false;

        //        // 同容器内交换
        //        if (slot1.Container == slot2.Container)
        //        {
        //            return SwapWithinSameContainer(slot1, slot2);
        //        }

        //        // 跨容器交换
        //        return SwapBetweenContainers(slot1, slot2);
        //    }

        //    private bool SwapWithinSameContainer(SlotKey slot1, SlotKey slot2)
        //    {
        //        var hasItem1 = storage.TryGetValue(slot1, out var item1);
        //        var hasItem2 = storage.TryGetValue(slot2, out var item2);

        //        if (hasItem1 && hasItem2)
        //        {
        //            storage[slot1] = item2;
        //            storage[slot2] = item1;
        //        }
        //        else if (hasItem1 && !hasItem2)
        //        {
        //            storage.Remove(slot1);
        //            storage.Add(slot2, item1);
        //        }
        //        else if (!hasItem1 && hasItem2)
        //        {
        //            storage.Remove(slot2);
        //            storage.Add(slot1, item2);
        //        }

        //        UpdateLastItemSlot();
        //        return true;
        //    }

        //    private bool SwapBetweenContainers(SlotKey slot1, SlotKey slot2)
        //    {
        //        // 检查装备限制等逻辑
        //        if (!CanItemMoveToContainer(slot1, slot2.Container) ||
        //            !CanItemMoveToContainer(slot2, slot1.Container))
        //            return false;

        //        var hasItem1 = storage.TryGetValue(slot1, out var item1);
        //        var hasItem2 = storage.TryGetValue(slot2, out var item2);

        //        if (hasItem1) storage.Remove(slot1);
        //        if (hasItem2) storage.Remove(slot2);

        //        if (hasItem1) storage.Add(slot2, item1);
        //        if (hasItem2) storage.Add(slot1, item2);

        //        UpdateLastItemSlot();
        //        return true;
        //    }

        //    private bool CanItemMoveToContainer(SlotKey sourceSlot, SlotContainerType targetContainer)
        //    {
        //        if (!storage.TryGetValue(sourceSlot, out var item))
        //            return true; // 空槽位可以移动

        //        // 添加装备类型检查等逻辑
        //        // 例如：检查物品是否可以装备到目标容器
        //        return true;
        //    }

        //    #endregion


        //    #region 删除物品相关方法

        //    public bool RemoveItem(SlotKey slot)
        //    {
        //        if (storage.Remove(slot))
        //        {
        //            UpdateLastItemSlot();
        //            return true;
        //        }
        //        return false;
        //    }

        //    public bool RemoveItem(SlotKey slot, int count)
        //    {
        //        if (count <= 0 || !storage.TryGetValue(slot, out var item))
        //            return false;

        //        if (item.ItemCount <= count)
        //        {
        //            return RemoveItem(slot);
        //        }
        //        else
        //        {
        //            item.ItemCount -= count;
        //            return true;
        //        }
        //    }

        //    #endregion


        //    #region 辅助方法

        //    private bool IsSlotValid(SlotKey slot)
        //    {
        //        var maxSize = GetContainerSize(slot.Container);
        //        return slot.Index >= 0 && slot.Index < maxSize;
        //    }

        //    private bool CanStack(ItemData existing, ItemData newItem)
        //    {
        //        return existing.ItemTemplateId == newItem.ItemTemplateId &&
        //               existing.IsStack &&
        //               newItem.IsStack;
        //    }

        //    private void UpdateLastItemSlot(SlotKey? newSlot = null)
        //    {
        //        if (newSlot.HasValue && newSlot.Value.Container == SlotContainerType.Inventory)
        //        {
        //            if (newSlot.Value.Index >= currentLastItemSlot.Index)
        //            {
        //                currentLastItemSlot = FindNextAvailableSlot();
        //            }
        //        }
        //        else
        //        {
        //            currentLastItemSlot = FindNextAvailableSlot();
        //        }
        //    }

        //    private SlotKey FindNextAvailableSlot()
        //    {
        //        for (int i = 0; i < maxInventorySize; i++)
        //        {
        //            var slot = new SlotKey(SlotContainerType.Inventory, i);
        //            if (!storage.ContainsKey(slot))
        //                return slot;
        //        }
        //        return new SlotKey(SlotContainerType.Inventory, maxInventorySize);
        //    }


        //    public bool HasEmptySlot(SlotContainerType containerType)
        //    {
        //        var maxSize = GetContainerSize(containerType);
        //        return storage.Count(x => x.Key.Container == containerType) < maxSize;
        //    }

        //    #endregion
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
        [Key(1)] public int Index = 0;

        public static readonly SlotKey Default = new SlotKey
        {
            Container = SlotContainerType.None,
            Index = 0
        };

        public SlotKey()
        {
            Container = SlotContainerType.None;
            Index = 0;
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
