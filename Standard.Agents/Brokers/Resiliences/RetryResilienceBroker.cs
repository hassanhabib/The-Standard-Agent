// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using Xeptions;

namespace Standard.Agents.Brokers.Resiliences;

// Retry with exponential backoff and jitter (SPEC.md §4.10).
//
// What is retryable is decided by the error's CATEGORY, never its text. The framework already
// localizes every failure into a Xeption category, so that classification is available for free
// and does not have to be re-derived from a message that a provider may reword at any time.
//
// The rule is narrow on purpose: a dependency failure is the network having a bad moment, so it
// is worth another attempt. A validation failure is the request being wrong, so retrying it will
// fail identically and only spends the budget.
public sealed class RetryResilienceBroker : IResilienceBroker
{
    private readonly int maxAttempts;
    private readonly TimeSpan initialDelay;
    private readonly Func<TimeSpan, ValueTask> delay;

    public RetryResilienceBroker(
        int retries = 3,
        TimeSpan? initialDelay = null,
        Func<TimeSpan, ValueTask>? delay = null)
    {
        this.maxAttempts = retries < 0 ? 1 : retries + 1;
        this.initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(200);
        this.delay = delay ?? (async wait => await Task.Delay(wait));
    }

    public async ValueTask<T> ExecuteAsync<T>(Func<ValueTask<T>> operation)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception exception)
                when (attempt < this.maxAttempts && IsTransient(exception))
            {
                await this.delay(BackoffFor(attempt));
            }
        }
    }

    // Jitter matters when many agents share one provider: without it every agent that failed at
    // the same instant retries at the same instant, and the recovering provider is hit by the
    // same spike that knocked it over.
    private TimeSpan BackoffFor(int attempt)
    {
        double exponential = this.initialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        double jitter = Random.Shared.NextDouble() * exponential * 0.5;

        return TimeSpan.FromMilliseconds(exponential + jitter);
    }

    private static bool IsTransient(Exception exception) =>
        exception switch
        {
            // The framework's own categories: a dependency failed, which may not fail again.
            // A *DependencyValidation* failure is the request being wrong and is never retried.
            Xeption when IsDependencyFailure(exception) => true,
            HttpRequestException httpException => IsTransientStatus(httpException.StatusCode),
            TaskCanceledException => false,
            OperationCanceledException => false,
            _ => false
        };

    private static bool IsDependencyFailure(Exception exception)
    {
        string name = exception.GetType().Name;

        return name.EndsWith("DependencyException", StringComparison.Ordinal)
            && name.EndsWith("DependencyValidationException", StringComparison.Ordinal) is false;
    }

    private static bool IsTransientStatus(HttpStatusCode? status) =>
        status is null
            || status is HttpStatusCode.RequestTimeout
            || status is HttpStatusCode.TooManyRequests
            || (int)status >= 500;
}
