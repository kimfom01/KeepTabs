using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var mongoDb = builder.AddMongoDB("MongoDb")
    .WithDataVolume()
    .WithMongoExpress()
    .AddDatabase("DefaultDatabase");

builder.AddProject<KeepTabs>("KeepTabs")
    .WithReference(mongoDb)
    .WaitFor(mongoDb);

builder.Build().Run();