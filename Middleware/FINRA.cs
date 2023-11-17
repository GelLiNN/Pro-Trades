using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PT.Middleware;
using PT.Models;
using TinyCsvParser;
using TinyCsvParser.Mapping;
using TinyCsvParser.Model;
using TinyCsvParser.TypeConverter;

namespace PT.Middleware
{
    public static class FINRA
    {
        public static readonly DateTime FirstDate = new DateTime(2018, 11, 5);
        public static readonly string BaseUrl = @"https://cdn.finra.org/equity/regsho/daily";
        private static readonly HttpClient Client = new HttpClient();

        private static readonly decimal SlightlyBearishLowerBound = 0.0M;
        private static readonly decimal SlightlyBearishUpperBound = 0.25M;
        private static readonly decimal ModeratelyBearishLowerBound = 0.25M;
        private static readonly decimal ModeratelyBearishUpperBound = 0.5M;

        public static ShortInterestResult GetShortInterest(string symbol, IEnumerable<Skender.Stock.Indicators.Quote> history, int daysToCalculate)
        {
            decimal compositeScore = 0;

            List<FinraRecord> shortRecords = GetShortVolume(symbol, history, daysToCalculate);

            int daysCalulated = 0;
            int numberOfResults = 0;
            HashSet<string> dates = new HashSet<string>();
            Stack<decimal> shortInterestYList = new Stack<decimal>();

            decimal shortInterestToday = 0;
            decimal totalVolume = 0;
            decimal totalVolumeShort = 0;
            foreach (FinraRecord shortRecord in shortRecords)
            {
                if (daysCalulated < daysToCalculate)
                {
                    decimal shortVolume = shortRecord.ShortVolume + shortRecord.ShortExemptVolume;
                    decimal shortInterest = (shortVolume / shortRecord.TotalVolume) * 100;

                    if (numberOfResults == 0)
                        shortInterestToday = shortInterest;

                    shortInterestYList.Push(shortInterest);
                    totalVolume += shortRecord.TotalVolume;
                    totalVolumeShort += shortVolume;
                    numberOfResults++;

                    string shortDate = shortRecord.Date.ToString("yyyy-MM-dd");
                    if (!dates.Contains(shortDate))
                    {
                        dates.Add(shortDate);
                        daysCalulated++;
                    }
                }
                else
                    break;
            }

            List<decimal> shortXList = new List<decimal>();
            for (int i = 1; i <= numberOfResults; i++)
                shortXList.Add(i);

            List<decimal> shortYList = shortInterestYList.ToList();
            decimal shortSlope = shortYList.Count > 0 ? Indicators.GetSlope(shortXList, shortYList) : 0.0M; //set to 0 if not found
            decimal shortSlopeMultiplier = Indicators.GetSlopeMultiplier(shortSlope);
            decimal shortInterestAverage = totalVolume > 0 ? (totalVolumeShort / totalVolume) * 100 : 30.0M; //set to 30 if not found

            //Add these bonuses to account for normal short interest fluctuations
            //The slope cannot be in both ranges if the ranges do not overlap
            //This prevents adding both bonuses
            bool slightlyBearish = (SlightlyBearishLowerBound <= shortSlope && shortSlope <= SlightlyBearishUpperBound);
            bool moderatelyBearish = (ModeratelyBearishLowerBound <= shortSlope && shortSlope <= ModeratelyBearishUpperBound);

            //calculate composite score based on the following values and weighted multipliers
            compositeScore += 100 - shortInterestAverage; //get base score as 100 - short interest
            compositeScore += (shortSlope < 0) ? (shortSlope * shortSlopeMultiplier) + 10 : -15;
            compositeScore += (shortSlope > 0 && slightlyBearish) ? 10 : 0;
            compositeScore += (shortSlope > 0 && moderatelyBearish) ? 5 : 0;

            //Cap this compositeScore at 100 because we should not give it extra weight
            compositeScore = Math.Min(compositeScore, 100);

            //Return ShortInterestResult
            return new ShortInterestResult
            {
                TotalVolume = totalVolume,
                TotalVolumeShort = totalVolumeShort,
                ShortInterestPercentToday = shortInterestToday,
                ShortInterestPercentAverage = shortInterestAverage,
                ShortInterestSlope = shortSlope,
                ShortInterestCompositeScore = compositeScore
            };
        }

