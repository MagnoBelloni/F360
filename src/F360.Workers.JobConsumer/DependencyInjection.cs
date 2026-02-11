using F360.Domain.Dtos.Messages;
using F360.Domain.Enums;
using F360.Domain.Interfaces.Database.Repositories;
using F360.Domain.Interfaces.HttpClients;
using F360.Infrastructure.Configuration;
using F360.Infrastructure.Database.Configuration;
using F360.Infrastructure.Database.Repositories;
using F360.Infrastructure.HttpClients;
using F360.Workers.JobConsumer.Consumers;
using MassTransit;
using Serilog;

namespace F360.Workers.JobConsumer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCustomSerilog(this IServiceCollection services, IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
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
            services.AddScoped<IJobRepository, JobRepository>();

            return services;
        }

        public static IServiceCollection AddHttpClients(this IServiceCollection services)
        {
            services.AddHttpClient<IViaCepClient, ViaCepClient>();

            return services;
        }

        public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            var rabbitMqSettings = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()!;
            services.AddMassTransit(x =>
            {
                x.AddConsumer<JobMessageConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqSettings.Host, h =>
                    {
                        h.Username(rabbitMqSettings.Username);
                        h.Password(rabbitMqSettings.Password);
                    });

                    cfg.Message<JobMessage>(e =>
                    {
                        e.SetEntityName("JobMessage");
                    });

                    cfg.Publish<JobMessage>(p =>
                    {
                        p.ExchangeType = "direct";
                    });

                    cfg.Send<JobMessage>(s =>
                    {
                        s.UseRoutingKeyFormatter(context =>
                        {
                            var message = context.Message;
                            return message.Priority.ToString();
                        });
                    });

                    cfg.ReceiveEndpoint("f360.job.high", e =>
                    {
                        e.ConfigureConsumeTopology = false;
                        e.Bind<JobMessage>(b =>
                        {
                            b.RoutingKey = nameof(JobPriority.High);
                            b.ExchangeType = "direct";
                        });

                        e.ConfigureConsumer<JobMessageConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("f360.job.low", e =>
                    {
                        e.ConfigureConsumeTopology = false;
                        e.Bind<JobMessage>(b =>
                        {
                            b.RoutingKey = nameof(JobPriority.Low);
                            b.ExchangeType = "direct";
                        });

                        e.ConfigureConsumer<JobMessageConsumer>(context);
                    });
                });
            });

            return services;
        }
    }
}
