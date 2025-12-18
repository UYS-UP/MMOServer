using Microsoft.EntityFrameworkCore;
using Server.DataBase.Entities;
using Server.DataBase.Repositories;
using Server.Game.Contracts.Network;
using Server.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Server.DataBase.Service
{
    /// <summary>
    /// 玩家服务 - 重构版本
    /// 使用EF Core + UnitOfWork模式
    /// </summary>
    public class PlayerService
    {
        private readonly UnitOfWork unitOfWork;
        private readonly CharacterService roleService;

        public PlayerService(UnitOfWork unitOfWork, CharacterService roleService)
        {
            this.unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            this.roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        /// <summary>
        /// 用户登录 (使用EF Core)
        /// </summary>
        public async Task<ResponseMessage<NetworkPlayer>> LoginAsync(string username, string password)
        {
            try
            {
                // 使用LINQ查询,包含关联的角色数据
                var player = await unitOfWork.Players
                    .FirstOrDefaultAsync(p => p.Username == username && p.Password == password);

                if (player == null)
                {
                    return ResponseMessage<NetworkPlayer>.Fail("用户名不存在或密码错误");
                }

                // 更新最后登录时间 (EF Core自动跟踪变更)
                player.LastLoginTime = DateTime.UtcNow;
                await unitOfWork.SaveChangesAsync();

                // 获取角色列表
                var rolesResponse = await roleService.GetPlayerCharactersAsync(player.PlayerId);
                if (rolesResponse.Code != StateCode.Success)
                {
                    return ResponseMessage<NetworkPlayer>.Fail("获取角色列表失败", StateCode.InternalError);
                }

                // 创建返回数据
                var playerData = NetworkPlayer.CreatePlayerData(player, rolesResponse.Data);
                return ResponseMessage<NetworkPlayer>.Success(playerData, "登录成功");
            }
            catch (Exception ex)
            {
                // 记录日志
                Console.WriteLine($"登录失败: {ex}");
                return ResponseMessage<NetworkPlayer>.Fail("系统错误", StateCode.InternalError);
            }
        }

        /// <summary>
        /// 用户登录 (使用Dapper,高性能版本)
        /// </summary>
        public async Task<ResponseMessage<NetworkPlayer>> LoginWithDapperAsync(string username, string password)
        {
            try
            {
                // 使用Dapper进行高性能查询
                var player = await unitOfWork.Players.QueryFirstOrDefaultAsync(
                    "SELECT * FROM players WHERE username = @username AND password = @password LIMIT 1",
                    new { username, password }
                );

                if (player == null)
                {
                    return ResponseMessage<NetworkPlayer>.Fail("用户名不存在或密码错误");
                }

                // 使用Dapper更新
                await unitOfWork.Players.ExecuteAsync(
                    "UPDATE players SET lastLoginTime = @lastLoginTime WHERE playerId = @playerId",
                    new { lastLoginTime = DateTime.UtcNow, playerId = player.PlayerId }
                );

                // 获取角色列表
                var rolesResponse = await roleService.GetPlayerCharactersAsync(player.PlayerId);
                if (rolesResponse.Code != StateCode.Success)
                {
                    return ResponseMessage<NetworkPlayer>.Fail("获取角色列表失败", StateCode.InternalError);
                }

                var playerData = NetworkPlayer.CreatePlayerData(player, rolesResponse.Data);
                return ResponseMessage<NetworkPlayer>.Success(playerData, "登录成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"登录失败: {ex}");
                return ResponseMessage<NetworkPlayer>.Fail("系统错误", StateCode.InternalError);
            }
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        public async Task<ResponseMessage<string>> RegisterAsync(string username, string password, string code)
        {
            try
            {
                // 验证
                if (!Validator.IsValidEmail(username))
                {
                    return ResponseMessage<string>.Fail("用户名必须输入邮箱");
                }

                if (!Validator.IsValidPassword(password))
                {
                    return ResponseMessage<string>.Fail("密码必须不包含特殊字符以及至少8位");
                }

                // 检查用户名是否已存在
                var exists = await unitOfWork.Players.AnyAsync(p => p.Username == username);
                if (exists)
                {
                    return ResponseMessage<string>.Fail("该用户名已经被注册");
                }

                // 创建新玩家
                var newPlayer = new Player
                {
                    PlayerId = Guid.NewGuid().ToString(),
                    Username = username,
                    Password = password, // 注意: 实际应该使用哈希加密
                    CreatedTime = DateTime.UtcNow,
                    LastLoginTime = DateTime.MinValue
                };

                await unitOfWork.Players.AddAsync(newPlayer);
                await unitOfWork.SaveChangesAsync();

                return ResponseMessage<string>.Success(username, "注册成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注册失败: {ex.Message}");
                return ResponseMessage<string>.Fail("系统错误", StateCode.InternalError);
            }
        }

        /// <summary>
        /// 根据PlayerId获取玩家信息
        /// </summary>
        public async Task<ResponseMessage<Player>> GetPlayerByIdAsync(string playerId)
        {
            try
            {
                var player = await unitOfWork.Players.GetByIdAsync(playerId);

                if (player == null)
                {
                    return ResponseMessage<Player>.Fail("玩家不存在");
                }

                return ResponseMessage<Player>.Success(player);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取玩家失败: {ex.Message}");
                return ResponseMessage<Player>.Fail("系统错误", StateCode.InternalError);
            }
        }

        /// <summary>
        /// 更新玩家最后登录时间
        /// </summary>
        public async Task<ResponseMessage<bool>> UpdateLastLoginTimeAsync(string playerId)
        {
            try
            {
                var player = await unitOfWork.Players.GetByIdAsync(playerId);

                if (player == null)
                {
                    return ResponseMessage<bool>.Fail("玩家不存在");
                }

                player.LastLoginTime = DateTime.UtcNow;
                await unitOfWork.SaveChangesAsync();

                return ResponseMessage<bool>.Success(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新登录时间失败: {ex.Message}");
                return ResponseMessage<bool>.Fail("系统错误", StateCode.InternalError);
            }
        }
    }
}

