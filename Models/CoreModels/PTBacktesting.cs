namespace PT.Models.CoreModels
{
    public class BacktestingResult
    {
        public List<Hit> Hits { get; set; }
        public string CompositeTested { get; set; }
        public long TimeMS { get; set; }
        public DateTime EarliestDate { get; set; }
        public DateTime LatestDate { get; set; }
        public int totalPrimes { get; set; }
        public int totalNeutralTurnouts { get; set; }
        public int totalWinningTurnouts { get; set; }
        public int totalLosingTurnouts { get; set; }
        public int totalPassingTurnouts { get; set; }
        public int totalFailingTurnouts { get; set; }
    }

    public class Hit
    {
        public DateTime HitDate { get; set; }
        public DateTime TurnoutDate { get; set; }
        public string HitSymbol { get; set; }
        public decimal HitScore { get; set; }
        public decimal HitPrice { get; set; }
        public decimal TurnoutPrice { get; set; }
        public decimal PercentChange { get; set; }
    }
}
