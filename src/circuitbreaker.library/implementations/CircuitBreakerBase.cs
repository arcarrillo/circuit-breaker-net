using System;
using System.Net.Http;
using circuitbreaker.library.aggregates;
using circuitbreaker.library.abstractions;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using Microsoft.Extensions.Logging;

namespace circuitbreaker.library.implementations;

public abstract class CircuitBreakerBase : ICircuitBreaker 
{
    Semaphore _semaphore = new Semaphore(1, 1);
    CircuitBreakerStatusController _currentStatusController;
    ILogger? _logger;

    public CircuitBreakerBase(TimeSpan openToHalfOpenThresold,
        int halfOpenToCloseThresold, CircuitBreakerThresold closeToOpenThresold, string serviceName, ILogger? logger = null)
    {
        OpenToHalfOpenThresold = openToHalfOpenThresold;
        HalfOpenToCloseThresold = halfOpenToCloseThresold;
        CloseToOpenThresold = closeToOpenThresold;
        ServiceName = serviceName;
        _logger = logger;
        _currentStatusController = new CircuitBreakerStatusController(openToHalfOpenThresold, halfOpenToCloseThresold, closeToOpenThresold, StatusChanged);
    }

    public HttpStatusCode? LastRequestStatusCode => _currentStatusController.LastRequestStatusCode;
    public CircuitBreakerStatus Status => _currentStatusController.Status;
    public TimeSpan OpenToHalfOpenThresold { get; private set; }
    public int HalfOpenToCloseThresold { get; private set;  }
    public string ServiceName { get; private set; }
    public CircuitBreakerThresold CloseToOpenThresold { get; private set; }

    protected async Task<CircuitBreakerResponse> ExecuteInCircuitBreaker(Func<Task<HttpResponseMessage>> functionToExecute)
    {
        _semaphore.WaitOne();

        if (!_currentStatusController.CanBeInvoked())
            return CircuitBreakerResponse.Opened();

        HttpResponseMessage response;
        try
        {
            response = await functionToExecute();
            _currentStatusController.CheckResponse(response);
        } 
        catch (Exception excep)
        {
            _currentStatusController.AddErrorResponse();
            throw excep;
        }
        finally
        {
            _semaphore.Release();
        }

        return new CircuitBreakerResponse(response, _currentStatusController.Status);
    }

    public void Reset() => _currentStatusController.Reset();

    private void StatusChanged(CircuitBreakerStatus fromStatus, CircuitBreakerStatus toStatus) {
        _logger?.Log(
            toStatus != CircuitBreakerStatus.Open ? LogLevel.Information : LogLevel.Critical,
            $"[Circuit Breaker] {ServiceName} service moved from {fromStatus} to {toStatus} status."
        );
    }

}