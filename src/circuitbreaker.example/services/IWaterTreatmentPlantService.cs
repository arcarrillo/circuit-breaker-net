using circuitbreaker.example.services.aggregates;
using circuitbreaker.library.abstractions;

namespace circuitbreaker.example.services;
internal interface IWaterTreatmentPlantService : ICircuitBreaker
{
    Task<List<OriginByDate>?> GetOriginsByDates();
}