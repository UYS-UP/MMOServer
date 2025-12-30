using Server.DataBase.Entities;
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
        /// 获取角色所有武器
        /// </summary>
        public async Task<List<WeaponItem>> GetWeaponsAsync(string characterId)
        {
            var weapons = await unitOfWork.WeaponItems.FindAsync(w => w.CharacterId == characterId);
            return new List<WeaponItem>(weapons);
        }

        /// <summary>
        /// 保存背包变动 (通常在下线或定时保存时调用)
        /// </summary>
        public async Task SaveInventoryChangesAsync(IEnumerable<InventoryItem> modifiedItems, IEnumerable<long> deletedItemDbIds)
        {
            await unitOfWork.BeginTransactionAsync();
            try
            {
                // 处理删除
                foreach (var id in deletedItemDbIds)
                {
                    await unitOfWork.InventoryItems.DeleteByIdAsync(id);
                }

                // 处理更新或新增 (EF Core Update/Add 智能识别)
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

        /// <summary>
        /// 添加新武器
        /// </summary>
        public async Task AddWeaponAsync(string characterId, int templateId)
        {
            var weapon = new WeaponItem
            {
                WeaponDbId = Guid.NewGuid().ToString(),
                CharacterId = characterId,
                TemplateId = templateId,
                Level = 1,
                Exp = 0,
                StarLevel = 0,
                IsLocked = false
            };

            await unitOfWork.WeaponItems.AddAsync(weapon);
            await unitOfWork.SaveChangesAsync();
        }
    }
}