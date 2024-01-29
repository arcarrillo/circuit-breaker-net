using circuitbreaker.library.aggregates;
using circuitbreaker.library.implementations;
using FluentAssertions;
using Moq;

namespace circuitbreaker.library.tests;

public class CircuitBreakerStatusControllerTests 
{

    [Fact]
    public void When_StatusCloseAndReceivesOkResponse_Should_RemainCloseAndCallbackShouldNotBeInvoked(){
        var actionMock = new Mock<Action<CircuitBreakerStatus, CircuitBreakerStatus>>();
        var controller = new CircuitBreakerStatusController(
            openToHalfOpenThresold: TimeSpan.FromSeconds(1),
            halfOpenToCloseThresold: 2,
            closeToOpenThresold: new CircuitBreakerThresold(1, TimeSpan.FromSeconds(10)),
            statusChangedCallback: actionMock.Object
        );

        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });

        controller.Status.Should().Be(CircuitBreakerStatus.Close);
        actionMock.Verify(x => x.Invoke(
            It.IsAny<CircuitBreakerStatus>(),
            It.IsAny<CircuitBreakerStatus>()
            ),
            Times.Never
        );
    }

    [Fact]
    public void When_StatusCloseAndReceivesCallerErrorResponse_Should_RemainCloseAndCallbackShouldNotBeInvoked()
    {
        var actionMock = new Mock<Action<CircuitBreakerStatus, CircuitBreakerStatus>>();
        var controller = new CircuitBreakerStatusController(
            openToHalfOpenThresold: TimeSpan.FromSeconds(1),
            halfOpenToCloseThresold: 2,
            closeToOpenThresold: new CircuitBreakerThresold(1, TimeSpan.FromSeconds(10)),
            statusChangedCallback: actionMock.Object
        );

        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.NotFound });

        controller.Status.Should().Be(CircuitBreakerStatus.Close);
        actionMock.Verify(x => x.Invoke(
            It.IsAny<CircuitBreakerStatus>(),
            It.IsAny<CircuitBreakerStatus>()
            ),
            Times.Never
        );
    }

    [Fact]
    public void When_StatusCloseAndReceivesTooManyRequestsResponse_Should_MoveToOpenAndCallbackShouldBeInvoked()
    {
        var actionMock = new Mock<Action<CircuitBreakerStatus, CircuitBreakerStatus>>();
        var controller = new CircuitBreakerStatusController(
            openToHalfOpenThresold: TimeSpan.FromSeconds(1),
            halfOpenToCloseThresold: 2,
            closeToOpenThresold: new CircuitBreakerThresold(2, TimeSpan.FromSeconds(10)),
            statusChangedCallback: actionMock.Object
        );

        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.TooManyRequests });

        controller.Status.Should().Be(CircuitBreakerStatus.Open);
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Close),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open)
            ),
            Times.Once
        );
    }

    [Fact]
    public void When_StatusCloseAndReceivesServerErrorResponseUnderThresold_Should_RemainCloseAndCallbackShouldNotBeInvoked()
    {
        var actionMock = new Mock<Action<CircuitBreakerStatus, CircuitBreakerStatus>>();
        var controller = new CircuitBreakerStatusController(
            openToHalfOpenThresold: TimeSpan.FromSeconds(1),
            halfOpenToCloseThresold: 2,
            closeToOpenThresold: new CircuitBreakerThresold(2, TimeSpan.FromSeconds(10)),
            statusChangedCallback: actionMock.Object
        );

        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });

        controller.Status.Should().Be(CircuitBreakerStatus.Close);
        actionMock.Verify(x => x.Invoke(
            It.IsAny<CircuitBreakerStatus>(),
            It.IsAny<CircuitBreakerStatus>()
            ),
            Times.Never
        );
    }

    [Fact]
    public void When_StatusCloseAndReceivesServerErrorResponseAboveThresold_Should_MoveToOpenAndCallbackShouldBeInvoked()
    {
        var actionMock = new Mock<Action<CircuitBreakerStatus, CircuitBreakerStatus>>();
        var controller = new CircuitBreakerStatusController(
            openToHalfOpenThresold: TimeSpan.FromSeconds(1),
            halfOpenToCloseThresold: 2,
            closeToOpenThresold: new CircuitBreakerThresold(2, TimeSpan.FromSeconds(10)),
            statusChangedCallback: actionMock.Object
        );

        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });

        controller.Status.Should().Be(CircuitBreakerStatus.Open);
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Close),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open)
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task When_StatusOpenAndPassesThresold_Should_MoveToHalfOpenAndCallbackShouldBeInvoked()
    {
        var actionMock = new Mock<Action<CircuitBreakerStatus, CircuitBreakerStatus>>();
        var controller = new CircuitBreakerStatusController(
            openToHalfOpenThresold: TimeSpan.FromMilliseconds(500),
            halfOpenToCloseThresold: 2,
            closeToOpenThresold: new CircuitBreakerThresold(2, TimeSpan.FromSeconds(10)),
            statusChangedCallback: actionMock.Object
        );

        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });

        await Task.Delay(TimeSpan.FromMilliseconds(501));

        controller.CanBeInvoked();

        controller.Status.Should().Be(CircuitBreakerStatus.HalfOpen);
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Close),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open)
            ),
            Times.Once
        );
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.HalfOpen)
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task When_StatusHalfOpenAndReceivesMultipleOKResponsesUnderThresold_Should_MaintainOnHalfOpen()
    {
        var actionMock = new Mock<Action<CircuitBreakerStatus, CircuitBreakerStatus>>();
        var controller = new CircuitBreakerStatusController(
            openToHalfOpenThresold: TimeSpan.FromMilliseconds(500),
            halfOpenToCloseThresold: 2,
            closeToOpenThresold: new CircuitBreakerThresold(2, TimeSpan.FromSeconds(10)),
            statusChangedCallback: actionMock.Object
        );

        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });

        await Task.Delay(TimeSpan.FromMilliseconds(501));

        controller.CanBeInvoked();
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });

        controller.Status.Should().Be(CircuitBreakerStatus.HalfOpen);
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Close),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open)
            ),
            Times.Once
        );
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.HalfOpen)
            ),
            Times.Once
        );
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.HalfOpen),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Close)
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task When_StatusHalfOpenAndReceivesMultipleOKResponsesAboveThresold_Should_MoveToClose()
    {
        var actionMock = new Mock<Action<CircuitBreakerStatus, CircuitBreakerStatus>>();
        var controller = new CircuitBreakerStatusController(
            openToHalfOpenThresold: TimeSpan.FromMilliseconds(500),
            halfOpenToCloseThresold: 2,
            closeToOpenThresold: new CircuitBreakerThresold(2, TimeSpan.FromSeconds(10)),
            statusChangedCallback: actionMock.Object
        );

        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });

        await Task.Delay(TimeSpan.FromMilliseconds(501));

        controller.CanBeInvoked();
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });

        controller.Status.Should().Be(CircuitBreakerStatus.Close);
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Close),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open)
            ),
            Times.Once
        );
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.HalfOpen)
            ),
            Times.Once
        );
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.HalfOpen),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Close)
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task When_StatusHalfOpenAndReceivesErrorThresold_Should_MoveToOpen()
    {
        var actionMock = new Mock<Action<CircuitBreakerStatus, CircuitBreakerStatus>>();
        var controller = new CircuitBreakerStatusController(
            openToHalfOpenThresold: TimeSpan.FromMilliseconds(500),
            halfOpenToCloseThresold: 2,
            closeToOpenThresold: new CircuitBreakerThresold(2, TimeSpan.FromSeconds(10)),
            statusChangedCallback: actionMock.Object
        );

        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });

        await Task.Delay(TimeSpan.FromMilliseconds(501));

        controller.CanBeInvoked();
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });

        controller.Status.Should().Be(CircuitBreakerStatus.Open);
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Close),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open)
            ),
            Times.Once
        );
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.HalfOpen)
            ),
            Times.Once
        );
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.HalfOpen),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open)
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task When_StatusHalfOpenCheckResetCounter_Should_RemainInHalfOpen()
    {
        var actionMock = new Mock<Action<CircuitBreakerStatus, CircuitBreakerStatus>>();
        var controller = new CircuitBreakerStatusController(
            openToHalfOpenThresold: TimeSpan.FromMilliseconds(500),
            halfOpenToCloseThresold: 2,
            closeToOpenThresold: new CircuitBreakerThresold(2, TimeSpan.FromSeconds(10)),
            statusChangedCallback: actionMock.Object
        );

        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });

        await Task.Delay(TimeSpan.FromMilliseconds(501));

        controller.CanBeInvoked();
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });

        await Task.Delay(TimeSpan.FromMilliseconds(501));

        controller.CanBeInvoked();
        controller.CheckResponse(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });

        controller.Status.Should().Be(CircuitBreakerStatus.HalfOpen);
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Close),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open)
            ),
            Times.Once
        );
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.HalfOpen)
            ),
            Times.AtMost(2)
        );
        actionMock.Verify(x => x.Invoke(
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.HalfOpen),
            It.Is<CircuitBreakerStatus>(s => s == CircuitBreakerStatus.Open)
            ),
            Times.Once
        );
    }
}