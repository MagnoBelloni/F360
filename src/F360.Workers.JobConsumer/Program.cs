using F360.Workers.JobConsumer;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddCustomSerilog(builder.Configuration)
    .AddConfigureOptions(builder.Configuration)
    .AddMongoDb()
    .AddRepositories()
    .AddHttpClients()
    .AddRabbitMq(builder.Configuration);

var host = builder.Build();
host.Run();
