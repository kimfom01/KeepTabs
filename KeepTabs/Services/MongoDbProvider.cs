using MongoDB.Driver;

namespace KeepTabs.Services;

public class MongoDbProvider
{
    public MongoDbProvider(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoConnection");

        var mongoUrl = MongoUrl.Create(connectionString);
        var mongoClient = new MongoClient(mongoUrl);
        MongoDatabase = mongoClient.GetDatabase(mongoUrl.DatabaseName);
    }

    public IMongoDatabase MongoDatabase { get; }
}