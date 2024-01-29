namespace circuitbreaker.library.aggregates;

public enum CircuitBreakerStatus {
    Close = 0,
    Open,
    HalfOpen
}