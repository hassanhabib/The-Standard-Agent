// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Standard.Agents.Models.Clients.Agents.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

public partial class StandardAgentTests
{
    // Found in the 2026-09-04 principal review (F-01): cost is priced off the token count times
    // the rate, and the rate defaulted to zero. A cost bound with no rate computed zero dollars
    // forever and never tripped — a guardrail the host believed was armed and was not. The
    // framework cannot know a model's price, so it cannot fill the rate in; what it can do is
    // refuse the contradiction of a dollar bound on a model declared free.
    [Theory]
    [InlineData(0)]
    [InlineData(-0.002)]
    public void ShouldThrowInvalidAgentBudgetExceptionOnBudgetIfCostBoundHasNoRate(
        decimal invalidCostPerThousandTokens)
    {
        // given
        var expectedInvalidAgentBudgetException =
            new InvalidAgentBudgetException(
                message:
                    "Invalid agent budget: a cost bound (maxCostUsd) needs a positive "
                        + "costPerThousandTokens. Cost is the token count times that rate, so at "
                        + "zero the bound can never trip. Pass your model's rate, or bound by "
                        + "maxTokens instead.");

        // when
        Action budgetAction = () =>
            new StandardAgent().Budget(
                maxCostUsd: 0.25m,
                costPerThousandTokens: invalidCostPerThousandTokens);

        InvalidAgentBudgetException actualInvalidAgentBudgetException =
            Assert.Throws<InvalidAgentBudgetException>(budgetAction);

        // then
        actualInvalidAgentBudgetException.Should()
            .BeEquivalentTo(expectedInvalidAgentBudgetException);
    }
}
