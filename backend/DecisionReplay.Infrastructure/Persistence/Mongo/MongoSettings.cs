namespace DecisionReplay.Infrastructure.Persistence.Mongo;

public sealed class MongoSettings
{
    public string ConnectionString { get; init; } = null!;
    public string DatabaseName { get; init; } = null!;
}
