using F360.Domain.Interfaces;
using F360.Domain.Interfaces.Repositories;
using F360.Infrastructure.Configuration;
using F360.Infrastructure.Database.Configuration;
using F360.Infrastructure.Database.Repositories;
using F360.Infrastructure.Messaging;
using MassTransit;
using Serilog;

namespace F360.Workers.OutboxWorker
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCustomSerilog(this IServiceCollection services, IConfiguration configuration) 
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                //.Enrich.FromLogContext()
                //.WriteTo.Console()
                .CreateLogger();

            services.AddSerilog();

            return services;
        }

        public static IServiceCollection AddConfigureOptions(this IServiceCollection services, IConfiguration configuration) 
        {
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDb"));
            services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));

            return services;
        }

        public static IServiceCollection AddMongoDb(this IServiceCollection services) 
        {
            services.AddSingleton<MongoDbContext>();

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services) 
        {
            services.AddScoped<IOutboxRepository, OutboxRepository>();

            return services;
        }

        public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration) 
        {
            var rabbitMqSettings = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()!;

            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqSettings.Host, h =>
                    {
                        h.Username(rabbitMqSettings.Username);
                        h.Password(rabbitMqSettings.Password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            services.AddScoped<IMessagePublisher, MessagePublisher>();

            return services;
        }

        public static IServiceCollection AddHostedService(this IServiceCollection services) 
        {
            services.AddHostedService<Worker>();

            return services;
        }
    }
}
