using System;
using System.Net;
using circuitbreaker.library.aggregates;

namespace circuitbreaker.library.abstractions;

public interface ICircuitBreaker {

    HttpStatusCode? LastRequestStatusCode { get; }
    CircuitBreakerStatus Status { get; }

    TimeSpan OpenToHalfOpenThresold {get; }
    int HalfOpenToCloseThresold { get; }
    CircuitBreakerThresold CloseToOpenThresold { get; }

    void Reset();
}