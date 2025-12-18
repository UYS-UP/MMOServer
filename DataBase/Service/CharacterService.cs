using Microsoft.EntityFrameworkCore;
using Server.DataBase.Entities;
using Server.DataBase.Repositories;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Server.DataBase.Service
{
    /// <summary>
    /// 角色服务 - 重构版本
    /// 使用EF Core + UnitOfWork模式
    /// </summary>
    public class CharacterService
    {
        private readonly UnitOfWork unitOfWork;

        public CharacterService(UnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// 创建角色 (使用EF Core事务)
        /// </summary>
        public async Task<(ResponseMessage<List<NetworkCharacter>>, Character)> CreateCharacterAsync(string playerId, Character character)
        {
            // 开始事务
            await unitOfWork.BeginTransactionAsync();

            try
            {
                // 1. 检查角色名是否已存在
                var exists = await unitOfWork.Characters.AnyAsync(r => r.CharacterName == character.CharacterName);
                if (exists)
                {
                    await unitOfWork.RollbackAsync();
                    return (ResponseMessage<List<NetworkCharacter>>.Fail("该昵称已经被注册"), null);
                }

                // 2. 创建实体位置信息
                var entity = new Entity
                {
                    EntityId = HelperUtility.GetKey(),
                    EntityType = EntityType.Character,
                    X = 0f,
                    Y = 0f,
                    Z = 0f,
                    Yaw = 0f,
                    RegionId = "001", // 应该从配置文件读取新手村区域ID
                    LastUpdated = DateTime.UtcNow
                };
                await unitOfWork.Entities.AddAsync(entity);

                // 3. 创建角色
                var creareCharacter = new Character
                {
                    CharacterId = HelperUtility.GetKey(),
                    PlayerId = playerId,
                    EntityId = entity.EntityId,
                    CharacterName = character.CharacterName,
                    Level = character.Level > 0 ? character.Level : 1,
                    HP = character.HP,
                    MP = character.MP,
                    Gold = character.Gold,
                    Profession = character.Profession,
                    LastLoginTime = DateTime.UtcNow,
                    EX = character.EX,
                    MaxHp = character.MaxHp,
                    MaxMp = character.MaxMp,
                    MaxEx = character.MaxEx
                };
                await unitOfWork.Characters.AddAsync(creareCharacter);

                // 4. 保存并提交事务
                await unitOfWork.CommitAsync();

                // 5. 返回该玩家的所有角色
                var characters = await GetPlayerCharactersAsync(playerId);
                return (characters, creareCharacter);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                Console.WriteLine($"创建角色失败: {ex.Message}");
                return (ResponseMessage<List<NetworkCharacter>>.Fail("创建角色失败", StateCode.InternalError), null);
            }
        }

        /// <summary>
        /// 获取玩家的所有角色
        /// </summary>
        public async Task<ResponseMessage<List<NetworkCharacter>>> GetPlayerCharactersAsync(string playerId)
        {
            try
            {
                // 查询玩家的所有角色
                var roles = await unitOfWork.Characters
                    .FindAsync(r => r.PlayerId == playerId);

                // 转换为网络传输对象
                var networkCharacters = roles.Select(r => new NetworkCharacter
                {
                    CharacterId = r.CharacterId,
                    PlayerId = r.PlayerId,
                    EntityId = r.EntityId,
                    Name = r.CharacterName,
                    Level = r.Level,
                    Gold = r.Gold,
                    Profession = r.Profession
                }).ToList();

                return ResponseMessage<List<NetworkCharacter>>.Success(networkCharacters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取角色列表失败: {ex.Message}");
                return ResponseMessage<List<NetworkCharacter>>.Fail("获取角色列表失败", StateCode.InternalError);
            }
        }


        public async Task<ResponseMessage<Character>> GetCharacterByNameAsync(string characterName)
        {
            try
            {
                var roles = await unitOfWork.Characters.FindAsync(r => r.CharacterName == characterName);
                var role = roles.FirstOrDefault();
                if (role == null)
                {
                    return ResponseMessage<Character>.Fail("角色不存在", StateCode.InternalError);
                }

                var entity = await unitOfWork.Entities.GetByIdAsync(role.EntityId);
                if (entity == null)
                {
                    return ResponseMessage<Character>.Fail("实体不存在", StateCode.InternalError);
                }

                return ResponseMessage<Character>.Success(role);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取角色详情失败: {ex.Message}");
                return ResponseMessage<Character>.Fail("获取角色详情失败", StateCode.InternalError);
            }
        }

        /// <summary>
        /// 根据角色ID获取角色详细信息
        /// </summary>
        public async Task<ResponseMessage<Character>> GetCharacterByIdAsync(string CharacterId)
        {
            try
            {
                var role = await unitOfWork.Characters.GetByIdAsync(CharacterId);


                if (role == null)
                {
                    return ResponseMessage<Character>.Fail("角色不存在", StateCode.InternalError);
                }

                // 预加载Entity
                var entity = await unitOfWork.Entities.GetByIdAsync(role.EntityId);
                if (entity == null)
                {
                    return ResponseMessage<Character>.Fail("实体不存在", StateCode.InternalError);
                }

                return ResponseMessage<Character>.Success(role);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取角色详情失败: {ex.Message}");
                return ResponseMessage<Character>.Fail("获取角色详情失败", StateCode.InternalError);
            }
        }


        /// <summary>
        /// 更新角色位置
        /// </summary>
        public async Task<ResponseMessage<bool>> UpdateCharacterPositionAsync(string entityId, float x, float y, float z, float yaw)
        {
            try
            {
                var entity = await unitOfWork.Entities.GetByIdAsync(entityId);

                if (entity == null)
                {
                    return ResponseMessage<bool>.Fail("实体不存在");
                }

                // EF Core自动跟踪变更
                entity.X = x;
                entity.Y = y;
                entity.Z = z;
                entity.Yaw = yaw;
                entity.LastUpdated = DateTime.UtcNow;

                await unitOfWork.SaveChangesAsync();

                return ResponseMessage<bool>.Success(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新位置失败: {ex.Message}");
                return ResponseMessage<bool>.Fail("更新位置失败", StateCode.InternalError);
            }
        }

        /// <summary>
        /// 更新角色属性 (HP, MP, EX等)
        /// </summary>
        public async Task<ResponseMessage<bool>> UpdateCharacterAttributesAsync(string CharacterId, int? hp = null, int? mp = null, int? ex = null, int? gold = null)
        {
            try
            {
                var role = await unitOfWork.Characters.GetByIdAsync(CharacterId);

                if (role == null)
                {
                    return ResponseMessage<bool>.Fail("角色不存在");
                }

                // 只更新传入的属性
                if (hp.HasValue) role.HP = hp.Value;
                if (mp.HasValue) role.MP = mp.Value;
                if (ex.HasValue) role.EX = ex.Value;
                if (gold.HasValue) role.Gold = gold.Value;

                await unitOfWork.SaveChangesAsync();

                return ResponseMessage<bool>.Success(true);
            }
            catch (Exception)
            {
                return ResponseMessage<bool>.Fail("更新角色属性失败", StateCode.InternalError);
            }
        }

        public async Task<ResponseMessage<bool>> UpdateCharacterWorldPositionAsync(string entityId, string regionId, Vector3 pos)
        {
            try
            {
                var entity = await unitOfWork.Entities.GetByIdAsync(entityId);
                if(entity == null)
                {
                    return ResponseMessage<bool>.Fail("角色不存在");
                }

                entity.RegionId = regionId;
                entity.X = pos.X;
                entity.Y = pos.Y;
                entity.Z = pos.Z;
                await unitOfWork.SaveChangesAsync();
                return ResponseMessage<bool>.Success(true);
            }
            catch (Exception)
            {
                return ResponseMessage<bool>.Fail("更新角色区域位置失败", StateCode.InternalError);
            }
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        public async Task<ResponseMessage<bool>> DeleteCharacterAsync(string playerId, string CharacterId)
        {
            await unitOfWork.BeginTransactionAsync();

            try
            {
                // 查询角色
                var roles = await unitOfWork.Characters.FindAsync(r => r.PlayerId == playerId && r.CharacterId == CharacterId);
                var role = roles.FirstOrDefault();

                if (role == null)
                {
                    await unitOfWork.RollbackAsync();
                    return ResponseMessage<bool>.Fail("角色不存在");
                }

                // 删除角色 (级联删除会自动删除Entity)
                unitOfWork.Characters.Delete(role);

                await unitOfWork.CommitAsync();

                return ResponseMessage<bool>.Success(true, "删除成功");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                Console.WriteLine($"删除角色失败: {ex.Message}");
                return ResponseMessage<bool>.Fail("删除角色失败", StateCode.InternalError);
            }
        }
    }
}