        public static List<FinraRecord> GetShortVolume(string symbol, IEnumerable<Skender.Stock.Indicators.Quote> history, int days)
        {
            List<FinraRecord> shortRecords = new List<FinraRecord>();

            //Get last 14 trading days for this symbol using TD
            //This way the FINRA short interest module completely relies on TD for dates
            //string ochlResponse = TwelveData.CompleteTwelveDataRequest("time_series", symbol).Result;
            //JObject data = JObject.Parse(ochlResponse);
            //JArray resultSet = (JArray)data.GetValue("values");

            List<Skender.Stock.Indicators.Quote> historyList = history.ToList();

            for (int i = 1; i <= days; i++)
            {
                try
                {
                    var ochlResult = historyList[historyList.Count - i];
                    decimal ochlResultVolume = Convert.ToDecimal(ochlResult.Volume);
                    DateTime date = ochlResult.Date;

                    if (DateTime.Compare(date, FirstDate) >= 0)
                    {
                        List<FinraRecord> allRecords = GetAllShortVolume(date).Result;
                        FinraRecord curDayRecord = allRecords.Where(x => x.Symbol == symbol).FirstOrDefault();

                        if (curDayRecord != null)
                        {
                            //Inject volume from TD since FINRA total volume CAN BE inaccurate
                            decimal curDayVolume = Math.Max(ochlResultVolume, curDayRecord.TotalVolume);
                            curDayRecord.TotalVolume = curDayVolume;
                            shortRecords.Add(curDayRecord);
                        }
                        else
                        {
                            //FINRA record was missing for this date, we should do something
                            Debug.WriteLine(string.Format("INFO: FINRA record missing for {0} on date {1}",
                                symbol, date.ToString("MM-dd-yyyy")));
                        }
                    }
                }
                catch (Exception e)
                {
                    //FINRA record parsing failed, we should do something
                    Debug.WriteLine(string.Format("EXCEPTION CAUGHT: FINRA record FAILED for {0} on day {1}",
                        symbol, i));
                    continue;
                }
            }
            return shortRecords;
        }

        public static async Task<List<FinraRecord>> GetAllShortVolume(DateTime date)
        {
            string dateString = date.ToString("yyyyMMdd");
            string fileName = $"CNMSshvol{dateString}.txt";
            string requestUrl = $"{BaseUrl}/{fileName}";

            var response = await Client.GetStringAsync(requestUrl);
            var finraResponse = FinraResponseParser.ParseResponse(response);

            return await Task.FromResult(finraResponse);
        }

        public class FinraResponseParser
        {
            public static int count = 0;
            public static List<FinraRecord> ParseResponse(string finraResponse)
            {
                CsvParserOptions csvParserOptions = new CsvParserOptions(true, '|');
                CsvPersonMapping mapping = new CsvPersonMapping();
                CsvParser<FinraRecord> csvParser = new CsvParser<FinraRecord>(csvParserOptions, mapping);

                CsvReaderOptions csvReaderOptions = new CsvReaderOptions(new string[] { "\r\n" });
                var result = csvParser.ReadFromFinraString(csvReaderOptions, finraResponse, 2);

                var a = result.ToList();
                return result.Select(r => r.Result).ToList();
            }

            private class CsvPersonMapping : CsvMapping<FinraRecord>
            {
                private static readonly DtConverter dtConverter = new DtConverter();
                public CsvPersonMapping()
                    : base()
                {
                    MapProperty(0, x => x.Date, dtConverter);
                    MapProperty(1, x => x.Symbol);
                    MapProperty(2, x => x.ShortVolume);
                    MapProperty(3, x => x.ShortExemptVolume);
                    MapProperty(4, x => x.TotalVolume);
                    MapProperty(5, x => x.Market);
                }
            }

            private class DtConverter : ITypeConverter<DateTime>
            {
                public Type TargetType => throw new NotImplementedException();
                private static readonly string validFormat = "yyyyMMdd";
                private static CultureInfo cultureInfo = new CultureInfo("en-US");

                public bool TryConvert(string value, out DateTime result)
                {
                    result = DateTime.ParseExact(value, validFormat, cultureInfo);
                    FinraResponseParser.count++;
                    return true;
                }
            }
        }
    }

    public static class CsvParserExtensions
    {
        public static ParallelQuery<CsvMappingResult<TEntity>> ReadFromFinraString<TEntity>(this CsvParser<TEntity> csvParser, CsvReaderOptions csvReaderOptions, string csvData, int skipLast)
        {
            var lines = csvData
                .Split(csvReaderOptions.NewLine, StringSplitOptions.None)
                .SkipLast(skipLast)
                .Select((line, index) => new Row(index, line));

            return csvParser.Parse(lines);
        }
    }
}
