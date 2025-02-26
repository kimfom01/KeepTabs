using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var mongoDb = builder.AddMongoDB("MongoDb")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithMongoExpress()
    .AddDatabase("DefaultDatabase");

builder.AddProject<KeepTabs>("KeepTabs")
    .WithReference(mongoDb)
    .WaitFor(mongoDb);

builder.Build().Run();