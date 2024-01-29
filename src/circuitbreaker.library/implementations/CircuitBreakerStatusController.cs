using circuitbreaker.library.aggregates;
using circuitbreaker.library.helpers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace circuitbreaker.library.implementations;

internal class CircuitBreakerStatusController(
    TimeSpan openToHalfOpenThresold, 
    int halfOpenToCloseThresold, 
    CircuitBreakerThresold closeToOpenThresold,
    Action<CircuitBreakerStatus, CircuitBreakerStatus>? statusChangedCallback = null)
{

    public HttpStatusCode? LastRequestStatusCode { get; private set; }
    public CircuitBreakerStatus Status { get; private set; } = CircuitBreakerStatus.Close;
    public DateTimeOffset? SetTime { get; private set; }

    public FixedSizeQueue<CircuitBreakerStatusControllerRequest> _lastRequestsQueue =
        new FixedSizeQueue<CircuitBreakerStatusControllerRequest>(closeToOpenThresold.NumberOfFailures);

    int _halfOpenSuccessfulCalls = 0;

    private void SetCurrentStatus(CircuitBreakerStatus status)
    {
        statusChangedCallback?.Invoke(Status, status);
        Status = status;
        SetTime = DateTimeOffset.Now;
    }

    private void MoveToHalfOpen()
    {
        SetCurrentStatus(CircuitBreakerStatus.HalfOpen);
        _halfOpenSuccessfulCalls = 0;
    }

    public bool CanBeInvoked()
    {
        var result = false;

        if (Status != CircuitBreakerStatus.Open)
            result = true;
        else if (SetTime.HasValue && SetTime.Value.Add(openToHalfOpenThresold) < DateTimeOffset.Now)
        {
            MoveToHalfOpen();
            result = true;
        }

        return result;
    }

    private void AddSuccessfulResponse()
    {
        if (Status == CircuitBreakerStatus.HalfOpen)
            _halfOpenSuccessfulCalls++;
        else
            _lastRequestsQueue.Enqueue(CircuitBreakerStatusControllerRequest.CreateSuccessful());

        if (_halfOpenSuccessfulCalls >= halfOpenToCloseThresold)
            SetCurrentStatus(CircuitBreakerStatus.Close);
    }

    private bool ShouldMoveFromCloseToOpen() => _lastRequestsQueue.Full &&
        _lastRequestsQueue.All(it => !it.Status && it.When.Add(closeToOpenThresold.TimeThresold) >= DateTimeOffset.Now);

    public void AddErrorResponse()
    {
        if (Status == CircuitBreakerStatus.HalfOpen)
            SetCurrentStatus(CircuitBreakerStatus.Open);
        else
        {
            _lastRequestsQueue.Enqueue(CircuitBreakerStatusControllerRequest.CreateError());
            if (ShouldMoveFromCloseToOpen())
                SetCurrentStatus(CircuitBreakerStatus.Open);
        }
    }

    private void CheckErrorResponse(HttpResponseMessage responseMessage)
    {
        if (responseMessage.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            SetCurrentStatus(CircuitBreakerStatus.Open);
        else if ((int)responseMessage.StatusCode >= 500)
            AddErrorResponse();
    }

    public void CheckResponse(HttpResponseMessage responseMessage)
    {
        LastRequestStatusCode = responseMessage.StatusCode;
        if (responseMessage.IsSuccessStatusCode)
            AddSuccessfulResponse();
        else
            CheckErrorResponse(responseMessage);
    }

    public void Reset() => SetCurrentStatus(CircuitBreakerStatus.Close);

    internal class CircuitBreakerStatusControllerRequest(DateTimeOffset when, bool status)
    {
        public DateTimeOffset When => when;
        public bool Status => status;

        public static CircuitBreakerStatusControllerRequest CreateSuccessful() => new CircuitBreakerStatusControllerRequest(DateTimeOffset.Now, true);
        public static CircuitBreakerStatusControllerRequest CreateError() => new CircuitBreakerStatusControllerRequest(DateTimeOffset.Now, false);
    }
}
