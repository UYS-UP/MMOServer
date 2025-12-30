using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Server.DataBase.Service;
using System;

namespace Server.DataBase
{
    /// <summary>
    /// 依赖注入扩展方法
    /// 用于配置数据库和服务
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加数据库服务
        /// </summary>
        public static IServiceCollection AddGameDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            // 获取连接字符串
            var connectionString = configuration.GetConnectionString("GameDatabase");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("数据库连接字符串未配置");
            }

            // 配置EF Core DbContext
            services.AddDbContext<GameDbContext>(options =>
            {
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    mysqlOptions =>
                    {
                        // 启用重试机制
                        mysqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null
                        );

                        // 命令超时时间
                        mysqlOptions.CommandTimeout(30);
                    }
                );

                // 开发环境配置
#if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
#endif
            });

            // 注册仓储和工作单元
            services.AddScoped<UnitOfWork>();

            // 注册服务
            services.AddScoped<PlayerService>();
            services.AddScoped<CharacterService>();

            return services;
        }

        /// <summary>
        /// 添加数据库服务 (使用连接字符串)
        /// </summary>
        public static IServiceCollection AddGameDatabase(this IServiceCollection services, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            // 配置EF Core DbContext
            services.AddDbContext<GameDbContext>(options =>
            {
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    mysqlOptions =>
                    {
                        mysqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null
                        );
                    }
                );

#if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
#endif
            });

            // 注册仓储和工作单元
            services.AddScoped<UnitOfWork>();

            // 注册服务
            services.AddScoped<PlayerService>();
            services.AddScoped<CharacterService>();


            return services;
        }
    }
}

