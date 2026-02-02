namespace DecisionReplay.API.DTOs;

/// <summary>
/// Standardized response for decision query/Q&A
/// </summary>
public class QueryDecisionResponse
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public QueryDecisionResponse(string question, string answer)
    {
        Question = question;
        Answer = answer;
        Timestamp = DateTime.UtcNow;
    }
}
