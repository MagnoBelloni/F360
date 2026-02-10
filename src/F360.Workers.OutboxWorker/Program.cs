using F360.Workers.OutboxWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddCustomSerilog(builder.Configuration)
    .AddConfigureOptions(builder.Configuration)
    .AddMongoDb()
    .AddRepositories()
    .AddRabbitMq(builder.Configuration)
    .AddHostedService();

var host = builder.Build();
host.Run();
