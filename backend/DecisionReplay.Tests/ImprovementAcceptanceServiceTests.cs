using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Infrastructure.Persistence.Repositories;

namespace DecisionReplay.Tests;

public sealed class ImprovementAcceptanceServiceTests
{
    [Fact]
    public async Task AdoptionAppendsCheckedVersionAndKeepsTheOriginalUnchanged()
    {
        var repository = new InMemoryDecisionV2Repository();
        var data = WorkflowFixture.Project(); data.Fields["required_scope"] = "reporting";
        var original = await WorkflowFixture.Engine(repository).AnalyzeStructuredAndCreateAsync(data, "owner", originalInput: WorkflowFixture.Narrative);
        var oldPlan = original.GetCurrentVersion()!.Plan!;
        var result = await WorkflowFixture.Acceptance(repository).AcceptAsync(original.Id, "owner", 1);
        var current = result.Decision.GetCurrentVersion()!;
        Assert.Equal(2, current.Version);
        Assert.Equal("login, payments, reporting", result.Decision.Versions[0].StructuredData.Get("scope"));
        Assert.Equal(oldPlan.PlanId, result.Decision.Versions[0].Plan!.PlanId);
        Assert.NotEqual(oldPlan.PlanId, current.Plan!.PlanId);
        Assert.Equal(oldPlan.EndDate, current.Plan.EndDate);
        Assert.Contains("reporting", current.StructuredData.Get("scope")!);
        Assert.Equal(current.StructuredData.Get("scope"), current.Context!.Fields["scope"]);
        Assert.Empty(current.AiAnalyses);
        Assert.Equal(WorkflowFixture.Narrative, result.Decision.OriginalInput);
        Assert.True(result.Comparison.ScoreDelta > 0);
        Assert.Contains(result.Decision.AuditTrail, entry => entry.Action == AuditActionType.ImprovementAccepted && entry.Actor == "owner");
    }

    [Fact]
    public async Task AnotherOwnerCannotAdoptAProposal()
    {
        var repository = new InMemoryDecisionV2Repository();
        var decision = await WorkflowFixture.Engine(repository).AnalyzeStructuredAndCreateAsync(WorkflowFixture.Project(), "owner");
        await Assert.ThrowsAsync<KeyNotFoundException>(() => WorkflowFixture.Acceptance(repository).AcceptAsync(decision.Id, "someone-else", 1));
        Assert.Single((await repository.GetByIdAsync(decision.Id))!.Versions);
    }

    [Fact]
    public async Task RepeatedOrStaleAcceptanceCannotCreateDuplicateVersions()
    {
        var repository = new InMemoryDecisionV2Repository();
        var decision = await WorkflowFixture.Engine(repository).AnalyzeStructuredAndCreateAsync(WorkflowFixture.Project(), "owner");
        await WorkflowFixture.Acceptance(repository).AcceptAsync(decision.Id, "owner", 1);
        await Assert.ThrowsAsync<DecisionConflictException>(() => WorkflowFixture.Acceptance(repository).AcceptAsync(decision.Id, "owner", 1));
        Assert.Equal(2, (await repository.GetByIdAsync(decision.Id))!.Versions.Count);
    }

    [Fact]
    public async Task UnfundedDecisionCannotAdoptANonexistentUpgrade()
    {
        var repository = new InMemoryDecisionV2Repository(); var data = WorkflowFixture.Project(); data.Fields["budget"] = "1000";
        var decision = await WorkflowFixture.Engine(repository).AnalyzeStructuredAndCreateAsync(data, "owner");
        await Assert.ThrowsAsync<ArgumentException>(() => WorkflowFixture.Acceptance(repository).AcceptAsync(decision.Id, "owner", 1));
        Assert.Single((await repository.GetByIdAsync(decision.Id))!.Versions);
    }

    [Theory]
    [InlineData("monthly_cost_per_person", "1")]
    [InlineData("fixed_fields", "none")]
    [InlineData("technical_readiness", "high")]
    public async Task AlteredProposalInputsOrPolicyRequireRegeneration(string field, string value)
    {
        var repository = new InMemoryDecisionV2Repository();
        var decision = await WorkflowFixture.Engine(repository).AnalyzeStructuredAndCreateAsync(WorkflowFixture.Project(), "owner");
        decision.GetCurrentVersion()!.Improvement!.ProposedFields[field] = value;
        await repository.UpdateAsync(decision);
        await Assert.ThrowsAsync<DecisionConflictException>(() => WorkflowFixture.Acceptance(repository).AcceptAsync(decision.Id, "owner", 1));
        Assert.Single((await repository.GetByIdAsync(decision.Id))!.Versions);
    }

    [Fact]
    public async Task LegacyUncheckedProposalIsNotAdopted()
    {
        var repository = new InMemoryDecisionV2Repository();
        var decision = await WorkflowFixture.Engine(repository).AnalyzeStructuredAndCreateAsync(WorkflowFixture.Project(), "owner");
        decision.GetCurrentVersion()!.Improvement!.ConstraintCheck = null;
        await repository.UpdateAsync(decision);
        await Assert.ThrowsAsync<ArgumentException>(() => WorkflowFixture.Acceptance(repository).AcceptAsync(decision.Id, "owner", 1));
    }

    [Fact]
    public async Task ChangedScoringRulesRequireRegenerationBeforeAdoption()
    {
        var repository = new InMemoryDecisionV2Repository();
        var decision = await WorkflowFixture.Engine(repository).AnalyzeStructuredAndCreateAsync(WorkflowFixture.Project(), "owner");
        decision.GetCurrentVersion()!.Feasibility.RulesVersion = "2.0";
        await repository.UpdateAsync(decision);
        var conflict = await Assert.ThrowsAsync<DecisionConflictException>(() => WorkflowFixture.Acceptance(repository).AcceptAsync(decision.Id, "owner", 1));
        Assert.Contains("Regenerate", conflict.Message);
        Assert.Single((await repository.GetByIdAsync(decision.Id))!.Versions);
    }

    [Fact]
    public async Task CancellationDoesNotPersistAnAdoptedVersion()
    {
        var repository = new InMemoryDecisionV2Repository();
        var decision = await WorkflowFixture.Engine(repository).AnalyzeStructuredAndCreateAsync(WorkflowFixture.Project(), "owner");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WorkflowFixture.Acceptance(repository).AcceptAsync(decision.Id, "owner", 1, cancelled.Token));
        Assert.Single((await repository.GetByIdAsync(decision.Id))!.Versions);
    }
}
