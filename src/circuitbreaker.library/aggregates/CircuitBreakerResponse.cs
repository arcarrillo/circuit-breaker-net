using System.Net.Http;

namespace circuitbreaker.library.aggregates;

public class CircuitBreakerResponse
{

    public CircuitBreakerResponse(HttpResponseMessage responseMessage, CircuitBreakerStatus status)
    {
        ResponseMessage = responseMessage;
        Status = status;
    }

    public CircuitBreakerResponse(CircuitBreakerStatus status)
    {
        Status = status;
    }

    public HttpResponseMessage? ResponseMessage { get; private set; }
    public bool IsSuccess => ResponseMessage?.IsSuccessStatusCode ?? false && Status != CircuitBreakerStatus.Open;
    public CircuitBreakerStatus Status { get; private set; }

    public static CircuitBreakerResponse Opened() => new CircuitBreakerResponse(CircuitBreakerStatus.Open);

}
