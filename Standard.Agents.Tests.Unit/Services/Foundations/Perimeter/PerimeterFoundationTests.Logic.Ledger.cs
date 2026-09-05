// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Brokers.Effects;
using Standard.Agents.Models.Orchestrations.Effects;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Perimeter;

// The ledger foundation over typed records (principal review 2026-09-04, F-08): a claim is
// written in flight under a lease, an outcome completes it, a thrown tool fails it, only an
// in-flight claim is ever released, and compensation is recorded before and after the reversal.
public partial class PerimeterFoundationTests
{
    private static readonly TimeSpan lease = TimeSpan.FromMinutes(5);

    private static DateTimeOffset CreateRandomDateTimeOffset() =>
        new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero).AddMinutes(Random.Shared.Next(1, 1000));

    private static EffectRecord CreateInFlightRecord(AgentEffect effect, DateTimeOffset claimedOn) =>
        new()
        {
            IdempotencyKey = effect.IdempotencyKey,
            ToolName = effect.ToolName,
            State = EffectState.InFlight,
            Owner = effect.RunId,
            ClaimedOn = claimedOn,
            LeaseUntil = claimedOn + lease
        };

    [Fact]
    public async Task ShouldClaimEffectAsync()
    {
        // given — a first-time act
        AgentEffect randomEffect = CreateRandomEffect();
        DateTimeOffset randomDateTimeOffset = CreateRandomDateTimeOffset();
        EffectRecord expectedClaim = CreateInFlightRecord(randomEffect, randomDateTimeOffset);

        this.timeBrokerMock.Setup(broker =>
            broker.GetCurrentDateTimeOffset())
                .Returns(randomDateTimeOffset);

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.InsertClaimAsync(It.IsAny<EffectRecord>()))
                .ReturnsAsync(true);

        // when
        EffectRecord? actualPrior = await this.effectLedgerService.ClaimEffectAsync(randomEffect);

        // then — null means proceed; the claim written is in flight, owned, stamped and leased
        actualPrior.Should().BeNull();

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.InsertClaimAsync(It.Is<EffectRecord>(claim =>
                claim.IdempotencyKey == expectedClaim.IdempotencyKey
                    && claim.ToolName == expectedClaim.ToolName
                    && claim.State == EffectState.InFlight
                    && claim.Owner == expectedClaim.Owner
                    && claim.ClaimedOn == expectedClaim.ClaimedOn
                    && claim.LeaseUntil == expectedClaim.LeaseUntil)),
                        Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldRetrievePriorRecordOnClaimEffectIfActWasSeenBeforeAsync()
    {
        // given — the key already has a record
        AgentEffect randomEffect = CreateRandomEffect();
        DateTimeOffset randomDateTimeOffset = CreateRandomDateTimeOffset();

        EffectRecord priorRecord = CreateInFlightRecord(randomEffect, randomDateTimeOffset) with
        {
            State = EffectState.Completed,
            Outcome = CreateRandomString()
        };

        EffectRecord expectedRecord = priorRecord;

        this.timeBrokerMock.Setup(broker =>
            broker.GetCurrentDateTimeOffset())
                .Returns(randomDateTimeOffset);

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.InsertClaimAsync(It.IsAny<EffectRecord>()))
                .ReturnsAsync(false);

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey))
                .ReturnsAsync(priorRecord);

        // when
        EffectRecord? actualRecord = await this.effectLedgerService.ClaimEffectAsync(randomEffect);

        // then — the prior record comes back whole; what it means is the tier above's judgment
        actualRecord.Should().BeEquivalentTo(expectedRecord);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.InsertClaimAsync(It.IsAny<EffectRecord>()),
                Times.Once);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey),
                Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldRecordOutcomeAsync()
    {
        // given — an in-flight claim, completed with what the act produced
        AgentEffect randomEffect = CreateRandomEffect();
        string randomOutcome = CreateRandomString();
        DateTimeOffset claimedOn = CreateRandomDateTimeOffset();
        DateTimeOffset recordedOn = claimedOn.AddSeconds(3);
        EffectRecord inFlightRecord = CreateInFlightRecord(randomEffect, claimedOn);

        EffectRecord expectedRecord = inFlightRecord with
        {
            State = EffectState.Completed,
            Outcome = randomOutcome,
            Detail = null,
            RecordedOn = recordedOn
        };

        this.timeBrokerMock.Setup(broker =>
            broker.GetCurrentDateTimeOffset())
                .Returns(recordedOn);

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey))
                .ReturnsAsync(inFlightRecord);

        // when
        await this.effectLedgerService.RecordOutcomeAsync(randomEffect, randomOutcome);

        // then
        this.effectLedgerBrokerMock.Verify(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey),
                Times.Once);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.UpdateRecordAsync(It.Is<EffectRecord>(record =>
                record.Equals(expectedRecord))),
                    Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldRecordFailureAsync()
    {
        // given — the tool threw; the act's fate is unknown and the record says so
        AgentEffect randomEffect = CreateRandomEffect();
        string randomDetail = CreateRandomString();
        DateTimeOffset claimedOn = CreateRandomDateTimeOffset();
        DateTimeOffset recordedOn = claimedOn.AddSeconds(3);
        EffectRecord inFlightRecord = CreateInFlightRecord(randomEffect, claimedOn);

        EffectRecord expectedRecord = inFlightRecord with
        {
            State = EffectState.Failed,
            Outcome = null,
            Detail = randomDetail,
            RecordedOn = recordedOn
        };

        this.timeBrokerMock.Setup(broker =>
            broker.GetCurrentDateTimeOffset())
                .Returns(recordedOn);

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey))
                .ReturnsAsync(inFlightRecord);

        // when
        await this.effectLedgerService.RecordFailureAsync(randomEffect, randomDetail);

        // then
        this.effectLedgerBrokerMock.Verify(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey),
                Times.Once);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.UpdateRecordAsync(It.Is<EffectRecord>(record =>
                record.Equals(expectedRecord))),
                    Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReleaseClaimIfRecordIsInFlightAsync()
    {
        // given — a held act: claimed, never performed
        AgentEffect randomEffect = CreateRandomEffect();

        EffectRecord inFlightRecord =
            CreateInFlightRecord(randomEffect, CreateRandomDateTimeOffset());

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey))
                .ReturnsAsync(inFlightRecord);

        // when
        await this.effectLedgerService.ReleaseClaimAsync(randomEffect);

        // then
        this.effectLedgerBrokerMock.Verify(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey),
                Times.Once);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.DeleteRecordAsync(randomEffect.IdempotencyKey),
                Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // A record with an outcome names an act that happened; releasing it would turn run-once
    // back into run-again.
    [Fact]
    public async Task ShouldNotReleaseClaimIfRecordIsNotInFlightAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();

        EffectRecord completedRecord =
            CreateInFlightRecord(randomEffect, CreateRandomDateTimeOffset()) with
            {
                State = EffectState.Completed,
                Outcome = CreateRandomString()
            };

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey))
                .ReturnsAsync(completedRecord);

        // when
        await this.effectLedgerService.ReleaseClaimAsync(randomEffect);

        // then
        this.effectLedgerBrokerMock.Verify(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey),
                Times.Once);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.DeleteRecordAsync(It.IsAny<string>()),
                Times.Never);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldRecordCompensationIntentAsync()
    {
        // given — a completed act about to be undone; the intent goes on the record first
        AgentEffect randomEffect = CreateRandomEffect();
        DateTimeOffset claimedOn = CreateRandomDateTimeOffset();
        DateTimeOffset recordedOn = claimedOn.AddSeconds(9);

        EffectRecord completedRecord = CreateInFlightRecord(randomEffect, claimedOn) with
        {
            State = EffectState.Completed,
            Outcome = CreateRandomString()
        };

        EffectRecord expectedRecord = completedRecord with
        {
            State = EffectState.CompensationPending,
            RecordedOn = recordedOn
        };

        this.timeBrokerMock.Setup(broker =>
            broker.GetCurrentDateTimeOffset())
                .Returns(recordedOn);

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey))
                .ReturnsAsync(completedRecord);

        // when
        await this.effectLedgerService.RecordCompensationIntentAsync(randomEffect.IdempotencyKey);

        // then
        this.effectLedgerBrokerMock.Verify(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey),
                Times.Once);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.UpdateRecordAsync(It.Is<EffectRecord>(record =>
                record.Equals(expectedRecord))),
                    Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldRecordCompensationAsync()
    {
        // given — the reversal happened, and how it happened is written against the same record
        AgentEffect randomEffect = CreateRandomEffect();
        string randomDetail = CreateRandomString();
        DateTimeOffset claimedOn = CreateRandomDateTimeOffset();
        DateTimeOffset recordedOn = claimedOn.AddSeconds(12);

        EffectRecord pendingRecord = CreateInFlightRecord(randomEffect, claimedOn) with
        {
            State = EffectState.CompensationPending,
            Outcome = CreateRandomString()
        };

        EffectRecord expectedRecord = pendingRecord with
        {
            State = EffectState.Compensated,
            Detail = randomDetail,
            RecordedOn = recordedOn
        };

        this.timeBrokerMock.Setup(broker =>
            broker.GetCurrentDateTimeOffset())
                .Returns(recordedOn);

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey))
                .ReturnsAsync(pendingRecord);

        // when
        await this.effectLedgerService.RecordCompensationAsync(
            randomEffect.IdempotencyKey, randomDetail);

        // then
        this.effectLedgerBrokerMock.Verify(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey),
                Times.Once);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.UpdateRecordAsync(It.Is<EffectRecord>(record =>
                record.Equals(expectedRecord))),
                    Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
