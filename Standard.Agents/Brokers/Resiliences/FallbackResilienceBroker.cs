// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

#if !NET9_0_OR_GREATER
// System.Threading.Lock arrived in .NET 9. On the net8.0 target a plain object under the
// same lock statements is the identical semantic; the alias keeps one body for both.
using Lock = System.Object;
#endif

using Standard.Agents.Models.Brokers.Generators.V1;

namespace Standard.Agents.Brokers.Resiliences;

// Degradation before failure (SPEC.md §4.10). After enough consecutive failures the primary is
// judged unhealthy and the circuit opens: calls stop going to something that is not answering
// and go to the alternative instead. A degraded answer is worth more than no answer.
//
// It refuses to pretend. With no alternative configured this broker fails rather than returning
// an empty result, because a caller cannot tell fabricated silence from a real one.
//
// The circuit closes again after a cool-down, so a provider that recovers is used again without
// anyone restarting anything.
public sealed class FallbackResilienceBroker : IResilienceBroker
{
    private readonly IResilienceBroker primary;
    private readonly Func<ValueTask<string>>? alternative;
    private readonly int failuresBeforeOpen;
    private readonly TimeSpan coolDown;
    private readonly Func<DateTimeOffset> now;

    private readonly Lock circuitLock = new();
    private int consecutiveFailures;
    private DateTimeOffset openedOn;

    public FallbackResilienceBroker(
        IResilienceBroker primary,
        Func<ValueTask<string>>? alternative = null,
        int failuresBeforeOpen = 3,
        TimeSpan? coolDown = null,
        Func<DateTimeOffset>? now = null)
    {
        this.primary = primary;
        this.alternative = alternative;
        this.failuresBeforeOpen = failuresBeforeOpen;
        this.coolDown = coolDown ?? TimeSpan.FromSeconds(30);
        this.now = now ?? (() => DateTimeOffset.UtcNow);
    }

    public bool IsOpen
    {
        get
        {
            lock (this.circuitLock)
            {
                return IsOpenNow();
            }
        }
    }

    public async ValueTask<T> ExecuteAsync<T>(Func<ValueTask<T>> operation)
    {
        if (IsOpen)
        {
            return await FallBackAsync<T>();
        }

        try
        {
            T result = await this.primary.ExecuteAsync(operation);

            lock (this.circuitLock)
            {
                this.consecutiveFailures = 0;
            }

            return result;
        }
        catch (Exception)
        {
            bool nowOpen;

            lock (this.circuitLock)
            {
                this.consecutiveFailures++;

                nowOpen = this.consecutiveFailures >= this.failuresBeforeOpen;

                if (nowOpen)
                {
                    this.openedOn = this.now();
                }
            }

            if (nowOpen && this.alternative is not null)
            {
                return await FallBackAsync<T>();
            }

            throw;
        }
    }

    private bool IsOpenNow()
    {
        if (this.consecutiveFailures < this.failuresBeforeOpen)
        {
            return false;
        }

        // Cool-down elapsed: close the circuit and let the primary prove itself again.
        if (this.now() - this.openedOn >= this.coolDown)
        {
            this.consecutiveFailures = 0;

            return false;
        }

        return true;
    }

    private async ValueTask<T> FallBackAsync<T>()
    {
        if (this.alternative is null)
        {
            throw new InvalidOperationException(
                "The provider is unhealthy and no alternative is configured. Failing rather "
                    + "than returning a fabricated result (SPEC.md §4.10).");
        }

        string answer = await this.alternative();

        return Degrade<T>(answer);
    }

    // The alternative is text — what a host can write without knowing which protocol will ask
    // for it — so each protocol's result is shaped from that text explicitly: the text seam
    // takes it as the reply, the native seam takes it as a final answer with no tool calls. A
    // bare cast to T held on the first and threw on the second, at the exact moment the control
    // was meant to work (principal review 2026-09-04, F-07). A T this broker has not been taught
    // is named rather than guessed at.
    private static T Degrade<T>(string answer)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)answer;
        }

        if (typeof(T) == typeof(GenerationResult))
        {
            return (T)(object)new GenerationResult { Content = answer };
        }

        throw new InvalidOperationException(
            $"No degraded {typeof(T).Name} can be shaped from a text fallback. Supply a "
                + "resilience broker that understands this result type (UseResilience).");
    }
}
