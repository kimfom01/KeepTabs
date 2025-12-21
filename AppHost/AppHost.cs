using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("keeptabsdb");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

var api = builder.AddProject<KeepTabs>("keeptabs")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

builder.AddProject<KeepTabs_Worker>("worker")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WaitFor(api);

await builder.Build()
    .RunAsync();