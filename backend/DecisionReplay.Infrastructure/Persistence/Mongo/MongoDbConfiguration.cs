using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace DecisionReplay.Infrastructure.Persistence.Mongo;

/// <summary>
/// MongoDB BSON serialization configuration
/// Simplified: Uses BSON attributes on entities instead of complex class maps
/// </summary>
public static class MongoDbConfiguration
{
    private static bool _configured = false;

    public static void Configure()
    {
        if (_configured) return;

        // Register GUID serializer with standard representation
        try
        {
            BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
        }
        catch (BsonSerializationException)
        {
            // Already registered, ignore
        }

        // Register nullable GUID serializer
        try
        {
            BsonSerializer.RegisterSerializer(typeof(Guid?), new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));
        }
        catch (BsonSerializationException)
        {
            // Already registered, ignore
        }

        // Register ObjectSerializer to handle Dictionary<string, object> with specific types
        try
        {
            var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || (type.FullName != null && type.FullName.StartsWith("System.Guid")));
            BsonSerializer.RegisterSerializer(typeof(object), objectSerializer);
        }
        catch (BsonSerializationException)
        {
            // Already registered, ignore
        }

        // Register conventions for camelCase and enum handling
        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String)
        };
        ConventionRegistry.Register("DecisionReplayConventions", conventionPack, _ => true);

        _configured = true;
    }
}
