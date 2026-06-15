using MongoDB.Driver;

namespace DecisionReplay.Infrastructure.Persistence.Mongo;

public sealed class MongoContext
{
    public IMongoDatabase Database { get; }

    public MongoContext(IMongoClient client, MongoSettings settings)
    {
        Database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        Database.GetCollection<T>(name);
}
