using DecisionReplay.Domain.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace DecisionReplay.Application.Interfaces;

/// <summary>
/// Interface: Intent Parser
/// 
/// SOLID Principles:
/// - Interface Segregation: Focused interface for one responsibility
/// - Dependency Inversion: High-level modules depend on this abstraction
/// 
/// Purpose:
/// Analyzes natural language input to extract:
/// - Decision intent and goal
/// - Domain type (software, construction, logistics, etc.)
/// - Key constraints and resources
/// - Implicit assumptions
/// 
/// This enables domain-agnostic decision processing without hard-coded fields.
/// </summary>
public interface IIntentParser
{
    /// <summary>
    /// Parses natural language input and extracts decision context
    /// </summary>
    Task<DecisionContext> ParseInputAsync(string naturalLanguageInput, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a domain-specific schema based on the parsed context
    /// </summary>
    Task<DecisionSchema> GenerateSchemaAsync(DecisionContext context, CancellationToken cancellationToken = default);
}
