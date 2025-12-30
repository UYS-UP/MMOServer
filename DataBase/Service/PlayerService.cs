using Microsoft.EntityFrameworkCore;
using Server.DataBase.DTO;
using Server.DataBase.Entities;
using Server.Game.Contracts.Network;
using Server.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MessagePack.GeneratedMessagePackResolver.Server.Game.Actor.Domain;

namespace Server.DataBase.Service
{
    public class PlayerService
    {
        private readonly UnitOfWork unitOfWork;
        private readonly CharacterService characterService;

        public PlayerService(UnitOfWork unitOfWork, CharacterService roleService)
        {
            this.unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            this.characterService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        public async Task<LoginResultDto> LoginAsync(string username, string password)
        {
            try
            {
                
                var player = await unitOfWork.Players
                    .FirstOrDefaultAsync(p => p.Username == username && p.Password == password);

                if (player == null)
                {
                    return new LoginResultDto
                    {
                        Succes = false,
                        Message = "账号或密码错误",
                        Player = null,
                        Characters = null
                    };
                }

                player.LastLoginTime = DateTime.UtcNow;
                await unitOfWork.SaveChangesAsync();

                // 获取角色列表
                var characters = await characterService.GetPlayerCharactersRawAsync(player.PlayerId);
                return new LoginResultDto
                {
                    Succes = true,
                    Message = "登录成功",
                    Player = player,
                    Characters = characters
                };
            }
            catch (Exception ex)
            {
                // 记录日志
                Console.WriteLine($"登录失败: {ex}");
                return new LoginResultDto
                {
                    Succes = false,
                    Message = "系统错误",
                    Player = null,
                    Characters = null
                };
            }
        }


        public async Task<RegisterResultDto> RegisterAsync(string username, string password, string code)
        {
            try
            {
                // 验证
                if (!Validator.IsValidEmail(username))
                {

                    return new RegisterResultDto { 
                        Succes = false,
                        Message = "用户名必须为有效邮箱",
                        Username = username,
                    };
                }

                if (!Validator.IsValidPassword(password))
                {
                    return new RegisterResultDto
                    {
                        Succes = false,
                        Message = "密码必须不包含特殊字符以及至少8位",
                        Username = username,
                    };
                }

                // 检查用户名是否已存在
                var exists = await unitOfWork.Players.AnyAsync(p => p.Username == username);
                if (exists)
                {
                    return new RegisterResultDto
                    {
                        Succes = false,
                        Message = "该邮箱已被注册",
                        Username = username,
                    };
                }

                // 创建新玩家
                var newPlayer = new Player
                {
                    PlayerId = Guid.NewGuid().ToString(),
                    Username = username,
                    Password = password,
                    CreatedTime = DateTime.UtcNow,
                    LastLoginTime = DateTime.MinValue
                };

                await unitOfWork.Players.AddAsync(newPlayer);
                await unitOfWork.SaveChangesAsync();

                return new RegisterResultDto
                {
                    Succes = true,
                    Message = "注册成功",
                    Username = username,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注册失败: {ex.Message}");
                return new RegisterResultDto
                {
                    Succes = false,
                    Message = "系统错误",
                    Username = username,
                };
            }
        }
    }
}

