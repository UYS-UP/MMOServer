using Microsoft.Extensions.DependencyInjection;
using Server.Data;
using Server.Data.Game.Json.ItemJson;
using Server.DataBase;
using Server.DataBase.Service;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain;
using Server.Game.Contracts.Common;
using Server.Game.World.Skill;
using Server.Network;
using Server.Utility;
using System.Diagnostics;
using System.Net;


// 明日计划:
// 1. 实现技能Phase，完成技能指示器
// 2. 完成Buff系统
// 3. 修复AI
// 4. 完成副本内死亡倒计时重生

public class Program
{
    private static async Task Main(string[] args)
    {
        ItemJsonSerializer.Deserializer("D:\\Project\\UnityDemo\\MMORPGServer\\Data\\Game\\Json\\ItemJson\\ItemConfigs.json");
        SkillTimelineJsonSerializer.Deserializer("D:\\Project\\UnityDemo\\MMORPGServer\\Data\\Game\\Json\\SkillJson\\SkillTimelineConfig.json");
        // Console.WriteLine($"{ExcelValue.RoleBaseTable[((int)RoleType.Warrior).ToString()].Attack}");
        // 创建服务器端点
        var endPoint = new IPEndPoint(IPAddress.Any, 8888);
        ITransport transport = new TcpTransport(endPoint);
        SessionManager sessionManager = new SessionManager();
   

        var services = new ServiceCollection();
        var connectionString = "Server=localhost;Port=3306;Database=mmorpg;Uid=root;Pwd=123456;";
        services.AddGameDatabase(connectionString);
        var serviceProvider = services.BuildServiceProvider();

        var playerService = serviceProvider.GetRequiredService<PlayerService>();
        var characterService = serviceProvider.GetRequiredService<CharacterService>();
        var unitOfWork = serviceProvider.GetRequiredService<UnitOfWork>();

        DatabaseService.Initalize(playerService, characterService);
        IActorSystem system = new ActorSystem();
        await system.CreateActor(
            new GameServerActor(GameField.GetActor<GameServerActor>(), 
            new GameServer(transport, sessionManager), 
            system)
            );
        Console.ReadLine();
        Console.WriteLine("ActorGameServer停止");
        await system.StopAllActors();

    }

   

}

