using circuitbreaker.library.implementations;
using FluentAssertions;
using System.Net;

namespace circuitbreaker.library.tests.implementations
{
    public class CircuitBreakerBaseTests
    {


        [Fact]
        public async Task When_ExecuteInCircuitBreakerWithSucess_Should_BeClose()
        {
            var service = new CircuitBreakerImplementation();
            var result = await service.Simulate(HttpStatusCode.OK);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            service.Status.Should().Be(aggregates.CircuitBreakerStatus.Close);
        }

        [Fact]
        public async Task When_ExecuteInCircuitBreakerWithSucessAndError_Should_BeClose()
        {
            var service = new CircuitBreakerImplementation();
            await service.Simulate(HttpStatusCode.InternalServerError);
            var result = await service.Simulate(HttpStatusCode.OK);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            service.Status.Should().Be(aggregates.CircuitBreakerStatus.Close);
        }

        [Fact]
        public async Task When_ExecuteInCircuitBreakerWithTooManyRequests_Should_BeOpen()
        {
            var service = new CircuitBreakerImplementation();
            var result = await service.Simulate(HttpStatusCode.TooManyRequests);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            service.Status.Should().Be(aggregates.CircuitBreakerStatus.Open);
        }

        [Fact]
        public async Task When_ExecuteInCircuitBreakerWithTwoErrors_Should_BeOpen()
        {
            var service = new CircuitBreakerImplementation();
            await service.Simulate(HttpStatusCode.InternalServerError);
            var result = await service.Simulate(HttpStatusCode.InternalServerError);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            service.Status.Should().Be(aggregates.CircuitBreakerStatus.Open);
        }

        [Fact]
        public async Task When_ExecuteInCircuitBreakerWithTwoErrorsAndThenWaitWithSuccessCall_Should_BeHalfOpen()
        {
            var service = new CircuitBreakerImplementation();
            await service.Simulate(HttpStatusCode.InternalServerError);
            await service.Simulate(HttpStatusCode.InternalServerError);

            await Task.Delay(501);
            var result = await service.Simulate(HttpStatusCode.OK);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            service.Status.Should().Be(aggregates.CircuitBreakerStatus.HalfOpen);
        }

        [Fact]
        public async Task When_ExecuteInCircuitBreakerWithTwoErrorsAndThenWaitWithTwoSuccessCall_Should_BeClose()
        {
            var service = new CircuitBreakerImplementation();
            await service.Simulate(HttpStatusCode.InternalServerError);
            await service.Simulate(HttpStatusCode.InternalServerError);

            await Task.Delay(501);
            await service.Simulate(HttpStatusCode.OK);
            var result = await service.Simulate(HttpStatusCode.OK);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            service.Status.Should().Be(aggregates.CircuitBreakerStatus.Close);
        }

        [Fact]
        public async Task When_ExecuteInCircuitBreakerWithTwoErrorsAndThenWaitWithOkAndErrorCall_Should_BeOpen()
        {
            var service = new CircuitBreakerImplementation();
            await service.Simulate(HttpStatusCode.InternalServerError);
            await service.Simulate(HttpStatusCode.InternalServerError);

            await Task.Delay(501);
            await service.Simulate(HttpStatusCode.OK);

            service.Status.Should().Be(aggregates.CircuitBreakerStatus.HalfOpen);

            var result = await service.Simulate(HttpStatusCode.InternalServerError);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            service.Status.Should().Be(aggregates.CircuitBreakerStatus.Open);
        }

        [Fact]
        public async Task When_ExecuteInOpenThenCallReset_Should_BeClose()
        {
            var service = new CircuitBreakerImplementation();
            await service.Simulate(HttpStatusCode.InternalServerError);
            await service.Simulate(HttpStatusCode.InternalServerError);

            service.Status.Should().Be(aggregates.CircuitBreakerStatus.Open);
            service.Reset();
            service.Status.Should().Be(aggregates.CircuitBreakerStatus.Close);
        }

        public class CircuitBreakerImplementation : CircuitBreakerBase 
        {
            public CircuitBreakerImplementation()
                : base(
                    openToHalfOpenThresold: TimeSpan.FromMilliseconds(500),
                    halfOpenToCloseThresold: 2, 
                    closeToOpenThresold: new aggregates.CircuitBreakerThresold(2, TimeSpan.FromSeconds(1)),
                    serviceName: nameof(CircuitBreakerImplementation)
                )
            {
                
            }

            public async Task<HttpResponseMessage?> Simulate(HttpStatusCode responseCode)
            {
                var result = await ExecuteInCircuitBreaker(
                    () => Task.FromResult(new HttpResponseMessage { StatusCode = responseCode })
                );

                return result.ResponseMessage;
            }
        }
    }
}
