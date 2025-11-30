using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("keeptabsdb");

var api = builder.AddProject<KeepTabs>("KeepTabs")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.AddProject<KeepTabs_Worker>("Worker")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WaitFor(api);

await builder.Build()
    .RunAsync();