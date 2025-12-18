using Microsoft.Extensions.DependencyInjection;
using Server.Data;
using Server.DataBase;
using Server.DataBase.Configuration;
using Server.DataBase.Data;
using Server.DataBase.Repositories;
using Server.DataBase.Service;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Region.Skill;
using Server.Game.Actor.Hosting;
using Server.Game.Service;
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
        SkillTimelineJsonSerializer.Deserializer("D:\\Project\\UnityDemo\\MMORPGServer\\Data\\Game\\SkillTimelineConfig.json");
        DataLoader.LoadAllExcel();
        DataLoader.LoadAllJson();
        // Console.WriteLine($"{ExcelValue.RoleBaseTable[((int)RoleType.Warrior).ToString()].Attack}");
        // 创建服务器端点
        var endPoint = new IPEndPoint(IPAddress.Any, 9999);
        ITransport transport = new TcpTransport(endPoint);
        SessionManager sessionManager = new SessionManager();
        IActorSystem system =  new ActorSystem();
        system.CreateActor(new GameServerActor("GameServerActor", new GameServer(transport, sessionManager), system));
        var services = new ServiceCollection();
        var connectionString = "Server=localhost;Port=3306;Database=mmorpg;Uid=root;Pwd=123456;";
        services.AddGameDatabase(connectionString);
        var serviceProvider = services.BuildServiceProvider();

        var playerService = serviceProvider.GetRequiredService<PlayerService>();
        var roleService = serviceProvider.GetRequiredService<CharacterService>();
        var friendService = serviceProvider.GetRequiredService<FriendService>();
        var unitOfWork = serviceProvider.GetRequiredService<UnitOfWork>();

        DatabaseService.Initalize(playerService, roleService, friendService);

        var gameServer = (GameServerActor)system.GetActor("GameServerActor");
        await gameServer.StartAsync();
        Console.ReadLine();
        Console.WriteLine("ActorGameServer停止");
        gameServer.Stop();

    }

   

}

