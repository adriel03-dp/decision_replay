namespace DecisionReplay.Domain.Enums;

public enum DecisionEventType
{
    DecisionCreated,        // User submits planning scenario
    ScopeAnalyzed,          // AI analyzes scope feasibility
    TimelineEvaluated,      // AI checks timeline realism
    ResourcesAssessed,      // AI reviews resource allocation
    ConstraintsIdentified,  // AI identifies conflicts/bottlenecks
    FeasibilityGenerated,   // Overall feasibility score computed
    UserRefinement,         // User adjusts plan based on insights
    DecisionFinalized       // User commits to final plan
}
