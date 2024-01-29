namespace circuitbreaker.example.services.aggregates
{
    public class OriginByDate
    {
        public DateTime Date { get; set; }
        public Dictionary<string, short> Distribution { get; set; }
    }
}
