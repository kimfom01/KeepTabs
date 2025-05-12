using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var mongoMain = builder.AddMongoDB("MongoMainDb")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithMongoExpress()
    .AddDatabase("MongoMain");

var mongoHangfire = builder.AddMongoDB("MongoHangfireDb")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithMongoExpress()
    .AddDatabase("MongoHangfire");

builder.AddProject<KeepTabs>("KeepTabs")
    .WithReference(mongoMain)
    .WithReference(mongoHangfire)
    .WaitFor(mongoMain)
    .WaitFor(mongoHangfire);

builder.Build().Run();