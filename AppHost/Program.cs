using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var posgresdb = builder.AddPostgres("Postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("keeptabsdb");

builder.AddProject<KeepTabs>("KeepTabs")
    .WithReference(posgresdb)
    .WaitFor(posgresdb);

await builder.Build()
    .RunAsync();