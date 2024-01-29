using circuitbreaker.example.services.aggregates;
using circuitbreaker.library.aggregates;
using circuitbreaker.library.implementations;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace circuitbreaker.example.services
{
    internal class WaterTreatmentPlantService : CircuitBreakerBase
    {
        static HttpClient HTTP_CLIENT = new HttpClient();

        public WaterTreatmentPlantService(ILogger<WaterTreatmentPlantService> logger) : 
            base(TimeSpan.FromMinutes(1), 3, 
                new CircuitBreakerThresold(3, TimeSpan.FromMinutes(5)), 
                "WaterTreatmentPlan", logger)
        {
        }

        public async Task<List<OriginByDate>?> GetOriginsByDates()
        {
            var result = new List<OriginByDate>();
            var response = await ExecuteInCircuitBreaker(() =>
            {
                return HTTP_CLIENT.GetAsync("https://www.zaragoza.es/sede/servicio/potabilizadora/procedencia.json");
            });

            if (response.IsSuccess)
            {
                var stringData = await response.ResponseMessage!.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<object[][]>(stringData);

                Dictionary<int, string> headers = new();

                for (int i = 0;i < data[0].Length; i++) 
                {
                    var header = data[0][i];
                    var stringName = ((JsonElement)header).GetString();

                    if (stringName != "X")
                    {
                        headers.Add(i, stringName);
                    }
                }

                data.Skip(1).ToList().ForEach(it =>
                {
                    if (DateTime.TryParseExact(((JsonElement)it[0]).GetString(), "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var dt))
                    {
                        result.Add(new OriginByDate
                        {
                            Date = dt,
                            Distribution = headers.ToDictionary(head => head.Value, head => ((JsonElement)it[head.Key]).GetInt16())
                        });
                    }
                });
            }

            return result;
        }
    }
}
