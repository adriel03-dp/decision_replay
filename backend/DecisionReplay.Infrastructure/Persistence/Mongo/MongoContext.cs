using MongoDB.Driver;

namespace DecisionReplay.Infrastructure.Persistence.Mongo;

public sealed class MongoContext
{
    public IMongoDatabase Database { get; }

    public MongoContext(MongoSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        Database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        Database.GetCollection<T>(name);
}
