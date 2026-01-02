using Microsoft.EntityFrameworkCore;
using Server.DataBase.DTO;
using Server.DataBase.Entities;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;

namespace Server.DataBase.Service
{
    public class CharacterService
    {
        private readonly UnitOfWork unitOfWork;

        public CharacterService(UnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 创建角色 (简化版)
        /// </summary>
        public async Task<CreateCharacterDto> CreateCharacterAsync(string playerId, string name, int initialMapId, int serverId)
        {
            try
            {
                if (await unitOfWork.Characters.AnyAsync(c => c.Name == name))
                {
                    return new CreateCharacterDto { Sucess = false, Message = "该昵称已被使用" };
                }

                var newChar = new Character
                {
                    CharacterId = Guid.NewGuid().ToString(),
                    PlayerId = playerId,
                    Name = name,
                    Hp = 1000,
                    Level = 1,
                    Exp = 0,
                    Gold = 0,
                    MapId = initialMapId,
                    ServerId = serverId,
                    Attributes = new Dictionary<AttributeType, float>
                {
                    { AttributeType.MaxHp, 1000},
                    { AttributeType.MaxExp, 1000},
                    { AttributeType.Attack, 50 }
                },
                    CreateTime = DateTime.UtcNow,
                    LastLoginTime = DateTime.UtcNow
                };

                await unitOfWork.Characters.AddAsync(newChar);

                // 初始化基础武器熟练度 (可选)
                await unitOfWork.WeaponMasteries.AddAsync(new WeaponMastery
                {
                    CharacterId = newChar.CharacterId,
                    WeaponType = WeaponType.Katana,
                    Level = 1
                });

                await unitOfWork.SaveChangesAsync();

                return new CreateCharacterDto { character = newChar, Sucess = true, Message = "创建成功" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建角色失败: {ex}");
                return new CreateCharacterDto { Sucess = false, Message = "系统错误" };
            }
        }

        /// <summary>
        /// 获取角色 (包括位置信息)
        /// </summary>
        public async Task<Character> GetCharacterFullAsync(string characterId)
        {
            return await unitOfWork.Characters.Query()
                .Include(c => c.InventoryItems)
                .Include(c => c.WeaponMasteries)
                .FirstOrDefaultAsync(c => c.CharacterId == characterId);
        }

        public async Task SaveCharacterStateAsync(Character character, int hp, long exp, int mapId, System.Numerics.Vector3 pos, float yaw)
        {
            character.LastLoginTime = DateTime.UtcNow;
            unitOfWork.Characters.Update(character);
            await unitOfWork.SaveChangesAsync();
        }

        public async Task<List<Character>> GetPlayerCharactersRawAsync(string playerId)
        {
            return (await unitOfWork.Characters.FindAsync(c => c.PlayerId == playerId)).ToList();
        }

        // 删除角色
        public async Task<DeleteCharacterDto> DeleteCharacterAsync(string characterId)
        {
            await unitOfWork.Characters.DeleteByIdAsync(characterId);
            await unitOfWork.SaveChangesAsync();
            return new DeleteCharacterDto
            {
                Sucess = true,
                Message = "删除成功"
            };
        }
    }
}