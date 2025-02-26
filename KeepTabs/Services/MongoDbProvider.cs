using KeepTabs.Entities;
using MongoDB.Driver;

namespace KeepTabs.Services;

public class MongoDbProvider
{
    public MongoDbProvider(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultDatabase");
        var mongoUrl = MongoUrl.Create(connectionString);
        var mongoClient = new MongoClient(mongoUrl);
        
        Collection = mongoClient
            .GetDatabase(mongoUrl.DatabaseName)
            .GetCollection<JobTracking>(configuration
                .GetConnectionString("MongoCollection"));
    }

    public IMongoCollection<JobTracking> Collection { get; }
}