using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace BuildingBlocks.Common.Resilience;

public static class PollyPolicies
{
    /// <summary>
    /// Retry policy with exponential backoff (2s, 4s, 8s) for transient HTTP errors
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
            );
    }

    /// <summary>
    /// Circuit breaker policy: opens circuit after 5 consecutive failures for 30 seconds
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
