using F360.Application.UseCases;
using F360.Application.Validators;
using F360.Domain.Interfaces.Repositories;
using F360.Infrastructure.Configuration;
using F360.Infrastructure.Database.Configuration;
using F360.Infrastructure.Database.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

namespace F360.Api
{
    public static class DependencyInjection
    {
        public static void AddCustomSerilog(IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        public static IServiceCollection AddConfigureOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDb"));
            services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
            services.Configure<ApiKeySettings>(configuration.GetSection("ApiKey"));

            return services;
        }

        public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDb"));
            services.AddSingleton<MongoDbContext>();

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();

            return services;
        }

        public static IServiceCollection AddUseCases(this IServiceCollection services)
        {
            services.AddScoped<CreateJobUseCase>();
            services.AddScoped<CancelJobUseCase>();
            services.AddScoped<GetJobUseCase>();

            return services;
        }

        public static IServiceCollection AddFluentValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<CreateJobRequestValidator>();
            services.AddFluentValidationAutoValidation();

            return services;
        }

        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var mongoDbSettings = configuration.GetSection("MongoDb").Get<MongoDbSettings>()!;
            var rabbitMqSettings = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()!;

            services.AddHealthChecks()
                .AddMongoDb(
                    mongodbConnectionString: mongoDbSettings.ConnectionString,
                    name: "mongodb",
                    timeout: TimeSpan.FromSeconds(5))
                .AddRabbitMQ(
                    rabbitConnectionString: $"amqp://{rabbitMqSettings.Username}:{rabbitMqSettings.Password}@{rabbitMqSettings.Host}",
                    name: "rabbitmq",
                    timeout: TimeSpan.FromSeconds(5));

            return services;
        }

        public static IServiceCollection AddPresentationApi(this IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            return services;
        }
    }
}
