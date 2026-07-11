using PT.Models.CoreModels;
using PT.Models.RequestModels;
using Skender.Stock.Indicators;
using System.Diagnostics;
using YahooQuotesApi;
using static PT.Core.Maths;

namespace PT.Core
{
    //https://github.com/DaveSkender/Stock.Indicators
    //https://dotnet.stockindicators.dev/examples/#content
    //https://www.codeproject.com/Articles/15047/Creating-a-Mechanical-Trading-System-Part-1-Techni

    /// <summary>
    /// Base class for evaluating individual GRU modules for the overall AI system.
    /// </summary>
    public static class GRU
    {
        /// <summary>
        /// Calculate final prediction composite score decimal and prediction parameter set HS type string
        /// </summary>
        /// <param name="fr"></param>
        /// <param name="hr"></param>
        /// <param name="sr"></param>
        /// <param name="adxComposite"></param>
        /// <param name="obvComposite"></param>
        /// <param name="macdComposite"></param> 
        /// <param name="bbandsComposite"></param>
        /// <param name="aroonComposite"></param>
        /// <returns></returns>
        public static (decimal cs, string hs) ParametrizeComposites(FundamentalsResult fr,
            HedgeFundsResult hr, ShortInterestResult sr, decimal adxComposite, decimal obvComposite,
            decimal macdComposite, decimal bbandsComposite, decimal aroonComposite)
        {
            decimal compositeScoreFinal = 0;
            if (fr.FundamentalsComposite == Constants.CORE_INVALID_COMP && hr.RatingsComposite == Constants.CORE_INVALID_COMP)
            {
                //HS6 - RAW SIGNALS, RatingsComposite error & FundamentalsComposite error, 4th Generation
                compositeScoreFinal = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + bbandsComposite) / 6;
                return (compositeScoreFinal + Constants.CORE_HS6_MOD, Constants.HS4);
            }
            else if (hr.RatingsComposite == Constants.CORE_INVALID_COMP && fr.FundamentalsComposite != Constants.CORE_INVALID_COMP)
            {
                //HS5 - FINANCIAL INSTRUMENTS, RatingsComposite error, 2nd Generation
                compositeScoreFinal = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + fr.FundamentalsComposite + bbandsComposite) / 7;
                decimal hs5Mod = Constants.CORE_HS5_MOD;
                hs5Mod += Constants.FUND_HANDICAP_MODE_ENABLED ? 0.25M : 0;
                if (Constants.CORE_EXT_MODE_ENABLED) // extreme circumstances mode
                {
                    bool applyExtMod = compositeScoreFinal <= Constants.CORE_PRIME_GATE && bbandsComposite >= 70
                        && macdComposite >= 70 && fr.FundamentalsComposite >= 50 && sr.ShortInterestComposite >= 60;
                    hs5Mod = applyExtMod ? Constants.CORE_HS5_MOD * 2 : Constants.CORE_HS5_MOD;
                    compositeScoreFinal += hs5Mod;
                    return (applyExtMod && compositeScoreFinal > Constants.CORE_PRIME_GATE ?
                        Math.Min(compositeScoreFinal, Constants.CORE_EXT_MODE_SNAP) + (GetRandomInt(0, 21) * .01M) :
                        compositeScoreFinal, Constants.HS5);
                }
                return (compositeScoreFinal + Constants.CORE_HS5_MOD, Constants.HS5);
            }
            else if (fr.FundamentalsComposite == Constants.CORE_INVALID_COMP && hr.RatingsComposite != Constants.CORE_INVALID_COMP)
            {
                //HS4 - INSTITUTION DRIVEN, FundamentalsComposite error, 3rd Generation
                compositeScoreFinal = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + bbandsComposite + hr.RatingsComposite) / 7;
                return (compositeScoreFinal + Constants.CORE_HS4_MOD, Constants.HS4);
            }
            else if (bbandsComposite > aroonComposite && aroonComposite < obvComposite)
            {
                //HS3 - BBANDS AROON SWAP, 3rd Generation
                compositeScoreFinal = (adxComposite + bbandsComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
                decimal hs3Mod = Constants.CORE_HS3_MOD;
                if (Constants.CORE_EXT_MODE_ENABLED) // extreme circumstances mode
                {
                    bool applyExtMod = compositeScoreFinal <= Constants.CORE_PRIME_GATE && bbandsComposite >= 70
                        && fr.FundamentalsComposite >= 50 && hr.RatingsComposite >= 50 && sr.ShortInterestComposite >= 60;
                    hs3Mod = applyExtMod ? Constants.CORE_HS3_MOD * 2 : Constants.CORE_HS3_MOD;
                    compositeScoreFinal += hs3Mod;
                    return (applyExtMod && compositeScoreFinal > Constants.CORE_PRIME_GATE ?
                        Math.Min(compositeScoreFinal, Constants.CORE_EXT_MODE_SNAP) + (GetRandomInt(0, 21) * .01M) :
                        compositeScoreFinal, Constants.HS3);
                }
                else
                {
                    return (compositeScoreFinal + hs3Mod, Constants.HS3);
                }
            }
            else if (bbandsComposite > obvComposite && obvComposite < aroonComposite)
            {
                //HS2 - BBANDS OBV SWAP, 2nd Generation
                compositeScoreFinal = (adxComposite + aroonComposite + bbandsComposite + macdComposite +
                    sr.ShortInterestComposite + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
                decimal hs2Mod = Constants.CORE_HS2_MOD;
                if (Constants.CORE_EXT_MODE_ENABLED) // extreme circumstances mode
                {
                    bool applyExtMod = compositeScoreFinal <= Constants.CORE_PRIME_GATE && bbandsComposite >= 70
                        && fr.FundamentalsComposite >= 50 && hr.RatingsComposite >= 50 && sr.ShortInterestComposite >= 60;
                    hs2Mod = applyExtMod ? Constants.CORE_HS2_MOD * 2 : Constants.CORE_HS2_MOD;
                    compositeScoreFinal += hs2Mod;
                    return (applyExtMod && compositeScoreFinal > Constants.CORE_PRIME_GATE ?
                        Math.Min(compositeScoreFinal, Constants.CORE_EXT_MODE_SNAP) + (GetRandomInt(0, 21) * .01M) :
                        compositeScoreFinal, Constants.HS2);
                }
                else
                {
                    return (compositeScoreFinal + hs2Mod, Constants.HS2);
                }
            }
            else
            {
                //HS1 - PURE FORM, Original
                compositeScoreFinal = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
                compositeScoreFinal += bbandsComposite <= 33 ? -1 : 0;
                decimal hs1Mod = Constants.CORE_HS1_MOD;
                if (Constants.CORE_EXT_MODE_ENABLED) // extreme circumstances mode
                {
                    bool applyExtMod = compositeScoreFinal <= Constants.CORE_PRIME_GATE && macdComposite >= 75
                        && fr.FundamentalsComposite >= 60 && hr.RatingsComposite >= 60 && sr.ShortInterestComposite >= 60;
                    hs1Mod = applyExtMod ? Constants.CORE_HS1_MOD * 2 : Constants.CORE_HS1_MOD;
                    compositeScoreFinal += hs1Mod;
                    return (applyExtMod && compositeScoreFinal > Constants.CORE_PRIME_GATE ?
                        Math.Min(compositeScoreFinal, Constants.CORE_EXT_MODE_SNAP) + (GetRandomInt(0, 21) * .01M) :
                        compositeScoreFinal, Constants.HS1);
                }
                else
                {
                    return (compositeScoreFinal + hs1Mod, Constants.HS1);
                }
            }
        }

        /// <summary>
        /// Calculate final prediction composite score decimal and prediction parameter set HS type string
        /// </summary>
        /// <param name="fr"></param>
        /// <param name="hr"></param>
        /// <param name="sr"></param>
        /// <param name="adxComposite"></param>
        /// <param name="obvComposite"></param>
        /// <param name="macdComposite"></param> 
        /// <param name="bbandsComposite"></param>
        /// <param name="aroonComposite"></param>
        /// <returns>Dictionary of prediction scores and their types</returns>
        public static Dictionary<string, decimal> ParametrizeCompositesNew(FundamentalsResult fr,
            HedgeFundsResult hr, ShortInterestResult sr, decimal adxComposite, decimal obvComposite,
            decimal macdComposite, decimal bbandsComposite, decimal aroonComposite)
        {
            Dictionary<string, decimal> paramSetScores = new();

            //HS6 - RAW SIGNALS, RatingsComposite error & FundamentalsComposite error, 4th Generation
            if (fr.FundamentalsComposite == Constants.CORE_INVALID_COMP && hr.RatingsComposite == Constants.CORE_INVALID_COMP)
            {
                decimal hs6Score = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + bbandsComposite) / 6;
                hs6Score += Constants.CORE_HS6_MOD;
                paramSetScores.Add(Constants.HS6, hs6Score);
                return paramSetScores;
            }

            //HS5 - FINANCIAL INSTRUMENTS, RatingsComposite error, 2nd Generation
            if (hr.RatingsComposite == Constants.CORE_INVALID_COMP && fr.FundamentalsComposite != Constants.CORE_INVALID_COMP)
            {
                decimal hs5Score = (adxComposite + aroonComposite + obvComposite + macdComposite +
                    sr.ShortInterestComposite + fr.FundamentalsComposite + bbandsComposite) / 7;
                decimal hs5Mod = Constants.CORE_HS5_MOD;
                hs5Mod += Constants.FUND_HANDICAP_MODE_ENABLED ? 0.25M : 0;
                if (Constants.CORE_EXT_MODE_ENABLED) // extreme circumstances mode
                {
                    bool applyExtMod = hs5Score <= Constants.CORE_PRIME_GATE && bbandsComposite >= 70
                        && macdComposite >= 70 && fr.FundamentalsComposite >= 50 && sr.ShortInterestComposite >= 60;
                    hs5Mod = applyExtMod ? Constants.CORE_HS5_MOD * 2 : Constants.CORE_HS5_MOD;
                    hs5Score += hs5Mod;
                    hs5Score = applyExtMod && hs5Score > Constants.CORE_PRIME_GATE ?
                        Math.Min(hs5Score, Constants.CORE_EXT_MODE_SNAP) + (GetRandomInt(0, 17) * .01M) :
                        hs5Score;
                }
                else
                {
                    hs5Mod += bbandsComposite < 40 ? -.33M : 0;
                    hs5Mod += obvComposite < 40 ? -.33M : 0;
                    hs5Mod += obvComposite >= 55 && bbandsComposite >= 70 ? .42M : 0;
                    hs5Mod += macdComposite >= 55 && bbandsComposite >= 70 ? .42M : 0;
                    hs5Score += hs5Mod;
                }
                paramSetScores.Add(Constants.HS5, hs5Score);
                return paramSetScores;
            }

            //HS4 - BBANDS ADX SWAP, 4th generation
            decimal hs4Score = (bbandsComposite + aroonComposite + obvComposite + macdComposite +
                sr.ShortInterestComposite + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
            decimal hs4Mod = Constants.CORE_HS4_MOD;
            if (Constants.CORE_EXT_MODE_ENABLED) // extreme circumstances mode
            {
                bool applyExtMod = hs4Score <= Constants.CORE_PRIME_GATE && bbandsComposite >= 70
                    && fr.FundamentalsComposite >= 50 && hr.RatingsComposite >= 50 && sr.ShortInterestComposite >= 60;
                hs4Mod = applyExtMod ? Constants.CORE_HS4_MOD * 2 : Constants.CORE_HS4_MOD;
                hs4Score += hs4Mod;
                hs4Score = applyExtMod && hs4Score > Constants.CORE_PRIME_GATE ?
                    Math.Min(hs4Score, Constants.CORE_EXT_MODE_SNAP) + (GetRandomInt(0, 17) * .01M) :
                    hs4Score;
            }
            else
            {
                hs4Mod += adxComposite <= 49 ? -.22M : 0;
                hs4Mod += adxComposite <= 30 ? -.55M : 0;
                hs4Mod += fr.FundamentalsComposite >= 50 && bbandsComposite >= 70 ? .55M : 0;
                hs4Score += hs4Mod;
            }
            paramSetScores.Add(Constants.HS4, hs4Score);

            //HS3 - BBANDS AROON SWAP, 3rd generation
            decimal hs3Score = (adxComposite + bbandsComposite + obvComposite + macdComposite +
                sr.ShortInterestComposite + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
            decimal hs3Mod = Constants.CORE_HS3_MOD;
            if (Constants.CORE_EXT_MODE_ENABLED) // extreme circumstances mode
            {
                bool applyExtMod = hs3Score <= Constants.CORE_PRIME_GATE && bbandsComposite >= 70
                    && fr.FundamentalsComposite >= 50 && hr.RatingsComposite >= 50 && sr.ShortInterestComposite >= 60;
                hs3Mod = applyExtMod ? Constants.CORE_HS3_MOD * 2 : Constants.CORE_HS3_MOD;
                hs3Score += hs3Mod;
                hs3Score = applyExtMod && hs3Score > Constants.CORE_PRIME_GATE ?
                    Math.Min(hs3Score, Constants.CORE_EXT_MODE_SNAP) + (GetRandomInt(0, 17) * .01M) :
                    hs3Score;
            }
            else
            {
                hs3Mod += aroonComposite <= 49 ? -.22M : 0;
                hs3Mod += aroonComposite <= 30 ? -.55M : 0;
                hs3Mod += fr.FundamentalsComposite >= 50 && bbandsComposite >= 70 ? .55M : 0;
                hs3Score += hs3Mod;
            }
            paramSetScores.Add(Constants.HS3, hs3Score);

            //HS2 - BBANDS OBV SWAP, 2nd generation
            decimal hs2Score = (adxComposite + aroonComposite + bbandsComposite + macdComposite +
                sr.ShortInterestComposite + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
            decimal hs2Mod = Constants.CORE_HS2_MOD;
            if (Constants.CORE_EXT_MODE_ENABLED) // extreme circumstances mode
            {
                bool applyExtMod = hs2Score <= Constants.CORE_PRIME_GATE && bbandsComposite >= 70
                    && fr.FundamentalsComposite >= 50 && hr.RatingsComposite >= 50 && sr.ShortInterestComposite >= 60;
                hs2Mod = applyExtMod ? Constants.CORE_HS2_MOD * 2 : Constants.CORE_HS2_MOD;
                hs2Score += hs2Mod;
                hs2Score = applyExtMod && hs2Score > Constants.CORE_PRIME_GATE ?
                    Math.Min(hs2Score, Constants.CORE_EXT_MODE_SNAP) + (GetRandomInt(0, 17) * .01M) :
                    hs2Score;
            }
            else
            {
                hs2Mod += obvComposite <= 49 ? -.22M : 0;
                hs2Mod += obvComposite <= 30 ? -.55M : 0;
                hs2Mod += obvComposite >= 60 ? .31M : 0;
                hs2Mod += fr.FundamentalsComposite >= 55 && hr.RatingsComposite >= 55 && bbandsComposite >= 70 ? .61M : 0;
                hs2Score += hs2Mod;
            }
            paramSetScores.Add(Constants.HS2, hs2Score);

            //HS1 - PURE FORM, Original 1st generation
            decimal hs1Score = (adxComposite + aroonComposite + obvComposite + macdComposite +
                sr.ShortInterestComposite + fr.FundamentalsComposite + hr.RatingsComposite) / 7;
            decimal hs1Mod = Constants.CORE_HS1_MOD;
            if (Constants.CORE_EXT_MODE_ENABLED) // extreme circumstances mode
            {
                bool applyExtMod = hs1Score <= Constants.CORE_PRIME_GATE && macdComposite >= 75
                        && fr.FundamentalsComposite >= 60 && hr.RatingsComposite >= 60 && sr.ShortInterestComposite >= 60;
                hs1Mod = applyExtMod ? Constants.CORE_HS1_MOD * 2 : Constants.CORE_HS1_MOD;
                hs1Score += hs1Mod;
                hs1Score = applyExtMod && hs1Score > Constants.CORE_PRIME_GATE ?
                    Math.Min(hs1Score, Constants.CORE_EXT_MODE_SNAP) + (GetRandomInt(0, 21) * .01M) :
                    hs1Score;
            }
            else
            {
                hs1Mod += bbandsComposite <= 50 ? -.33M : 0;
                hs1Mod += bbandsComposite <= 30 ? -.88M : 0;
                hs1Mod += bbandsComposite >= 60 ? .31M : 0;
                hs1Mod += fr.FundamentalsComposite >= 55 && hr.RatingsComposite >= 55 && bbandsComposite >= 70 ? .61M : 0;
                hs1Score += hs1Mod;
            }
            paramSetScores.Add(Constants.HS1, hs1Score);

            return paramSetScores;
        }

        public static decimal GetADXComposite(IEnumerable<AdxResult> resultSet, int daysToCalculate)
        {
            List<AdxResult> results = resultSet.ToList();

            HashSet<string> dates = new HashSet<string>();
            int daysCalculated = 0;

            Queue<decimal> adxValueYList = new Queue<decimal>();
            Queue<decimal> pDmiValueYList = new Queue<decimal>();
            Queue<decimal> nDmiValueYList = new Queue<decimal>();

            decimal adxTotal = 0;
            decimal pDmiTotal = 0;
            decimal nDmiTotal = 0;
            bool hasBuySignal = false;
            bool hasSellSignal = false;
            int daysSinceSignal = -1;

            for (int i = results.Count - daysToCalculate; i < results.Count; i++)
            {
                if (daysCalculated < daysToCalculate)
                {
                    AdxResult result = results[i];

                    string adxDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(adxDate))
                    {
                        decimal adxVal = result.Adx != null ? (decimal)result.Adx : 0.0M;
                        adxValueYList.Enqueue(adxVal);
                        decimal plusDmiVal = result.Pdi != null ? (decimal)result.Pdi : 0.0M;
                        pDmiValueYList.Enqueue(plusDmiVal);
                        decimal negDmiVal = result.Mdi != null ? (decimal)result.Mdi : 0.0M;
                        nDmiValueYList.Enqueue(negDmiVal);
                        adxTotal += adxVal;
                        pDmiTotal += plusDmiVal;
                        nDmiTotal += negDmiVal;

                        //Get buy and sell signals
                        if (adxVal > 25 && plusDmiVal > negDmiVal)
                        {
                            //Cancel the previous sell signal if buy signal is most recent
                            hasBuySignal = true;
                            hasSellSignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }
                        else if (adxVal > 25 && plusDmiVal < negDmiVal)
                        {
                            //Cancel the previous buy signal if sell signal is most recent
                            hasSellSignal = true;
                            hasBuySignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }
                        daysCalculated++;
                        dates.Add(adxDate);
                    }
                }
                else
                    break;
            }

            List<decimal> adxXList = new List<decimal>();
            for (int i = 1; i <= daysToCalculate; i++)
                adxXList.Add(i);

            List<decimal> adxYList = adxValueYList.ToList();
            List<decimal> pDmiYList = pDmiValueYList.ToList();
            List<decimal> nDmiYList = nDmiValueYList.ToList();

            decimal pDmiSlope = GetSlope(adxXList, pDmiYList);
            decimal pDmiSlopeMultiplier = GetSlopeMultiplier(pDmiSlope);
            decimal pDmiAvg = pDmiTotal / daysToCalculate;

            decimal nDmiSlope = GetSlope(adxXList, nDmiYList);
            decimal nDmiSlopeMultiplier = GetSlopeMultiplier(nDmiSlope);
            decimal nDmiAvg = nDmiTotal / daysToCalculate;

            decimal adxSlope = GetSlope(adxXList, adxYList);
            decimal adxSlopeMultiplier = GetSlopeMultiplier(adxSlope);
            decimal adxAvg = adxTotal / daysToCalculate;

            List<decimal> adxZScores = GetZScores(adxYList);
            decimal zScoreSlope = GetSlope(adxXList, adxZScores);
            decimal zScoreSlopeMultiplier = GetSlopeMultiplier(zScoreSlope);

            bool averageDmiTrendingPositive = pDmiAvg > nDmiAvg;
            bool recentDmiTrendingPositive = pDmiYList[pDmiYList.Count - 1] > nDmiYList[nDmiYList.Count - 1];

            //base value average of the 2 most recent +DMI values, and adx average if trending
            //also add the most recent Z Score as an average percentage if it is positive
            decimal baseValue = (pDmiYList[pDmiYList.Count - 1] + pDmiYList[pDmiYList.Count - 2]) / 2;

            //Cap base value at 42 obviously
            baseValue = Math.Min(baseValue, 42.0M);

            //Add bonus for recent trending and avg trending
            decimal recentTrendingBonus = recentDmiTrendingPositive ? Constants.CORE_BONUS * 2 : 0;
            decimal averageTrendingBonus = averageDmiTrendingPositive ? Constants.CORE_BONUS * 2 : 0;

            //Add time-scaled bonus and penalty for buy and sell signals
            decimal buySignalBonus = CalcTimeScaledBuySignalBonus(hasBuySignal, Constants.CORE_BONUS, daysSinceSignal);
            decimal sellSignalPenalty = CalcTimeScaledSellSignalPenalty(hasSellSignal, Constants.CORE_BONUS, daysSinceSignal);

            //Add bonus for ADX average above 25 per investopedia recommendation
            decimal averageBuySignalBonus = adxAvg > 25 && hasBuySignal ? Constants.CORE_BONUS * 2 : 0;

            //Only add zscore slope bonus if +DMI > -DMI
            decimal zScoreSlopeBonus = (zScoreSlope > 0.1m) && averageDmiTrendingPositive ?
                (zScoreSlope * zScoreSlopeMultiplier) + Constants.CORE_BONUS : 0;

            decimal pDmiSlopeBonus = (pDmiSlope > 0.1m) ? Constants.CORE_BONUS * 2 : Constants.CORE_PENALTY * 2;
            decimal nDmiSlopeBonus = (nDmiSlope < -0.1m) ? Constants.CORE_BONUS * 2 : Constants.CORE_PENALTY * 2;

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += recentTrendingBonus;
            composite += averageTrendingBonus;
            composite += zScoreSlopeBonus;
            composite += pDmiSlopeBonus;
            composite += nDmiSlopeBonus;
            composite = Math.Min(composite, 75);
            composite += averageBuySignalBonus;
            composite += buySignalBonus;
            composite += composite > 50 ? sellSignalPenalty : 0;

            composite = Math.Max(composite, 0); //limit ADX composite to 0, no negatives
            return Math.Min(composite, 100); //cap ADX composite at 100, no extra weight
        }

        public static decimal GetOBVComposite(IEnumerable<ObvResult> resultSet, int daysToCalculate)
        {
            List<ObvResult> results = resultSet.ToList();

            HashSet<string> dates = new HashSet<string>();
            Queue<decimal> obvValueYList = new Queue<decimal>();
            Queue<decimal> obvSmaValueYList = new Queue<decimal>();

            int daysCalculated = 0;
            decimal obvSum = 0;
            bool obvHasBuySignal1 = false;
            bool obvHasSellSignal1 = false;
            bool obvHasBuySignal2 = false;
            bool obvHasSellSignal2 = false;
            int daysSinceSignal1 = -1;
            int daysSinceSignal2 = -1;

            for (int i = results.Count - daysToCalculate; i < results.Count; i++)
            {
                if (daysCalculated < daysToCalculate)
                {
                    ObvResult result = results[i];
                    ObvResult prevResult = results[i - 1];

                    string obvDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(obvDate))
                    {
                        decimal obvValue = Convert.ToDecimal(result.Obv);
                        decimal obvSmaValue = Convert.ToDecimal(result.ObvSma);
                        decimal prevObvValue = Convert.ToDecimal(prevResult.Obv);
                        decimal prevObvSmaValue = Convert.ToDecimal(prevResult.ObvSma);

                        obvValueYList.Enqueue(obvValue);
                        obvSmaValueYList.Enqueue(obvSmaValue);
                        obvSum += obvValue;

                        //Get buy and sell signals
                        //Buy signal 1 when OBV value crosses negative to positive
                        bool obvCurrentIsNegative = obvValue < 0;
                        bool obvPrevIsNegative = prevObvValue < 0;
                        if (!obvCurrentIsNegative && obvPrevIsNegative)
                        {
                            //Cancel the previous sell signal if buy signal is most recent
                            obvHasBuySignal1 = true;
                            obvHasSellSignal1 = false;
                            daysSinceSignal1 = daysToCalculate - daysCalculated;
                        }
                        else if (obvCurrentIsNegative && !obvPrevIsNegative)
                        {
                            //Cancel the previous buy signal if sell signal is most recent
                            obvHasSellSignal1 = true;
                            obvHasBuySignal1 = false;
                            daysSinceSignal1 = daysToCalculate - daysCalculated;
                        }
                        //Buy signal 2 when OBV value crosses above or below SMA value
                        bool obvCurrentIsHigherThanSma = obvValue < obvSmaValue;
                        bool obvPrevIsHigherThanSma = prevObvValue < prevObvSmaValue;
                        if (obvCurrentIsHigherThanSma && !obvPrevIsHigherThanSma)
                        {
                            //Cancel the previous sell signal if buy signal is most recent
                            obvHasBuySignal2 = true;
                            obvHasSellSignal2 = false;
                            daysSinceSignal2 = daysToCalculate - daysCalculated;
                        }
                        else if (!obvCurrentIsHigherThanSma && obvPrevIsHigherThanSma)
                        {
                            //Cancel the previous buy signal if sell signal is most recent
                            obvHasSellSignal2 = true;
                            obvHasBuySignal2 = false;
                            daysSinceSignal2 = daysToCalculate - daysCalculated;
                        }

                        dates.Add(obvDate);
                        daysCalculated++;
                    }
                }
                else
                    break;
            }

            List<decimal> obvXList = new List<decimal>();
            for (int i = 1; i <= daysCalculated; i++)
                obvXList.Add(i);

            List<decimal> obvYList = obvValueYList.ToList();
            decimal obvSlope = GetSlope(obvXList, obvYList);
            List<decimal> obvSmaYList = obvSmaValueYList.ToList();
            decimal obvSmaSlope = GetSlope(obvXList, obvSmaYList);

            List<decimal> zScores = GetZScores(obvYList);
            decimal zScoreSlope = GetSlope(obvXList, zScores);
            decimal zScoreSlopeMultiplier = GetSlopeMultiplier(zScoreSlope);

            List<decimal> normalizedScores = GetNormalizedData(obvYList);
            decimal normalizedSlope = GetSlope(obvXList, normalizedScores);
            decimal normalizedSlopeMultiplier = GetSlopeMultiplier(normalizedSlope);

            //Figures for OBV composite calculations
            decimal lastObv = obvYList[obvYList.Count - 1];
            decimal lastObvSma = obvSmaYList[obvSmaYList.Count - 1];
            decimal obvAverage = obvSum / daysCalculated;

            //OBV base value function
            decimal baseValue = 0;
            //Start with the average of the 2 most recent OBV Normalized Scores
            //Only allow positive normalizedScoreBase, divide by 4 instead of 2 (which would be classic mean)
            decimal normalizedScoreBase =
                ((normalizedScores[normalizedScores.Count - 1] + normalizedScores[normalizedScores.Count - 2]) / 4) * 100;
            normalizedScoreBase = normalizedScoreBase < 0 ? Constants.CORE_BONUS * 5 : normalizedScoreBase;

            //ZScore base helps us get the base value for composite from derivatives
            //Only allow positive zScoreBase, divide by 4 instead of 2 (which would be classic mean)
            decimal zScoreBase = ((zScores[zScores.Count - 1] + zScores[zScores.Count - 2]) / 4) * 100;
            zScoreBase = zScoreBase < 0 ? Constants.CORE_BONUS * 5 : zScoreBase;

            baseValue = (normalizedScoreBase + zScoreBase) / 2.0M;

            //Cap base value at 42 obviously
            baseValue = Math.Min(baseValue, 42.0M);

            //Add OBV average bonuses comparing last OBV values to averages
            decimal obvAverageBonus = obvAverage > 0 ? Constants.CORE_BONUS : 0;
            decimal obvHigherThan7dAvgBonus = lastObv > obvAverage ? Constants.CORE_BONUS * 2 : 0;
            decimal obvHigherThanSmaBonus = lastObv > lastObvSma ? Constants.CORE_BONUS * 2 : 0;
            obvAverageBonus += obvHigherThan7dAvgBonus + obvHigherThanSmaBonus;

            //Add bonuses if last OBV or OBV sum are greater than 1mil
            decimal obvLastGatedBonus = lastObv > Constants.MILLION ? Constants.CORE_BONUS : 0;
            decimal obvSumGatedBonus = obvSum > Constants.MILLION ? Constants.CORE_BONUS : 0;
            decimal obvGateBonus = obvLastGatedBonus + obvSumGatedBonus;

            //Add bonus if OBV slopes positive
            decimal obvSlopeBonus = obvSlope > 0 ? Constants.CORE_BONUS * 2 : 0;
            obvSlopeBonus += obvSmaSlope > 0 ? Constants.CORE_BONUS * 2 : 0;

            //Add Zscore slope bonus
            decimal zScoreSlopeBonus = 0;
            if (zScoreSlope > 0.05m && obvAverage > 0 && obvSlope > 0)
                zScoreSlopeBonus += (zScoreSlope * zScoreSlopeMultiplier);
            if (zScoreSlope > 0.05m)
                zScoreSlopeBonus += Constants.CORE_BONUS;

            //Add Normalized slope bonus
            decimal normalizedSlopeBonus = 0;
            if (normalizedSlope > 0.05m && obvAverage > 0 && obvSlope > 0)
                normalizedSlopeBonus += (normalizedSlope * normalizedSlopeMultiplier);
            if (normalizedSlope > 0.05m)
                normalizedSlopeBonus += Constants.CORE_BONUS;

            //Get time-scaled buy and sell signal bonus and penalty
            decimal buySignal1Bonus = CalcTimeScaledBuySignalBonus(obvHasBuySignal1, Constants.CORE_BONUS, daysSinceSignal1);
            decimal sellSignal1Penalty = CalcTimeScaledSellSignalPenalty(obvHasSellSignal1, Constants.CORE_BONUS, daysSinceSignal1);
            decimal buySignal2Bonus = CalcTimeScaledBuySignalBonus(obvHasBuySignal2, Constants.CORE_BONUS * Constants.HALF, daysSinceSignal2);
            decimal sellSignal2Penalty = CalcTimeScaledSellSignalPenalty(obvHasSellSignal2, Constants.CORE_BONUS * Constants.THIRD, daysSinceSignal2);

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += obvAverageBonus;
            composite += obvSlopeBonus;
            composite += obvGateBonus;
            composite += zScoreSlopeBonus;
            composite += normalizedSlopeBonus;
            composite = Math.Min(composite, 60);
            composite += composite == 60 ? Constants.CORE_BONUS : 0;
            composite += buySignal1Bonus;
            composite += buySignal2Bonus;
            composite += composite > 50 ? sellSignal1Penalty : 0;
            composite += composite > 60 ? sellSignal2Penalty : 0;

            composite = Math.Max(composite, 0); //limit OBV composite at 0, no negatives
            return Math.Min(composite, 100); //cap OBV composite at 100, no extra weight
        }

        public static decimal GetMACDComposite(IEnumerable<MacdResult> resultSet, int daysToCalculate)
        {
            List<MacdResult> results = resultSet.ToList();

            HashSet<string> dates = new HashSet<string>();
            Queue<decimal> macdHistYList = new Queue<decimal>();
            Queue<decimal> macdBaseYList = new Queue<decimal>();
            Queue<decimal> macdSignalYList = new Queue<decimal>();

            int daysCalculated = 0;
            decimal macdTotalHist = 0;
            decimal macdTotalBase = 0;
            decimal macdTotalSignal = 0;
            int positiveHistDays = 0;
            int daysSinceSignal = -1;
            bool macdHasBuySignal = false;
            bool macdHasSellSignal = false;

            for (int i = results.Count - daysToCalculate; i < results.Count; i++)
            {
                if (daysCalculated < daysToCalculate)
                {
                    MacdResult result = results[i];
                    MacdResult prevResult = results[i - 1];

                    string macdDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(macdDate))
                    {
                        decimal macdBaseValue = result.Macd != null ? (decimal)result.Macd : 0.0M;
                        decimal macdSignalValue = result.Signal != null ? (decimal)result.Signal : 0.0M;
                        decimal macdHistogramValue = result.Histogram != null ? (decimal)result.Histogram : 0.0M;

                        decimal prevMacdBaseValue = prevResult.Macd != null ? (decimal)prevResult.Macd : 0.0M;
                        decimal prevMacdSignalValue = prevResult.Signal != null ? (decimal)prevResult.Signal : 0.0M;
                        decimal prevMacdHistogramValue = prevResult.Histogram != null ? (decimal)prevResult.Histogram : 0.0M;

                        macdHistYList.Enqueue(macdHistogramValue);
                        macdBaseYList.Enqueue(macdBaseValue);
                        macdSignalYList.Enqueue(macdSignalValue);
                        macdTotalHist += macdHistogramValue;
                        macdTotalBase += macdBaseValue;
                        macdTotalSignal += macdSignalValue;

                        //Look for buy and sell signals
                        bool macdCurrentIsNegative = macdBaseValue < macdSignalValue;
                        bool macdPrevIsNegative = prevMacdBaseValue < prevMacdSignalValue;
                        if (!macdCurrentIsNegative && macdPrevIsNegative)
                        {
                            //Cancel the previous sell signal if buy signal is most recent
                            macdHasBuySignal = true;
                            macdHasSellSignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }
                        else if (macdCurrentIsNegative && !macdPrevIsNegative)
                        {
                            //Cancel the previous buy signal if sell signal is most recent
                            macdHasSellSignal = true;
                            macdHasBuySignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }

                        //Positive hist days
                        if (!macdCurrentIsNegative)
                        {
                            positiveHistDays++;
                        }

                        dates.Add(macdDate);
                        daysCalculated++;
                    }
                }
                else
                    break;
            }

            List<decimal> macdXList = new List<decimal>();
            for (int i = 1; i <= daysCalculated; i++)
                macdXList.Add(i);

            List<decimal> baseYList = macdBaseYList.ToList();
            decimal baseSlope = GetSlope(macdXList, baseYList);
            List<decimal> signalYList = macdSignalYList.ToList();
            decimal signalSlope = GetSlope(macdXList, signalYList);
            List<decimal> histYList = macdHistYList.ToList();
            decimal histSlope = GetSlope(macdXList, histYList);

            List<decimal> zScores = GetZScores(histYList);
            decimal zScoreSlope = GetSlope(macdXList, zScores);
            decimal zScoreSlopeMultiplier = GetSlopeMultiplier(zScoreSlope);

            List<decimal> normalizedHist = GetNormalizedData(histYList);
            decimal normalizedHistSlope = GetSlope(macdXList, normalizedHist);
            decimal normalizedSlopeMultiplier = GetSlopeMultiplier(normalizedHistSlope);

            //Use total base and signal diffs, along with macd total hist to get macd base value
            decimal baseValue = 0;
            decimal macdBaseSignalDiff = macdTotalBase - macdTotalSignal;
            if (macdBaseSignalDiff > 0)
            {
                baseValue = (macdBaseSignalDiff * Constants.CORE_BONUS) + 5;
            }
            if (macdTotalHist > 0)
            {
                baseValue += (macdTotalHist * Constants.CORE_BONUS) + 5;
            }
            if (baseValue == 0)
            {
                baseValue += (3 * Constants.CORE_BONUS); //3-pi pity points
            }
            baseValue = Math.Min(30, baseValue);
            baseValue += positiveHistDays;

            decimal histSlopeBonus = (histSlope > 0) ? histSlope + (Constants.CORE_BONUS * 3) : 0;
            decimal baseSlopeBonus = (baseSlope > 0) ? baseSlope + (Constants.CORE_BONUS * 2) : 0;
            decimal signalSlopeBonus = (signalSlope > 0) ? signalSlope + Constants.CORE_BONUS : 0;

            //Add histogram zscore slope bonus
            decimal zScoreHistSlopeBonus = 0;
            if (zScoreSlope > 0.1m)
                zScoreHistSlopeBonus += (zScoreSlope * zScoreSlopeMultiplier);
            if (zScoreSlope > 0)
                zScoreHistSlopeBonus += (2 * Constants.CORE_BONUS);

            //Add normalized histogram slope bonus
            decimal normalizedHistSlopeBonus = 0;
            if (normalizedHistSlope >= 0.5m)
                normalizedHistSlopeBonus += normalizedHistSlope;
            if (normalizedHistSlope > 0)
                normalizedHistSlopeBonus += (2 * Constants.CORE_BONUS);

            //Get previous 2 base above signal bonus
            bool prevTwoBaseAboveSignal = baseYList[baseYList.Count - 1] > signalYList[signalYList.Count - 1]
                && baseYList[baseYList.Count - 2] > signalYList[signalYList.Count - 2];
            decimal baseAboveSignalBonus = prevTwoBaseAboveSignal ? Constants.CORE_BONUS * 3 : 0;

            //Get time-scaled buy and sell signal bonus and penalty
            decimal buySignalBonus = CalcTimeScaledBuySignalBonus(macdHasBuySignal, Constants.CORE_BONUS, daysSinceSignal);
            decimal sellSignalPenalty = CalcTimeScaledSellSignalPenalty(macdHasSellSignal, Constants.CORE_BONUS, daysSinceSignal);

            //Calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += histSlopeBonus;
            composite += baseSlopeBonus;
            composite += signalSlopeBonus;
            composite += zScoreHistSlopeBonus;
            composite += normalizedHistSlopeBonus;
            composite = Math.Min(80, composite);
            composite += baseAboveSignalBonus;
            composite += buySignalBonus;
            composite += composite > 50 ? sellSignalPenalty : 0;

            composite = Math.Max(composite, 0); //limit MACD composite at 0, no negatives
            return Math.Min(composite, 115); //cap MACD composite at 115, extra weight
        }

        public static decimal GetAROONComposite(IEnumerable<AroonResult> resultSet, int daysToCalculate)
        {
            List<AroonResult> results = resultSet.ToList();

            HashSet<string> dates = new HashSet<string>();
            Queue<decimal> aroonUpYList = new Queue<decimal>();
            Queue<decimal> aroonDownYList = new Queue<decimal>();
            Queue<decimal> aroonOscillatorYList = new Queue<decimal>();

            int daysCalculated = 0;
            int aroonPositiveDays = 0;
            int aroonNegativeDays = 0;
            decimal aroonUpTotal = 0;
            decimal aroonDownTotal = 0;
            int daysSinceSignal = -1;
            bool aroonHasBuySignal = false;
            bool aroonHasSellSignal = false;
            bool aroonHasRecentPositivityMinor = false;
            bool aroonHasRecentPositivityMajor = false;

            for (int i = results.Count - daysToCalculate; i < results.Count; i++)
            {
                if (daysCalculated < daysToCalculate)
                {
                    AroonResult result = results[i];
                    AroonResult prevResult = results[i - 1];
                    AroonResult prevPrevResult = results[i - 2];

                    string aroonDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(aroonDate))
                    {
                        decimal curAroonUpVal = result.AroonUp != null ? (decimal)result.AroonUp : 0.0M;
                        decimal curAroonDownVal = result.AroonDown != null ? (decimal)result.AroonDown : 0.0M;
                        decimal curAroonOsc = curAroonUpVal - curAroonDownVal;

                        aroonUpYList.Enqueue(curAroonUpVal);
                        aroonDownYList.Enqueue(curAroonDownVal);
                        aroonOscillatorYList.Enqueue(curAroonOsc);
                        aroonUpTotal += curAroonUpVal;
                        aroonDownTotal += curAroonDownVal;

                        decimal prevAroonUpVal = prevResult.AroonUp != null ? (decimal)prevResult.AroonUp : 0.0M;
                        decimal prevAroonDownVal = prevResult.AroonDown != null ? (decimal)prevResult.AroonDown : 0.0M;
                        decimal prevAroonOsc = prevAroonUpVal - prevAroonDownVal;

                        decimal prevPrevAroonUpVal = prevPrevResult.AroonUp != null ? (decimal)prevPrevResult.AroonUp : 0.0M;
                        decimal prevPrevAroonDownVal = prevPrevResult.AroonDown != null ? (decimal)prevPrevResult.AroonDown : 0.0M;
                        decimal prevPrevAroonOsc = prevPrevAroonUpVal - prevPrevAroonDownVal;

                        //Look for buy and sell signals
                        bool aroonCurrentIsNegative = curAroonOsc < 0;
                        bool aroonPrevIsNegative = prevAroonOsc < 0;
                        bool aroonPrevPrevIsNegative = prevPrevAroonOsc < 0;
                        bool aroonWithinBounds = AroonUpDownWithinBounds(curAroonUpVal, prevAroonUpVal,
                            curAroonDownVal, prevAroonDownVal);

                        if (!aroonCurrentIsNegative && aroonPrevIsNegative && aroonWithinBounds)
                        {
                            //Cancel the previous sell signal if buy signal is most recent
                            aroonHasBuySignal = true;
                            aroonHasSellSignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }
                        else if (aroonCurrentIsNegative && !aroonPrevIsNegative && aroonWithinBounds)
                        {
                            //Cancel the previous buy signal if sell signal is most recent
                            aroonHasSellSignal = true;
                            aroonHasBuySignal = false;
                            daysSinceSignal = daysToCalculate - daysCalculated;
                        }

                        //Look for recent positivity minor
                        if (!aroonCurrentIsNegative && !aroonPrevIsNegative)
                        {
                            aroonHasRecentPositivityMinor = true;
                        }
                        else if (aroonCurrentIsNegative && aroonPrevIsNegative)
                        {
                            aroonHasRecentPositivityMinor = false;
                        }

                        //Look for recent positivity major
                        if (!aroonCurrentIsNegative && !aroonPrevIsNegative && !aroonPrevPrevIsNegative)
                        {
                            aroonHasRecentPositivityMajor = true;
                        }
                        else if (aroonCurrentIsNegative && aroonPrevIsNegative && aroonPrevPrevIsNegative)
                        {
                            aroonHasRecentPositivityMajor = false;
                        }

                        //Count positive days
                        if (!aroonCurrentIsNegative)
                        {
                            aroonPositiveDays++;
                        }
                        else
                        {
                            aroonNegativeDays++;
                        }

                        dates.Add(aroonDate);
                        daysCalculated++;
                    }
                }
                else
                    break;
            }

            List<decimal> aroonXList = new List<decimal>();
            for (int i = 1; i <= daysCalculated; i++)
                aroonXList.Add(i);

            List<decimal> upYList = aroonUpYList.ToList();
            decimal upSlope = GetSlope(aroonXList, upYList);
            List<decimal> downYList = aroonDownYList.ToList();
            decimal downSlope = GetSlope(aroonXList, downYList);
            List<decimal> oscillatorYList = aroonOscillatorYList.ToList();
            decimal oscillatorSlope = GetSlope(aroonXList, oscillatorYList);

            decimal upSlopeMultiplier = GetSlopeMultiplier(upSlope);
            decimal downSlopeMultiplier = GetSlopeMultiplier(downSlope);
            decimal oscillatorSlopeMultiplier = GetSlopeMultiplier(oscillatorSlope);

            //Use percent diffs to get aroon base value
            decimal aroonAvgUp = Math.Max(aroonUpTotal / daysCalculated, 1.0M);
            decimal aroonAvgDown = Math.Max(aroonDownTotal / daysCalculated, 1.0M);
            decimal percentDiffDown = (aroonAvgDown / aroonAvgUp) * 100;
            decimal percentDiffUp = (aroonAvgUp / aroonAvgDown) * 100;

            //Whether aroonAvgUp or aroonAvgDown is higher, that one will be more than 100 percent of the other
            decimal baseBullResult = Math.Min(100 - percentDiffDown, 42); //base bull result caps at 42
            decimal baseBearResult = Math.Min(percentDiffUp, 30); //base bear result caps at 30
            decimal baseValue = (aroonAvgUp > aroonAvgDown) ? baseBullResult : baseBearResult;
            baseValue += aroonPositiveDays * Constants.HALF;
            baseValue += aroonPositiveDays > aroonNegativeDays ? Constants.CORE_BONUS : 0;

            //Get recent positivity modifiers
            decimal recentPositivityMinorModifier = aroonHasRecentPositivityMinor ? Constants.CORE_BONUS : 0;
            decimal recentPositivityMajorModifier = aroonHasRecentPositivityMajor ? 2 * Constants.CORE_BONUS : 0;

            //Get aroon average modifier
            decimal aroonAvgModifier = aroonAvgUp > aroonAvgDown ? Constants.CORE_BONUS * 2 : Constants.CORE_PENALTY * 2;

            //Get slope modifiers
            decimal oscilatorSlopeModifier = (oscillatorSlope > 1.0M) ? oscillatorSlope + (2 * Constants.CORE_BONUS) : Constants.CORE_PENALTY * 3;
            oscilatorSlopeModifier = oscilatorSlopeModifier > 20 ? Math.Max(20, oscilatorSlopeModifier) : oscilatorSlopeModifier;

            decimal downSlopeModifier = (downSlope < 0) ? (-1 * downSlope) + (2 * Constants.CORE_BONUS) : (-1 * downSlope) + (Constants.CORE_PENALTY * 2);
            downSlopeModifier = downSlopeModifier > 20 ? Math.Max(20, downSlopeModifier) : downSlopeModifier;
            downSlopeModifier = downSlopeModifier < -20 ? Math.Max(-20, downSlopeModifier) : downSlopeModifier;

            //Get time-scaled buy and sell signal bonus and penalty
            decimal buySignalBonus = CalcTimeScaledBuySignalBonus(aroonHasBuySignal, Constants.CORE_BONUS, daysSinceSignal);
            decimal sellSignalPenalty = CalcTimeScaledSellSignalPenalty(aroonHasSellSignal, Constants.CORE_BONUS, daysSinceSignal);

            //Get other bonuses
            decimal lastOscValue = oscillatorYList[oscillatorYList.Count - 1];

            //Add bull major bonus if last AROON UP >= 70 per investopedia recommendation
            //This is the same as when last AROON OSC >= 50
            decimal bullMajorBonus = (lastOscValue >= 50) ? Constants.CORE_BONUS * 2 : 0;

            //calculate composite score based on the following values and weighted multipliers
            //if AROON avg up > AROON avg down, start score with 100 - (down as % of up)
            //if AROON avg up < AROON avg down, start score with 100 - (up as % of down)
            decimal composite = 0;
            composite += baseValue;
            composite += recentPositivityMinorModifier;
            composite += recentPositivityMajorModifier;
            composite += aroonAvgModifier;
            composite += oscilatorSlopeModifier;
            composite += downSlopeModifier;
            composite = Math.Min(composite, 70 + (Constants.CORE_BONUS * 2));
            composite += bullMajorBonus;
            composite += buySignalBonus;
            composite += composite > 50 ? sellSignalPenalty : 0;
            composite += composite < 70 ? (Constants.CORE_BONUS - 2.55M) : 0;
            composite = Math.Max(composite, 0); //limit AROON composite at 0, no negatives
            return Math.Min(composite, 115); //cap AROON composite at 115, extra weight
        }

        private static bool AroonUpDownWithinBounds(decimal curAroonUpVal, decimal prevAroonUpVal, decimal curAroonDownVal, decimal prevAroonDownVal)
        {
            decimal aroonUpMidpoint = (curAroonUpVal + prevAroonUpVal) / 2.0M;
            decimal aroonDownMidpoint = (curAroonDownVal + prevAroonDownVal) / 2.0M;
            decimal aroonCrossingPoint = (aroonUpMidpoint + aroonDownMidpoint) / 2.0M;

            return (aroonCrossingPoint >= 30 && aroonCrossingPoint <= 70);
        }

        public static decimal GetBBANDSComposite(IEnumerable<BollingerBandsResult> resultSet, List<Skender.Stock.Indicators.Quote> supplement, int daysToCalculate)
        {
            List<BollingerBandsResult> results = resultSet.ToList();

            HashSet<string> dates = new HashSet<string>();
            Queue<decimal> lowerBandYList = new Queue<decimal>();
            Queue<decimal> middleBandYList = new Queue<decimal>();
            Queue<decimal> upperBandYList = new Queue<decimal>();
            Queue<decimal> differenceValueYList = new Queue<decimal>();

            decimal highestUpperPrice = 0;
            decimal lowestMiddlePrice = 0;
            decimal averageLowerPrice = 0;
            decimal calculationsCount = 0;
            foreach (BollingerBandsResult result in resultSet)
            {
                if (result.LowerBand != null && result.Sma != null && result.UpperBand != null)
                {
                    highestUpperPrice = Math.Max(highestUpperPrice, (decimal)result.UpperBand);
                    lowestMiddlePrice = lowestMiddlePrice == 0 ? (decimal)result.Sma : Math.Min(lowestMiddlePrice, (decimal)result.Sma);
                    averageLowerPrice += (decimal)result.LowerBand;
                    calculationsCount++;
                }
            }
            averageLowerPrice = averageLowerPrice / calculationsCount;
            decimal historicalBandsMidpoint = (highestUpperPrice + lowestMiddlePrice + lowestMiddlePrice) / Constants.THREE;

            int daysCalulated = 0;
            for (int i = results.Count - daysToCalculate; i < results.Count; i++)
            {
                if (daysCalulated < daysToCalculate)
                {
                    BollingerBandsResult result = results[i];

                    string bbandsDate = result.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(bbandsDate))
                    {
                        decimal lowerBandValue = result.LowerBand != null ? (decimal)result.LowerBand : 0.0M;
                        decimal middleBandValue = result.Sma != null ? (decimal)result.Sma : 0.0M;
                        decimal upperBandValue = result.UpperBand != null ? (decimal)result.UpperBand : 0.0M;
                        decimal difference = upperBandValue - lowerBandValue;

                        lowerBandYList.Enqueue(lowerBandValue);
                        middleBandYList.Enqueue(middleBandValue);
                        upperBandYList.Enqueue(upperBandValue);
                        differenceValueYList.Enqueue(difference);

                        dates.Add(bbandsDate);
                        daysCalulated++;
                    }
                }
                else
                    break;
            }

            List<decimal> bbandsXList = new List<decimal>();
            for (int i = 1; i <= daysCalulated; i++)
                bbandsXList.Add(i);

            List<decimal> lowerYList = lowerBandYList.ToList();
            decimal lowerSlope = GetSlope(bbandsXList, lowerYList);
            List<decimal> middleYList = middleBandYList.ToList();
            decimal middleSlope = GetSlope(bbandsXList, middleYList);
            List<decimal> upperYList = upperBandYList.ToList();
            decimal upperSlope = GetSlope(bbandsXList, upperYList);
            List<decimal> differenceYList = differenceValueYList.ToList();
            decimal differenceSlope = GetSlope(bbandsXList, differenceYList);

            //TODO: Unused for now, may remove
            decimal lowerSlopeMultiplier = GetSlopeMultiplier(lowerSlope);
            decimal middleSlopeMultiplier = GetSlopeMultiplier(middleSlope);
            decimal upperSlopeMultiplier = GetSlopeMultiplier(upperSlope);

            //if there is more divergence than convergence in the last N days and there's positive price movement
            //if the current price is approaching the lower band and there's positive prive volume action
            //measure arbitrary base value as the percentage difference between the current price and the upper band

            //if there's more convergence than divergence we have low volatility, we don't want to subtract from the score
            //1 negative day when the price is approaching the upper band would probably generate a good enough sell signal
            //measure arbitrary base value minus the percentage difference between the current price and the lower band
            decimal breakoutSlopeCutoff = 0.15M;
            bool hasBreakout = differenceSlope >= breakoutSlopeCutoff;

            // look for buy and sell signals
            bool bbandsMinorBuySignal = false;
            bool bbandsMajorBuySignal = false;
            bool bbandsMinorSellSignal = false;
            bool bbandsMajorSellSignal = false;
            bool crossAboveMiddleBand = false;
            bool crossBelowMiddleBand = false;
            int daysSinceCrossAboveMiddleBand = -1;
            int daysSinceCrossBelowMiddleBand = -1;
            int daysSinceMinorBuySignal = -1;
            int daysSinceMajorBuySignal = -1;
            int daysSinceMinorSellSignal = -1;
            int daysSinceMajorSellSignal = -1;
            List<Skender.Stock.Indicators.Quote> ochlvList = supplement.ToList();
            List<decimal> prices = new List<decimal>();
            for (int i = 0; i < ochlvList.Count; i++)
            {
                var ochlv = ochlvList[i];
                decimal curPrice = ochlv.Close;
                prices.Add(curPrice);
                bool hasPrevPrice = i - 1 >= 0;

                if (curPrice < lowerYList[i])
                {
                    // Minor buy signal for crossing below the lower band
                    bbandsMinorBuySignal = true;
                    daysSinceMinorBuySignal = daysToCalculate - i;
                }
                if (curPrice > upperYList[i])
                {
                    // Minor sell signal for crossing above the upper band
                    bbandsMinorSellSignal = true;
                    daysSinceMinorSellSignal = daysToCalculate - i;
                }

                if (hasPrevPrice && curPrice >= middleYList[i] && prices[i - 1] < middleYList[i - 1])
                {
                    if (hasBreakout)
                    {
                        // Major buy signal for crossing above the middle band during breakout
                        bbandsMajorBuySignal = true;
                        daysSinceMajorBuySignal = daysToCalculate - i;
                    }
                    if (bbandsMinorSellSignal)
                    {
                        // Undo minor sell signal if cross back above middle band
                        bbandsMinorSellSignal = false;
                        daysSinceMinorSellSignal = -1;
                    }
                    crossAboveMiddleBand = true;
                    daysSinceCrossAboveMiddleBand = daysToCalculate - i;
                    crossBelowMiddleBand = false;
                    daysSinceCrossBelowMiddleBand = -1;
                }
                else if (hasPrevPrice && curPrice < middleYList[i] && prices[i - 1] >= middleYList[i - 1])
                {
                    if (hasBreakout)
                    {
                        // Major sell signal for crossing below the middle band during breakout
                        bbandsMajorSellSignal = true;
                        daysSinceMajorSellSignal = daysToCalculate - i;

                        // Undo major buy signal if major sell signal encountered
                        bbandsMajorBuySignal = false;
                        daysSinceMajorBuySignal = -1;
                    }
                    crossBelowMiddleBand = true;
                    daysSinceCrossBelowMiddleBand = daysToCalculate - i;
                    crossAboveMiddleBand = false;
                    daysSinceCrossAboveMiddleBand = -1;
                }
            }

            // BBAND figures
            decimal lastPrice = prices[prices.Count - 1];
            bool recentPositivity = lastPrice > prices[prices.Count - 4] ||
                lastPrice > prices[prices.Count - 3] || lastPrice > prices[prices.Count - 2];
            decimal priceSlope = GetSlope(bbandsXList, prices);
            bool allSlopesPositive = lowerSlope > 0 && middleSlope > 0 && upperSlope > 0;
            bool allSlopesNegative = lowerSlope < -.05M && middleSlope < -.05M && upperSlope < -.05M;
            bool keySlopesNegative = lowerSlope < -.1M && middleSlope < -.1M;
            bool priceAboveMiddleBand = lastPrice > middleYList[middleYList.Count - 1];
            bool priceAboveUpperBand = lastPrice > upperYList[upperYList.Count - 1];
            bool priceBelowLowerBand = lastPrice < lowerYList[lowerYList.Count - 1];
            bool noRecentCrossBelowMiddle = daysSinceCrossBelowMiddleBand <= 0 ||
                daysSinceCrossBelowMiddleBand >= Constants.BBANDS_BELOW_MIDDLE_CUTOFF_DAYS;
            bool priceBelowBandsMidpoint = lastPrice <= historicalBandsMidpoint + (historicalBandsMidpoint * .015M);
            int middleBandCrossMultiplier = daysSinceCrossAboveMiddleBand > 0 ?
                daysToCalculate - daysSinceCrossAboveMiddleBand - 1 : 0;

            // Base value from percentage diff from the middle band if above middle band (bullish conditions)
            // Base value from percentage diff from the lower band if below middle band (rebound conditions)
            // Supplement base value with recent positivity and bbands historical range values
            decimal baseValue = 0;
            decimal baseValueDivider = bbandsMajorSellSignal ? Constants.THREE : Constants.TWO;
            if (priceAboveMiddleBand) // Base value case 1 above middle band
            {
                baseValue += Constants.CORE_BONUS - 3;
                decimal percentageDiffBullish = GetPercentDiff(middleYList[middleYList.Count - 1], lastPrice);
                if (priceAboveUpperBand) // Pity points if price above upper band (lowest base value condition)
                {
                    baseValue += Math.Min(percentageDiffBullish + (Constants.CORE_BONUS - 2.33M), 17);
                    baseValue += recentPositivity && priceBelowBandsMidpoint ? Constants.CORE_BONUS - 1.33M : 0;
                    baseValue += !priceBelowBandsMidpoint ? Constants.CORE_PENALTY : 0;
                }
                else
                {
                    baseValue += ((100 - percentageDiffBullish - 19) / baseValueDivider) - 0.5M;
                    baseValue += recentPositivity && priceBelowBandsMidpoint ? Constants.CORE_BONUS - 1.25M : 0;
                    baseValue += percentageDiffBullish <= 7 ? Constants.CORE_BONUS - 2.25M : 0;
                    baseValue += percentageDiffBullish <= 12.5M && priceBelowBandsMidpoint ? Constants.CORE_BONUS - 2.33M : 0;
                    baseValue += percentageDiffBullish > 12.5M ? Constants.CORE_PENALTY + 1 : 0;
                    baseValue += !priceBelowBandsMidpoint ? Constants.CORE_PENALTY - 1 : 0;
                    baseValue += bbandsMinorSellSignal ? Constants.CORE_PENALTY + 1 : 0;
                }

                // Cap base value at 40 with small bonus for max
                baseValue = Math.Min(baseValue, 40);
                baseValue += baseValue == 40 && (recentPositivity && priceBelowBandsMidpoint) ? Constants.CORE_BONUS - 2 : 0;
            }
            else // Base value case 2 below middle band
            {
                baseValue += Constants.CORE_BONUS;
                decimal percentageDiffRebound = GetPercentDiff(lowerYList[lowerYList.Count - 1], lastPrice);
                if (priceBelowLowerBand) // Reward for price being below 2.5 std devs (highest base value condition)
                {
                    baseValue += 50;
                    baseValue += !noRecentCrossBelowMiddle ? Constants.CORE_PENALTY - 1 : 0;
                }
                else
                {
                    baseValue += ((100 - percentageDiffRebound - Constants.THIRD) / baseValueDivider);
                    baseValue += recentPositivity && priceBelowBandsMidpoint ? Constants.CORE_BONUS : 0;
                    baseValue += percentageDiffRebound <= 9 ? Constants.CORE_BONUS - 1 : 0;
                    baseValue += percentageDiffRebound <= 14 && priceBelowBandsMidpoint ? Constants.CORE_BONUS - 2 : 0;
                    baseValue += percentageDiffRebound <= 14 && noRecentCrossBelowMiddle ? Constants.CORE_BONUS - 2 : 0;
                    baseValue += percentageDiffRebound > 14 ? Constants.CORE_PENALTY + 2 : 0;
                    baseValue += !priceBelowBandsMidpoint ? Constants.CORE_PENALTY + 1 : 0;
                }
                // Cap base value at 50 with small bonus for max
                baseValue = Math.Min(baseValue, 50);
                baseValue += baseValue == 50 && (recentPositivity && priceBelowBandsMidpoint) ? Constants.CORE_BONUS - 1 : 0;

                // Penalize base value for crossing below middle band recently
                baseValue += !noRecentCrossBelowMiddle ? Constants.CORE_PENALTY - (7 - daysSinceCrossBelowMiddleBand) : 0;
            }
            baseValue += recentPositivity ? Constants.CORE_BONUS - 1.5M : Constants.CORE_PENALTY + 1.7M;
            baseValue = Math.Max(baseValue, Constants.CORE_BONUS - 3); // Prevent negative baseValue

            // Bonus for bullish consolidation of the bands or rebound conditions
            decimal consolidationReboundBonus = 0;
            if (!priceAboveMiddleBand && !bbandsMajorSellSignal && (recentPositivity || priceSlope > .05M))
            {
                consolidationReboundBonus += Constants.CORE_BONUS - 1;
                consolidationReboundBonus += lastPrice - (lastPrice * .07M) < middleYList[middleYList.Count - 1] ?
                    Constants.CORE_BONUS : 0;
            }
            bool negativeSlopesRebound = middleSlope < -.01M && lowerSlope < -.01M && upperSlope > -.05M;
            consolidationReboundBonus += negativeSlopesRebound && noRecentCrossBelowMiddle &&
                (recentPositivity || lastPrice > averageLowerPrice) ? Constants.CORE_BONUS * 2 - 1 : 0;
            consolidationReboundBonus += negativeSlopesRebound && noRecentCrossBelowMiddle ?
                Constants.CORE_BONUS - 0.5M : 0;

            // Bonus for custom slope conditions 1, relative to other statistics
            bool positiveAndNegativeSlopes = (lowerSlope > 0 || middleSlope > 0 || upperSlope > 0) &&
                (lowerSlope < 0 || middleSlope < 0 || upperSlope < 0);
            decimal customSlopeBonusRel = differenceSlope > .05M && positiveAndNegativeSlopes && priceSlope > -.05M ?
                Constants.CORE_BONUS + 2 : 0;
            customSlopeBonusRel += lowerSlope > middleSlope + (Math.Abs(middleSlope) * .01M) ?
                Constants.CORE_BONUS + 1 : 0;
            customSlopeBonusRel += upperSlope < 0 && positiveAndNegativeSlopes && noRecentCrossBelowMiddle && priceSlope > -.05M ?
                Constants.CORE_BONUS : 0;
            customSlopeBonusRel += priceBelowBandsMidpoint && allSlopesPositive ? Constants.CORE_BONUS + 1 : 0;
            customSlopeBonusRel += !priceBelowBandsMidpoint && allSlopesPositive ? Constants.CORE_BONUS - 1 : 0;

            // Bonus for custom slope considitons 2, individualized with slope multipliers
            decimal customSlopeBonusInd = 0;
            customSlopeBonusInd += lowerSlope > 0.01M ? Constants.CORE_BONUS : 0;
            customSlopeBonusInd += lowerSlope > 1.0M && lowerSlope < 10.0M && recentPositivity && !bbandsMajorSellSignal ?
                (lowerSlope * lowerSlopeMultiplier) + 1.5M : 0;
                //Constants.CORE_BONUS - .5M : 0;
            customSlopeBonusInd += upperSlope > 0.01M && recentPositivity &&
                !bbandsMajorSellSignal && noRecentCrossBelowMiddle ? Constants.CORE_BONUS + 1 : 0;
            customSlopeBonusInd += upperSlope > 0.01M ? Constants.CORE_BONUS - 1 : 0;
            customSlopeBonusInd += middleSlope > 0.01M ? Constants.CORE_BONUS : 0;
            customSlopeBonusInd += middleSlope > 1.0M && middleSlope < 10.0M && (!priceAboveMiddleBand || priceBelowBandsMidpoint) ?
                Constants.CORE_BONUS + .5M : 0;
                //(middleSlope * middleSlopeMultiplier) + 1.5M : 0;

            decimal noSellSignalsBonus = !bbandsMajorSellSignal && !bbandsMinorSellSignal &&
                priceAboveMiddleBand ? Constants.CORE_BONUS + 1 : 0;
            noSellSignalsBonus = !bbandsMajorSellSignal && !bbandsMinorSellSignal && noRecentCrossBelowMiddle ?
                Constants.CORE_BONUS - 1 : noSellSignalsBonus;
            noSellSignalsBonus += !bbandsMajorSellSignal && !bbandsMinorSellSignal && priceBelowBandsMidpoint ? 2 : 0;

            // Modifier for bullish or bearish band range conditions relative to current price
            decimal bandRangeModifier = 0;
            bandRangeModifier += lowerYList[lowerYList.Count - 1] < historicalBandsMidpoint ? Constants.CORE_BONUS - 1 : -1;
            bandRangeModifier += upperYList[upperYList.Count - 1] < historicalBandsMidpoint ? Constants.CORE_BONUS - 0.25M : 0;
            bandRangeModifier += priceBelowBandsMidpoint && recentPositivity ? 2.88M : 0;
            bandRangeModifier += priceBelowBandsMidpoint ? 2.55M : -1.1M;
            bandRangeModifier += lastPrice < averageLowerPrice ? Constants.CORE_BONUS + 2 : 0;
            bandRangeModifier += bandRangeModifier == 0 &&
                !priceBelowBandsMidpoint && lastPrice > averageLowerPrice ? Constants.CORE_PENALTY * 2 + 1 : 0;

            //Get time-scaled buy and sell signal bonus and penalty
            decimal minorBuySignalBonus = bbandsMinorBuySignal ? Constants.CORE_BONUS + (daysToCalculate - daysSinceMinorBuySignal) + 1 : 0;
            minorBuySignalBonus += bbandsMinorBuySignal && hasBreakout ? Constants.CORE_BONUS : 0;
            decimal majorBuySignalBonus = CalcTimeScaledBuySignalBonus(bbandsMajorBuySignal, Constants.CORE_BONUS, daysSinceMajorBuySignal);
            majorBuySignalBonus += bbandsMajorBuySignal && recentPositivity ? Constants.CORE_BONUS : 0;
            decimal minorSellSignalPenalty = bbandsMinorSellSignal ? Constants.CORE_PENALTY - (daysToCalculate - daysSinceMinorSellSignal) - 1 : 0;
            minorSellSignalPenalty += bbandsMinorSellSignal && hasBreakout ? Constants.CORE_PENALTY : 0;
            decimal majorSellSignalPenalty = CalcTimeScaledSellSignalPenalty(bbandsMajorSellSignal, Constants.CORE_BONUS, daysSinceMajorSellSignal);

            //calculate composite score based on the following values and weighted multipliers
            decimal composite = 0;
            composite += baseValue;
            composite += consolidationReboundBonus;
            composite += customSlopeBonusRel;
            composite += customSlopeBonusInd;
            composite += noSellSignalsBonus;
            composite = Math.Min(composite, Constants.BBANDS_COMP_MID_LIMIT);
            composite += composite <= Constants.BBANDS_COMP_MID_LIMIT && crossAboveMiddleBand && !bbandsMajorBuySignal ?
                Constants.CORE_BONUS * middleBandCrossMultiplier : 0; // Non-breakout middle cross modifier
            composite += bandRangeModifier;
            composite = Math.Min(composite, Constants.BBANDS_COMP_UPPER_LIMIT);
            composite += minorBuySignalBonus;
            composite += majorBuySignalBonus;
            composite += majorSellSignalPenalty;
            composite += minorSellSignalPenalty;
            composite += composite > 30 && allSlopesNegative ? Constants.CORE_PENALTY - 1 : 0;
            composite += composite > 30 && keySlopesNegative && !allSlopesNegative ? Constants.CORE_PENALTY - 1 : 0;
            composite += (Constants.CORE_BONUS - 3);
            composite = Math.Min(composite, 100); // cap BBANDS composite at 100, no extra weight
            return Math.Max(0, composite); // limit BBANDS composite at 0, no negatives
        }

        /// <summary>
        /// Fundamentals (advanced stats, volume, price, earnings and filings up-to-date).
        /// Relies completely on unofficial yahoo finance API by dshe for now.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="quote"></param>
        /// <param name="history"></param>
        /// <returns></returns>
        public static FundamentalsResult GetFundamentalsResult(string symbol, Snapshot? quote, PTHistory history)
        {
            try
            {
                //Get last 10 VWAPs, Volume, and Date data from PTHistory
                List<decimal> normalizedPrice = GetNormalizedData(history.Price10YList);
                decimal priceSlope = GetSlope(history.Price10XList, history.Price10YList);

                decimal normalizedPriceSlope = GetSlope(history.Price10XList, normalizedPrice);
                decimal normalizedPriceSlopeMultiplier = GetSlopeMultiplier(normalizedPriceSlope);

                List<decimal> normalizedVolume = GetNormalizedData(history.Volume10YList);
                decimal volumeSlope = GetSlope(history.Volume10XList, history.Volume10YList);
                decimal normalizedVolumeSlope = GetSlope(history.Volume10XList, normalizedVolume);
                decimal normalizedVolumeSlopeMultiplier = GetSlopeMultiplier(normalizedVolumeSlope);

                //Get avg vwap slope for price target projection
                decimal vwapSlope = GetSlope(history.HistoricalVwapXList, history.HistoricalVwapYList);
                decimal avgVolumeSlope = GetSlope(history.HistoricalVolAvgXList, history.HistoricalVolAvgYList);

                // Collect values from YahooFinance
                decimal peTrailing = 0;
                decimal peForward = 0;
                decimal epsTrailing = 0;
                decimal epsCurrentYear = 0;
                decimal epsForward = 0;
                decimal priceToBook = 0;
                decimal bookValue = 0;
                decimal marketCap = 0;
                decimal sharesOutstanding = -1;
                decimal divRate = 0;
                decimal divYield = 0;
                decimal postMarketPrice = 0;
                decimal fiftyTwoWeekLow = 0;
                decimal fiftyTwoWeekHigh = 0;
                decimal netAssets = -1; // TODO: use to boost HS5
                decimal netExpenseRatio = 1;// TODO: use to boost HS5
                DateTime? nextEarningsDate = null;
                DateTime? prevEarningsDate = null;
                string? assetName = null;
                string? assetType = null;
                string? parseMessage = null;

                try
                {
                    if (quote != null)
                    {
                        peTrailing = Convert.ToDecimal(quote.TrailingPE);
                        peTrailing = peTrailing == 0 ? Convert.ToDecimal(quote.PriceEpsCurrentYear) : peTrailing;
                        peForward = Convert.ToDecimal(quote.ForwardPE);
                        peForward = peForward == 0 ? peTrailing : peForward;
                        epsTrailing = quote.EpsTrailingTwelveMonths;
                        epsCurrentYear = quote.EpsCurrentYear;
                        epsForward = quote.EpsForward == 0 ? epsCurrentYear : epsForward;
                        priceToBook = Convert.ToDecimal(quote.PriceToBook);
                        bookValue = Convert.ToDecimal(quote.BookValue);
                        marketCap = Convert.ToDecimal(quote.MarketCap);
                        sharesOutstanding = Convert.ToDecimal(quote.SharesOutstanding);
                        divRate = quote.DividendRate;
                        divYield = Convert.ToDecimal(quote.DividendYield);
                        netAssets = Convert.ToDecimal(quote.NetAssets);
                        netExpenseRatio = Convert.ToDecimal(quote.NetExpenseRatio);
                        postMarketPrice = quote.PostMarketPrice;
                        fiftyTwoWeekLow = quote.FiftyTwoWeekLow;
                        fiftyTwoWeekHigh = quote.FiftyTwoWeekHigh;
                        nextEarningsDate = quote.EarningsTimestampStart.ToDateTimeUtc();
                        prevEarningsDate = quote.EarningsTimestamp.ToDateTimeUtc();
                        assetName = !string.IsNullOrWhiteSpace(quote.LongName) ? quote.LongName : quote.ShortName;
                        assetType = quote.TypeDisp;
                    }
                }
                catch (Exception e)
                {
                    parseMessage = $"Unable to parse Yahoo Finance for statistics for symbol {symbol}";
                }

                // Get stats for PTHistory
                history.TodayPostMarket = postMarketPrice > 0 ? postMarketPrice : history.TodayClose;

                // Get base value starting with 1Mil USD bonus and VWAP slope bonus
                decimal baseValue = history.TodayVolUsd >= Constants.MILLION ? Constants.CORE_BONUS : 0;
                baseValue += vwapSlope > 0.33M ? Constants.CORE_BONUS : 0;

                // Get net expense ratio bonus to supplement HS5 financial instruments
                decimal netExpenseRatioBonus = CalcNetExpenseRatioBonus(netExpenseRatio);

                decimal bookValuePrice = 0;
                decimal bookValuePriceDiffPercent = 0;
                // HS5 or error case book value custom
                if (netAssets > 0 && netExpenseRatio > 0 && sharesOutstanding == 0)
                {
                    // Weighted Average 52 week low (2X) with 100d SMA
                    bookValuePrice = (fiftyTwoWeekLow * 2 + history.AveragePrice200Day) / Constants.THREE;
                    // Cost adjustment for this year net ETF fee, from net expense ratio
                    bookValuePrice = bookValuePrice - (netExpenseRatio / Constants.ONE_HUNDRED * bookValuePrice);
                }
                bool canGetBaseValue = (bookValuePrice > 0 || priceToBook > 0) && history.TodayVwap > 0;
                if (canGetBaseValue)
                {
                    // Get base value as gated function of price-to-book percentage diff, or custom HS5
                    bookValuePrice = bookValuePrice == 0 ? history.TodayVwap * (1 / priceToBook) : bookValuePrice;
                    bookValuePriceDiffPercent = GetPercentDiff(history.TodayVwap, bookValuePrice);
                    baseValue += bookValuePriceDiffPercent > 0 ? bookValuePriceDiffPercent : 0;
                    if (baseValue >= 0)
                    {
                        baseValue = Constants.CORE_BONUS - 1; // base undervalued bonus
                    }
                    if (baseValue >= 10)
                    {
                        baseValue += Constants.CORE_BONUS + 1; // more than 10% undervalued bonus
                    }
                    if (baseValue >= -10)
                    {
                        baseValue += Constants.CORE_BONUS * Constants.HALF; // less than 10% overvalued bonus
                    }
                    if (priceToBook <= 2.5M) // PB less than 2.5 Bonus
                    {
                        baseValue += Constants.CORE_BONUS;
                    }
                }
                if (baseValue < Constants.CORE_BONUS)
                {
                    baseValue = Constants.CORE_BONUS; // Pity points
                }
                baseValue = Math.Min(30, baseValue); // Apply gate to base value

                // Calculate net asset value if unavailable
                if ((netAssets < 0 || netAssets == 0) && sharesOutstanding > 0)
                {
                    // Otherwise get net asset value from bookValue from Yahoo
                    netAssets = bookValue > 0 ?
                        bookValue * sharesOutstanding : bookValuePrice * sharesOutstanding;
                }

                // Fair value price bonus
                decimal fairValuePriceBonus = 0;
                decimal fairValuePrice = 0;
                if (netAssets > 0 && sharesOutstanding > 0)
                {
                    fairValuePrice = netAssets / sharesOutstanding;
                    decimal avgPrice10d = history.AveragePrice10Day;
                    if (avgPrice10d < fairValuePrice)
                    {
                        fairValuePriceBonus = Constants.CORE_BONUS + 1;
                    }
                    else if (avgPrice10d / fairValuePrice <= 3)
                    {
                        fairValuePriceBonus = Constants.CORE_BONUS - Constants.HALF;
                    }
                    else if (avgPrice10d / fairValuePrice <= 7)
                    {
                        fairValuePriceBonus = Constants.CORE_BONUS * Constants.HALF - Constants.HALF;
                    }
                } // HS5 fair value custom
                else if (netAssets > 0 && netExpenseRatio > 0 && bookValuePrice > 0)
                {
                    fairValuePrice = bookValuePrice;
                    fairValuePrice += netAssets >= Constants.TEN_BILLION ?
                        fairValuePrice / Constants.ONE_HUNDRED : 0;
                    fairValuePriceBonus += netAssets >= Constants.TEN_BILLION ?
                        Constants.CORE_BONUS : 0;
                    fairValuePriceBonus += netAssets >= Constants.ONE_BILLION ?
                        Constants.CORE_BONUS * Constants.HALF : 0;
                }

                // Calculate figures for EPS bonus and PE bonus
                decimal averageEPS = 0.0M, growthEPS = 0.0M, averagePE = 0.0M, growthPE = 0.0M;
                averageEPS = epsForward > 0 ? (epsForward + epsTrailing + epsCurrentYear) / 3 : (epsTrailing + epsCurrentYear) / 2;
                epsForward = epsForward == 0 ? epsCurrentYear - (Math.Abs(epsCurrentYear) * .02M) : epsForward;
                growthEPS = epsForward - epsTrailing;

                averagePE = peForward > 0 ? (peForward + peTrailing) / 2 : peTrailing;
                peForward = peForward == 0 ? peTrailing - (Math.Abs(peTrailing) * .02M) : peTrailing;
                growthPE = peForward - peTrailing;

                // Get EPS activity modifier
                decimal epsModifier = CalcEPSModifier(averageEPS, growthEPS, epsTrailing);

                // Get PE ratio activity modifier
                decimal peModifier = CalcPEModifier(averagePE, growthPE);

                // Get volume trending modifier
                decimal volumeTrendingModifier = CalcVolumeTrendingModifier(history);

                // Get modifier for interactions with 30d SMA and 100d SMA
                decimal smaModifier = CalcSmaModifier(history);

                // Get dividend bonus
                decimal divBonus = CalcDividendBonus(divRate, divYield);

                // Get golden path bonus if todays dollar is volume geater than the 10d avg
                // dollar volume, and if 10d avg dollar volume is greater than the 30d avg dollar volume
                decimal goldenPathBonus = 0;
                bool hasGoldenPath = history.TodayVolUsd > (history.AverageVolUsd10Day + Constants.THIRTY_THOUSAND)
                    && history.AverageVolUsd10Day > (history.AverageVolUsd30Day + Constants.THIRTY_THOUSAND);
                if (hasGoldenPath)
                {
                    goldenPathBonus += Constants.FUND_HANDICAP_MODE_ENABLED ?
                        Constants.CORE_BONUS * 3 - 1 : Constants.CORE_BONUS * 2;
                }

                // Get normalized price slope and volume slope bonus
                decimal normalizedPriceVolBonus = normalizedPriceSlope > 0.025M ? Constants.CORE_BONUS * Constants.HALF + 1 : 0;
                normalizedPriceVolBonus += normalizedVolumeSlope > 0.025M ? Constants.CORE_BONUS * Constants.HALF + 1 : 0;

                // calculate composite score based on the following values and weighted multipliers
                // Base value should be calculated based on EPS and PE data
                // Bonuses added for positive v:wq
                // olume and price slopes, PE Growth, and dividends
                decimal composite = 0;
                composite += baseValue;
                composite += fairValuePriceBonus;
                composite += netExpenseRatioBonus;
                composite += peModifier;
                composite += normalizedPriceVolBonus;
                composite = Math.Min(60, composite);
                composite += goldenPathBonus;
                composite += epsModifier;
                composite += divBonus;
                composite += composite >= 50 && smaModifier < 0 ? smaModifier : 0;
                composite += smaModifier > 0 ? smaModifier : 0;
                composite += composite >= 50 && volumeTrendingModifier < 0 ? volumeTrendingModifier : 0;
                composite += volumeTrendingModifier > 0 ? volumeTrendingModifier : 0;
                // Give back half the PE penalty if composite is below fair
                composite += composite < 50 && peModifier < 0 ? (-0.5M * peModifier) : 0;
                // Add bonus if composite below fair and price inside SMA band
                composite += composite < 50 && history.IsInsideSmaBand ? Constants.CORE_BONUS * 2 - 1 : 0;
                // For handicapped mode
                composite += Constants.FUND_HANDICAP_MODE_ENABLED ? Constants.FUND_HANDICAP : 0;

                composite = Math.Min(composite, 100); // cap composite at 100, no extra weight
                composite = Math.Max(composite, 0); // limit composite at 0, no negatives

                // Custom fair value after GRU comp
                decimal customFairValue = CalcCustomFairValue(history.TodayVwap, fairValuePrice, bookValuePrice,
                    fiftyTwoWeekLow, averagePE, epsTrailing, composite);
                decimal priceToFairValue = Constants.FUND_HANDICAP_MODE_ENABLED ? 0 : history.TodayVwap / customFairValue;

                // Final GRU gates
                composite += composite > 60 && priceToFairValue > 5 ? Constants.CORE_PENALTY * 2 : 0;
                composite += composite > 60 && priceToFairValue > 10 ? Constants.CORE_PENALTY : 0;
                composite += composite < 60 && priceToBook < 2.5M && priceToFairValue < 2.25M ? Constants.CORE_BONUS : 0;
                composite += composite < 60 && priceToFairValue < 1.5M ? Constants.CORE_BONUS : 0;
                composite += composite < 60 && priceToFairValue < 1.0M ? Constants.CORE_BONUS : 0;

                decimal volUsdAvg = (history.TodayVolUsd + history.AverageVolUsd10Day + history.AverageVolUsd30Day) / Constants.THREE;
                bool hasDivs = divRate > 0 && divYield > 0;

                return new FundamentalsResult
                {
                    AssetName = assetName ?? "Not Found",
                    AssetType = assetType ?? "Not Found",
                    FundamentalsComposite = composite,
                    IsBullishSMA = history.IsBullishSMA,
                    IsBearishSMA = history.IsBearishSMA,
                    IsAboveSMABand = history.IsAboveSMABand,
                    IsBelowSMABand = history.IsBelowSMABand,
                    HasDividends = hasDivs,
                    HasGoldenPath = hasGoldenPath,
                    MarketCap = marketCap > 0 ? marketCap : sharesOutstanding * history.TodayVwap,
                    PriceToBook = priceToBook,
                    PriceToEarnings = peTrailing,
                    PriceToFairValue = priceToFairValue,
                    EarningsPerShare = epsTrailing,
                    BookValuePrice = bookValuePrice,
                    FairValuePrice = customFairValue,
                    AverageEPS = averageEPS,
                    AveragePE = averagePE,
                    GrowthEPS = growthEPS,
                    GrowthPE = growthPE,
                    DivRate = divRate,
                    DivYield = divYield,
                    NextEarningsDate = nextEarningsDate,
                    PrevEarningsDate = prevEarningsDate,
                    AveragePrice200Day = history.AveragePrice200Day,
                    AveragePrice100Day = history.AveragePrice100Day,
                    AveragePrice50Day = history.AveragePrice50Day,
                    AveragePrice30Day = history.AveragePrice30Day,
                    AveragePrice20Day = history.AveragePrice20Day,
                    AveragePrice10Day = history.AveragePrice10Day,
                    DollarVolumeToday = history.TodayVolUsd,
                    DollarVolume10Day = history.AverageVolUsd10Day,
                    DollarVolume30Day = history.AverageVolUsd30Day,
                    DollarVolumeAverage = volUsdAvg,
                    VolumeSlope = volumeSlope,
                    PriceSlope = priceSlope,
                    VwapSlope = vwapSlope,
                    Message = parseMessage ?? string.Empty
                };
            }
            catch (Exception e)
            {
                string msg = $"ERROR: Indicators.cs GetFundamentals for symbol {symbol}, message: {e.Message}";
                Debug.WriteLine(msg);
                return new FundamentalsResult
                {
                    FundamentalsComposite = Constants.CORE_INVALID_COMP,
                    IsBullishSMA = false,
                    IsBearishSMA = false,
                    IsAboveSMABand = false,
                    IsBelowSMABand = false,
                    HasDividends = false,
                    HasGoldenPath = false,
                    DollarVolumeToday = 0.0M,
                    DollarVolume10Day = 0.0M,
                    DollarVolume30Day = 0.0M,
                    DollarVolumeAverage = 0.0M,
                    VolumeSlope = 0.0M,
                    PriceSlope = 0.0M,
                    AverageEPS = 0.0M,
                    AveragePE = 0.0M,
                    GrowthEPS = 0.0M,
                    GrowthPE = 0.0M,
                    Message = msg
                };
            }
        }

        public static decimal CalcEPSModifier(decimal averageEPS, decimal growthEPS, decimal trailingEps)
        {
            decimal epsModifier = 0;

            // trailingEPS base
            if (trailingEps > 0)
            {
                epsModifier += Constants.CORE_BONUS * 2 - 1;
            }

            //If calculating figures negative return base
            if (averageEPS <= 0 && growthEPS <= 0)
            {
                return epsModifier;
            }

            // averageEPS score formulation
            // Reward cases
            if (0 < averageEPS && averageEPS <= 0.5M)
            {
                epsModifier += averageEPS * 10 + Constants.CORE_BONUS;
            }
            else if (0.5M < averageEPS && averageEPS <= 1)
            {
                epsModifier += averageEPS * 5 + (2 * Constants.CORE_BONUS);
            }
            else if (1 < averageEPS && averageEPS <= 2)
            {
                epsModifier += averageEPS * 2 + (3 * Constants.CORE_BONUS);
            }
            else if (2 < averageEPS && averageEPS <= 5)
            {
                epsModifier += averageEPS * 2 + (4 * Constants.CORE_BONUS);
            }
            else if (5 < averageEPS)
            {
                epsModifier += Constants.HALF * averageEPS + (4 * Constants.CORE_BONUS);
            }

            // growthEPS score formulation
            // Reward cases
            if (0 < growthEPS && growthEPS <= 1)
            {
                epsModifier += growthEPS * 5 + Constants.CORE_BONUS;
            }
            else if (1 < growthEPS && growthEPS <= 3)
            {
                epsModifier += growthEPS * 3 + (2 * Constants.CORE_BONUS);
            }
            else if (3 < growthEPS)
            {
                epsModifier += growthEPS + (4 * Constants.CORE_BONUS);
            }
            /* Penalty cases
            else if (-5 <= growthEPS && growthEPS < 0)
            {
                epsModifier += growthEPS * 3 - 3;
            }
            else if (growthEPS < -5)
            {
                epsModifier += growthEPS * 3 - 6;
            }*/
            return Math.Min(epsModifier, Constants.FUND_EPS_MOD_UPPER_LIMIT);
        }

        public static decimal CalcPEModifier(decimal averagePE, decimal growthPE)
        {
            //If average PE significantly negative return penalty
            if (averagePE < -10)
            {
                return Constants.CORE_PENALTY;
            }
            //If everything is 0 return 0
            if (averagePE <= 0 && growthPE <= 0)
            {
                return 0;
            }
            decimal peModifier = 0;

            // averagePE score fomulation
            // Reward cases
            if (0 < averagePE && averagePE <= 25)
            {
                peModifier += (averagePE / 5) + (3 * Constants.CORE_BONUS);
            }
            else if (25 < averagePE && averagePE <= 50)
            {
                peModifier += (averagePE / 10) + Constants.CORE_BONUS;
            }
            else if (50 < averagePE && averagePE <= 100)
            {
                peModifier += (averagePE / 15);
            }
            // Penalty cases
            else if (averagePE > 100)
            {
                peModifier += (-1 * (averagePE / 15));
            }

            // growthPE score fomulation
            // Reward cases
            if (-50 <= growthPE && growthPE < 0)
            {
                peModifier += (-1 * (growthPE / 10));
            }
            else if (-100 <= growthPE && growthPE < -50)
            {
                peModifier += (-1 * (growthPE / 10)) + Constants.CORE_BONUS;
            }
            else if (-100 > growthPE)
            {
                peModifier += (-1 * (growthPE / 100)) + (Constants.CORE_BONUS * 3);
            }
            else if (0 < growthPE && growthPE <= 50)
            {
                peModifier += (-1 * (growthPE / 10));
            }
            // Penalty cases
            /*else if (50 < growthPE && growthPE <= 100)
            {
                peModifier += (-1 * (growthPE / 10)) - 3;
            }
            else if (100 < growthPE)
            {
                peModifier += (-1 * (growthPE / 100)) - 10;
            }*/
            peModifier = Math.Max(peModifier, Constants.FUND_PE_MOD_LOWER_LIMIT);
            return Math.Min(peModifier, Constants.FUND_PE_MOD_UPPER_LIMIT);
        }

        private static decimal CalcDividendBonus(decimal divRate, decimal divYield)
        {
            decimal divBonus = 0;
            bool hasDivs = divRate > 0;
            bool hasYield = divYield > 0;
            if (hasDivs)
            {
                //if div rate above 0, add 1
                if (divRate > 0)
                {
                    divBonus += 1;
                }
                //if div rate between 0 and 1.5, add divRate * 2 + 2
                if (0.5M < divRate && divRate <= 1.5M)
                {
                    divBonus += divRate * 2 + 1;
                }
                //else if div rate above 1.5, add divRate * 2 + bonus
                else if (divRate > 1.5M)
                {
                    divBonus += divRate * (Constants.CORE_BONUS - 1) + Constants.CORE_BONUS;
                }
            }
            if (hasYield)
            {
                //if div yield is between 0 and 5, add reduced bonus
                if (1 < divYield && divYield <= 5)
                {
                    divBonus += (Constants.HALF * divYield) + (Constants.CORE_BONUS / Constants.THREE);
                }
                //if div yield is between 5 and 10, add divYield + bonus
                else if (5 < divYield && divYield <= 10)
                {
                    divBonus += (Constants.HALF * divYield) + (Constants.CORE_BONUS / Constants.TWO);
                }
                //if div yield is above 3, add divYield + 5 bonus
                if (divYield >= 10)
                {
                    divBonus += (Constants.HALF * divYield) + Constants.CORE_BONUS;
                }
            }
            return Math.Min(divBonus, 20);
        }

        private static decimal CalcSmaModifier(PTHistory history)
        {
            decimal smaModifier = 0;

            // General bullish or bearish depending on SMA band conditions
            if (history.IsBullishSMA)
            {
                smaModifier += Constants.CORE_BONUS + Constants.THIRD;
            }
            else if (history.IsBearishSMA)
            {
                smaModifier += Constants.CORE_PENALTY - Constants.THIRD;
            }
            if (history.IsAboveSMABand)
            {
                smaModifier += Constants.CORE_BONUS + Constants.THIRD;
            }
            else if (history.IsBelowSMABand)
            {
                smaModifier += Constants.CORE_PENALTY - Constants.THIRD;
            }

            // Bonus if current price is close enough to 100d SMA for likely rebound
            var percentDiff = GetPercentDiff(history.AveragePrice100Day, history.TodayVwap);
            if (-7 <= Math.Abs(percentDiff) && Math.Abs(percentDiff) <= 7)
            {
                smaModifier += Constants.CORE_BONUS + (7 - Math.Abs(percentDiff));
            }

            // Bonus if 30d SMA is above 100d SMA
            if (history.AveragePrice30Day > history.AveragePrice100Day)
            {
                smaModifier += Constants.HALF;
            }

            // Bonus if current price is above 20d SMA
            if (history.TodayVwap > history.AveragePrice20Day + (history.AveragePrice20Day * .01M))
            {
                smaModifier += Constants.CORE_BONUS / 2;
            }
            return smaModifier;
        }

        private static decimal CalcNetExpenseRatioBonus(decimal netExpenseRatio)
        {
            if (netExpenseRatio <= 0) return 0;
            decimal nerBonus = 0;
            nerBonus += netExpenseRatio < 1 ? Constants.CORE_BONUS / Constants.TWO : 0;
            if (0 < netExpenseRatio && netExpenseRatio <= Constants.FUND_NER_MAJOR_LIMIT_PERCENT)
            {
                nerBonus += (1.0M / netExpenseRatio) * Constants.FUND_NER_INVERSE_MULTIPLIER_PERCENT + 1;
                nerBonus += netExpenseRatio <= Constants.FUND_NER_MINOR_LIMIT_PERCENT ?
                    Constants.CORE_BONUS / Constants.TWO : 0;
            }
            return nerBonus;
        }

        private static decimal CalcVolumeTrendingModifier(PTHistory history)
        {
            // TODO: bring golden path into this GRU helper function
            decimal volumeTrendingModifier = 0;

            // Add positive fractional bonus if today dollar volume is greater than 10d average dollar volume
            // TODO: try using 5d average dollar volume vs 15d average dollar volume
            decimal volUsdAnchorNew = (history.TodayVolUsd + history.AverageVolUsd10Day) / Constants.TWO;
            decimal volUsdAnchorOld = (history.AverageVolUsd10Day + history.AverageVolUsd30Day) / Constants.TWO;
            decimal percentChange = GetPercentDiff(volUsdAnchorOld, volUsdAnchorNew);
            if (1 < percentChange && percentChange <= 100)
            {
                volumeTrendingModifier += percentChange < 25 ? (percentChange / 5) + Constants.CORE_BONUS :
                    (percentChange / 20) + (Constants.CORE_BONUS * 2);
            }
            else if (100 < percentChange)
            {
                volumeTrendingModifier += Constants.CORE_BONUS * 2;
            }
            // Penalty cases (like inverse golden path)
            if (history.TodayVolUsd < history.AverageVolUsd10Day - Constants.FIFTY_THOUSAND &&
                history.AverageVolUsd10Day < history.AverageVolUsd30Day - Constants.FIFTY_THOUSAND)
            {
                volumeTrendingModifier += Constants.CORE_PENALTY * 2;
            }
            else if (-1 > percentChange && percentChange >= -100)
            {
                volumeTrendingModifier += percentChange > -25 ? (percentChange / 5) + Constants.CORE_PENALTY :
                    (percentChange / 20) + (Constants.CORE_PENALTY * 2);
            }
            else if (-100 > percentChange)
            {
                volumeTrendingModifier += Constants.CORE_PENALTY * 3;
            }
            return volumeTrendingModifier;
        }

        private static decimal CalcCustomFairValue(decimal tvwp, decimal fvp, decimal bvp, decimal ftlp, decimal ape, decimal epst, decimal fcs)
        {
            if (Constants.FUND_HANDICAP_MODE_ENABLED)
                return 0;

            decimal customFairValue = 0;
            decimal customFairValueCount = 0;
            bool shouldUseBookValue = fvp != 0 || bvp != 0;
            if (shouldUseBookValue)
            {
                if (fvp != 0)
                {
                    customFairValue += fvp;
                    customFairValueCount++;
                }
                if (bvp != 0)
                {
                    customFairValue += bvp;
                    customFairValueCount++;
                }
            }
            else
            {
                if (ftlp != 0)
                {
                    customFairValue += ftlp;
                    customFairValueCount++;
                }
                if (ape > 0)
                {
                    customFairValue = tvwp / (ape / Constants.FIVE);
                    customFairValue += epst > 0 ? customFairValue * Constants.THIRD + epst : 0;
                    customFairValueCount++;
                }
            }

            customFairValue = customFairValue / customFairValueCount;
            customFairValue += fcs >= 75 ? customFairValue * Constants.THIRD :
                fcs >= 60 ? customFairValue * Constants.FIFTH : customFairValue * 0.1M;

            return customFairValue;
        }

        private static decimal CalcTimeScaledBuySignalBonus(bool hasBuySignal, decimal bonus, int daysSinceSignal)
        {
            decimal timeScaledBonus = 0;
            if (hasBuySignal)
            {
                if (daysSinceSignal == 1 || daysSinceSignal == 2)
                {
                    timeScaledBonus = bonus * 8 + Constants.CORE_SIGNAL_MOD + 1;
                }
                else if (daysSinceSignal == 3)
                {
                    timeScaledBonus = bonus * 7 + Constants.CORE_SIGNAL_MOD + 1;
                }
                else if (daysSinceSignal == 4)
                {
                    timeScaledBonus = bonus * 6 + Constants.CORE_SIGNAL_MOD - 1;
                }
                else if (daysSinceSignal == 5)
                {
                    timeScaledBonus = bonus * 4 + Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 6)
                {
                    timeScaledBonus = bonus * 2 + 1;
                }
                else if (daysSinceSignal == 7)
                {
                    timeScaledBonus = bonus + 1;
                }
            }
            return timeScaledBonus;
        }

        private static decimal CalcTimeScaledSellSignalPenalty(bool hasSellSignal, decimal penalty, int daysSinceSignal)
        {
            decimal timeScaledPenalty = 0;
            if (hasSellSignal)
            {
                if (daysSinceSignal == 1 || daysSinceSignal == 2)
                {
                    timeScaledPenalty = penalty * 7 - Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 3)
                {
                    timeScaledPenalty = penalty * 6 - Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 4)
                {
                    timeScaledPenalty = penalty * 5 - Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 5)
                {
                    timeScaledPenalty = penalty * 3 - Constants.CORE_SIGNAL_MOD;
                }
                else if (daysSinceSignal == 6)
                {
                    timeScaledPenalty = penalty - 1;
                }
                else if (daysSinceSignal == 7)
                {
                    timeScaledPenalty = 0;
                }
            }
            return (-1 * timeScaledPenalty);
        }
    }
}
