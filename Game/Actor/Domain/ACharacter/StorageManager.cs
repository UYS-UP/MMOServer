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

        private readonly Dictionary<EquipType, SlotKey> equipSlotMapping = new()
        {
            { EquipType.Weapon,  new SlotKey(SlotContainerType.Equipment, 0)},
            { EquipType.Helmet ,  new SlotKey(SlotContainerType.Equipment, 1) },
            { EquipType.Clothes ,  new SlotKey(SlotContainerType.Equipment, 2) },
            { EquipType.Pants ,  new SlotKey(SlotContainerType.Equipment, 3) },
            { EquipType.Shoes ,  new SlotKey(SlotContainerType.Equipment, 4) },
            { EquipType.Necklace ,  new SlotKey(SlotContainerType.Equipment, 5) },
        };

        public bool TryGetItem(SlotKey key, out ItemData item) => storage.TryGetValue(key, out item);
        public bool TryGetEquipSlotIndex(EquipType equipType, out SlotKey slot) => equipSlotMapping.TryGetValue(equipType, out slot);
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
                if(equipSlotMapping.TryGetValue(equip.EquipType, out var requiredSlot))
                {
                    return targetSlot == requiredSlot;
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
