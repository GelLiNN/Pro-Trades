namespace PT.Models.CoreModels
{
    public class BacktestingResult
    {
        public string CompositeTested { get; set; }
        public long TimeMS { get; set; }
        public int TotalPrimes { get; set; }
        public int TotalNeutralTurnouts { get; set; }
        public int TotalWinningTurnouts { get; set; }
        public int TotalLosingTurnouts { get; set; }
        public int TotalPassingTurnouts { get; set; }
        public int TotalFailingTurnouts { get; set; }
        public decimal CompositePassingPercent { get; set; }
        public decimal AveragePriceMovePercent { get; set; }
        public DateTime? EarliestDate { get; set; }
        public DateTime? LatestDate { get; set; }
        public List<Hit> Hits { get; set; }
    }

    public class Hit
    {
        public DateTime HitDate { get; set; }
        public DateTime TurnoutDate { get; set; }
        public string HitSymbol { get; set; }
        public decimal HitScore { get; set; }
        public decimal HitPriceClose { get; set; }
        public decimal TurnoutPrice { get; set; }
        public decimal TurnoutPercentChange { get; set; }
        public bool Passing { get; set; }
    }
}
