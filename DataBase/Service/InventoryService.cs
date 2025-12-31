using Server.DataBase.Entities;
using Server.Game.Actor.Domain.ACharacter;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.DataBase.Service
{
    public class InventoryService
    {
        private readonly UnitOfWork unitOfWork;

        public InventoryService(UnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 获取角色所有背包物品
        /// </summary>
        public async Task<List<InventoryItem>> GetInventoryAsync(string characterId)
        {
            var items = await unitOfWork.InventoryItems.FindAsync(i => i.CharacterId == characterId);
            return new List<InventoryItem>(items);
        }

        /// <summary>
        /// 添加物品 (通用)
        /// </summary>
        public async Task AddItemAsync(string characterId, SlotKey slot, ItemData data)
        {
            var newItem = new InventoryItem
            {
                CharacterId = characterId,
                TemplateId = data.TemplateId,
                InstanceId = data.InstanceId,
                ItemType = data.ItemType,
                Count = data.ItemCount,
                SlotContainer = slot.Container,
                SlotIndex = slot.Index, // 需要查找空位
                ForgeLevel = 0,
                // 初始化动态数据
                DynamicData = new EquipDynamicData()
            };

            await unitOfWork.InventoryItems.AddAsync(newItem);
            await unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 保存背包变动
        /// </summary>
        public async Task SaveInventoryChangesAsync(IEnumerable<InventoryItem> modifiedItems, IEnumerable<long> deletedItemDbIds)
        {
            await unitOfWork.BeginTransactionAsync();
            try
            {
                // 删除
                foreach (var id in deletedItemDbIds)
                {
                    await unitOfWork.InventoryItems.DeleteByIdAsync(id);
                }

                // 更新或新增
                foreach (var item in modifiedItems)
                {
                    if (item.DbId == 0)
                        await unitOfWork.InventoryItems.AddAsync(item);
                    else
                        unitOfWork.InventoryItems.Update(item);
                }

                await unitOfWork.CommitAsync();
            }
            catch
            {
                await unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
