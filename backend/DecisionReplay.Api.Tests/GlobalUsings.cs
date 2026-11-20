global using Xunit;
global using System.Net;
global using System.Net.Http.Json;

// The API's dotenv configuration is process-wide; isolate host fixtures from each other.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
