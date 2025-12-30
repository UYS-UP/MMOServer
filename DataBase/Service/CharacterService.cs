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
        public async Task<CreateCharacterDto> CreateCharacterAsync(string playerId, string name, int initialMapId)
        {
            try
            {
                if (await unitOfWork.Characters.AnyAsync(c => c.Name == name))
                {
                    return new CreateCharacterDto
                    {
                        character = null,
                        Sucess = false,
                        Message = "该昵称已被使用"
                    };
                }

                var newChar = new Character
                {
                    CharacterId = Guid.NewGuid().ToString(),
                    PlayerId = playerId,
                    Name = name,
                    Level = 1,
                    Hp = 100,
                    Gold = 0,
                    MapId = initialMapId,
                    X = 0,
                    Y = 0,
                    Z = 0,
                    CreateTime = DateTime.UtcNow,
                    LastLoginTime = DateTime.UtcNow,
                    ServerId = 0
                };

                await unitOfWork.Characters.AddAsync(newChar);

                // var starterWeapon = new WeaponItem { ... };
                // await unitOfWork.WeaponItems.AddAsync(starterWeapon);

                await unitOfWork.SaveChangesAsync();

                return new CreateCharacterDto
                {
                    character = newChar,
                    Sucess = true,
                    Message = "创建成功"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建角色失败: {ex}");
                return new CreateCharacterDto
                {
                    character = null,
                    Sucess = false,
                    Message = "创建角色失败"
                };
            }
        }

        /// <summary>
        /// 获取角色 (包括位置信息)
        /// </summary>
        public async Task<Character> GetCharacterFullAsync(string characterId)
        {
            return await unitOfWork.Characters.GetByIdAsync(characterId);

            // context.Characters.Include(c => c.WeaponItems).FirstOrDefault...
        }

        public async Task SaveCharacterStateAsync(string characterId, int hp, long exp, int mapId, System.Numerics.Vector3 pos, float yaw)
        {
            var character = await unitOfWork.Characters.GetByIdAsync(characterId);
            if (character != null)
            {
                character.Hp = hp;
                character.Exp = exp;
                character.MapId = mapId;
                character.X = pos.X;
                character.Y = pos.Y;
                character.Z = pos.Z;
                character.Yaw = yaw;
                character.LastLoginTime = DateTime.UtcNow;

                await unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<List<Character>> GetPlayerCharactersRawAsync(string playerId)
        {
            var result = await unitOfWork.Characters.FindAsync(c => c.PlayerId == playerId);
            return result.ToList();
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