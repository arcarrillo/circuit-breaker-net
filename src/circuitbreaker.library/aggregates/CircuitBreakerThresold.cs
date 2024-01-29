using System;

namespace circuitbreaker.library.aggregates;

public class CircuitBreakerThresold(int numberOfFailures, TimeSpan timeThresold) {
    public int NumberOfFailures => numberOfFailures;
    public TimeSpan TimeThresold => timeThresold;
}