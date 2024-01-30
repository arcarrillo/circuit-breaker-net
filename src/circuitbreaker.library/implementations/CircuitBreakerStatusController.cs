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
    CircuitBreakerStatus _status = CircuitBreakerStatus.Close;

    public HttpStatusCode? LastRequestStatusCode { get; private set; }
    public CircuitBreakerStatus Status { get { CheckCurrentStatus(); return _status; } private set { _status = value; } }
    public DateTimeOffset? SetTime { get; private set; }

    public FixedSizeQueue<CircuitBreakerStatusControllerRequest> _lastFailedRequestsQueue =
        new FixedSizeQueue<CircuitBreakerStatusControllerRequest>(closeToOpenThresold.NumberOfFailures);

    int _halfOpenSuccessfulCalls = 0;

    #region Public surface

    public bool CanBeInvoked()
    {
        CheckCurrentStatus();
        return _status != CircuitBreakerStatus.Open;
    }
    public void AddErrorResponse()
    {
        if (_status == CircuitBreakerStatus.HalfOpen)
            SetCurrentStatus(CircuitBreakerStatus.Open);
        else
        {
            _lastFailedRequestsQueue.Enqueue(CircuitBreakerStatusControllerRequest.CreateError());
            if (ShouldMoveFromCloseToOpen())
                SetCurrentStatus(CircuitBreakerStatus.Open);
        }
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

    #endregion

    #region Private surface

    private void SetCurrentStatus(CircuitBreakerStatus status)
    {
        statusChangedCallback?.Invoke(_status, status);
        Status = status;
        SetTime = DateTimeOffset.Now;
    }

    private void MoveToHalfOpen()
    {
        SetCurrentStatus(CircuitBreakerStatus.HalfOpen);
        _halfOpenSuccessfulCalls = 0;
    }

    private void CheckCurrentStatus()
    {
        if (_status == CircuitBreakerStatus.Open &&
            SetTime.HasValue && SetTime.Value.Add(openToHalfOpenThresold) < DateTimeOffset.Now)
            MoveToHalfOpen();
    }

    private void AddSuccessfulResponse()
    {
        if (_status == CircuitBreakerStatus.HalfOpen)
            _halfOpenSuccessfulCalls++;

        if (_halfOpenSuccessfulCalls >= halfOpenToCloseThresold)
            SetCurrentStatus(CircuitBreakerStatus.Close);
    }

    private bool ShouldMoveFromCloseToOpen() => _lastFailedRequestsQueue.Full &&
        _lastFailedRequestsQueue.All(it => !it.Status && it.When.Add(closeToOpenThresold.TimeThresold) >= DateTimeOffset.Now);

    private void CheckErrorResponse(HttpResponseMessage responseMessage)
    {
        if (responseMessage.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            SetCurrentStatus(CircuitBreakerStatus.Open);
        else if ((int)responseMessage.StatusCode >= 500)
            AddErrorResponse();
    }

    #endregion

    internal class CircuitBreakerStatusControllerRequest(DateTimeOffset when, bool status)
    {
        public DateTimeOffset When => when;
        public bool Status => status;

        public static CircuitBreakerStatusControllerRequest CreateSuccessful() => new CircuitBreakerStatusControllerRequest(DateTimeOffset.Now, true);
        public static CircuitBreakerStatusControllerRequest CreateError() => new CircuitBreakerStatusControllerRequest(DateTimeOffset.Now, false);
    }
}
