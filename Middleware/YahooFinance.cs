﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using PT.Models;
using YahooFinanceApi;
using YahooQuotesApi;

namespace PT.Middleware
{
    // Yahoo Finance API Documentation https://github.com/lppkarl/YahooFinanceApi
    // No API key needed for this one?
    // Other Yahoo Finance API Documentation https://github.com/dshe/YahooQuotesApi
    public class YahooFinance
    {
        // With YahooFinanceApi
        public static async Task<CompanyStatsYF> GetCompanyStatsAsync1(string symbol)
        {
            CompanyStatsYF companyStat = new CompanyStatsYF();
            try
            {
                //Yahoo Quote
                YahooQuotesApi.Security quote = GetQuoteAsync(symbol).Result;
                if (quote != null)
                {
                    //parse quote data into company stat
                    companyStat.Symbol = symbol;
                    companyStat.Exchange = quote.FullExchangeName;
                    companyStat.CompanyName = quote.LongName;
                    //companyStat.CompanyTrading = quote.Tradeable;

                    //companyStat.CompanyQuoteType = quote.QuoteType;
                    //companyStat.CompanyMarket = quote.Market;
                    //companyStat.MarketCap = quote.MarketCap;
                    //companyStat.SharesOutstanding = quote.SharesOutstanding;

                    decimal peTrailing = 0.0M;
                    try { peTrailing = decimal.Parse(quote.TrailingPE.ToString()); }
                    catch (Exception e) { /*do nothing*/ }

                    decimal peForward = 0.0M;
                    try { peForward = decimal.Parse(quote.ForwardPE.ToString()); }
                    catch (Exception e) { /*set to trailing*/ peForward = peTrailing; }

                    decimal epsTrailing = 0.0M;
                    try { epsTrailing = decimal.Parse(quote.EpsTrailingTwelveMonths.ToString()); }
                    catch (Exception e) { /*do nothing*/ }

                    decimal epsForward = 0.0M;
                    try { epsForward = decimal.Parse(quote.EpsForward.ToString()); }
                    catch (Exception e) { /*set to trailing*/ epsForward = epsTrailing; }

                    //companyStat.BookValue = decimal.Parse(quote.BookValue.ToString());

                    companyStat.Price = decimal.Parse(quote.RegularMarketPrice.ToString());

                    companyStat.PriceAverage50DayUSD = decimal.Parse(quote.FiftyDayAverage.ToString());
                    companyStat.PriceAverage200DayUSD = decimal.Parse(quote.TwoHundredDayAverage.ToString());

                    companyStat.PriceHigh52w = decimal.Parse(quote.FiftyTwoWeekHigh.ToString());
                    companyStat.PriceLow52w = decimal.Parse(quote.FiftyTwoWeekLow.ToString());
                    companyStat.PriceAverageEstimate52w = (companyStat.PriceHigh52w + companyStat.PriceLow52w) / 2;

                    //companyStat.PriceToBook = decimal.Parse(quote.PriceToBook.ToString());

                    companyStat.VolumeToday = decimal.Parse(quote.RegularMarketVolume.ToString());
                    companyStat.VolumeTodayUSD = (companyStat.VolumeToday * companyStat.Price);
                    companyStat.VolumeAverage10d = decimal.Parse(quote.AverageDailyVolume10Day.ToString());
                    companyStat.VolumeAverage10dUSD = (companyStat.VolumeAverage10d * companyStat.Price);
                    companyStat.VolumeAverage3m = decimal.Parse(quote.AverageDailyVolume3Month.ToString());
                    companyStat.VolumeAverage3mUSD = (companyStat.VolumeAverage3m * companyStat.Price);

                    /*Could get more trade data from IEX possibly
                    companyStat.TradeData = new TradeDataYF
                    {
                        BidPrice = decimal.Parse(quote.Bid.ToString()),
                        BidSize = decimal.Parse(quote.BidSize.ToString()),
                        AskPrice = decimal.Parse(quote.Ask.ToString()),
                        AskSize = decimal.Parse(quote.AskSize.ToString())
                    };*/
                }

                //Yahoo Dividends for the last year
                //You should be able to query data from various markets including US, HK, TW
                /*var dividends = await Yahoo.GetDividendsAsync(symbol, DateTime.Now.AddYears(-1), DateTime.Now);
                foreach (DividendTick div in dividends)
                    companyStat.Dividends.Add(div);*/

                //Yahoo historical trade data and splits data
                //var splits = await Yahoo.GetSplitsAsync(symbol, DateTime.Now.AddYears(-1), DateTime.Now);
                //foreach (var split in splits)
                //    companyStat.Splits.Add(split);

                //Composite Score, this gets the YF quote twice right now
                //TODO: Update this method to not get YF quote twice during cache loading
                //var score = Controllers.SearchController.GetCompositeScoreInternal(symbol, quote);
                //if (score.CompositeScoreValue > 0)
                //    companyStat.CompositeScoreResult = score;
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION CAUGHT: YF.cs YF.GetComanyStatsAsync for symbol " + symbol + ", message: " + e.Message + ", StackTrace: " + e.StackTrace);
            }
            return await Task.FromResult(companyStat);
        }

        // With YahooQuotesApi
        public static async Task<CompanyStatsYF> GetCompanyStatsAsync(string symbol)
        {
            CompanyStatsYF companyStat = new CompanyStatsYF();
            try
            {
                //Yahoo Quote
                YahooQuotesApi.Security quote = GetQuoteAsync(symbol).Result;
                if (quote != null)
                {
                    //parse quote data into company stat
                    companyStat.Symbol = symbol;
                    companyStat.Exchange = quote.FullExchangeName;
                    companyStat.CompanyName = quote.LongName;
                    companyStat.CompanyTrading = quote.Tradeable == null || quote.Tradeable == true ? true : false;

                    //companyStat.CompanyQuoteType = quote.QuoteType;
                    //companyStat.CompanyMarket = quote.Market;
                    //companyStat.MarketCap = quote.MarketCap;
                    //companyStat.SharesOutstanding = quote.SharesOutstanding;

                    // TODO: Think of a better way to parse these into decimal types
                    decimal peTrailing = 0.0M;
                    try { peTrailing = decimal.Parse(quote.TrailingPE.ToString()); }
                    catch (Exception e) { /*do nothing*/ }
                    companyStat.PeRatioTrailing = peTrailing;

                    decimal peForward = 0.0M;
                    try { peForward = decimal.Parse(quote.ForwardPE.ToString()); }
                    catch (Exception e) { /*set to trailing*/ peForward = peTrailing; }
                    companyStat.PeRatioForward = peForward;

                    decimal epsTrailing = 0.0M;
                    try { epsTrailing = decimal.Parse(quote.EpsTrailingTwelveMonths.ToString()); }
                    catch (Exception e) { /*do nothing*/ }
                    companyStat.EpsTrailing = epsTrailing;

                    decimal epsForward = 0.0M;
                    try { epsForward = decimal.Parse(quote.EpsForward.ToString()); }
                    catch (Exception e) { /*set to trailing*/ epsForward = epsTrailing; }
                    companyStat.EpsForward = epsForward;

                    //companyStat.BookValue = decimal.Parse(quote.BookValue.ToString());

                    companyStat.Price = decimal.Parse(quote.RegularMarketPrice.ToString());

                    companyStat.PriceAverage50DayUSD = decimal.Parse(quote.FiftyDayAverage.ToString());
                    companyStat.PriceAverage200DayUSD = decimal.Parse(quote.TwoHundredDayAverage.ToString());

                    companyStat.PriceHigh52w = decimal.Parse(quote.FiftyTwoWeekHigh.ToString());
                    companyStat.PriceLow52w = decimal.Parse(quote.FiftyTwoWeekLow.ToString());
                    companyStat.PriceAverageEstimate52w = (companyStat.PriceHigh52w + companyStat.PriceLow52w) / 2;

                    //companyStat.PriceToBook = decimal.Parse(quote.PriceToBook.ToString());

                    companyStat.VolumeToday = decimal.Parse(quote.RegularMarketVolume.ToString());
                    companyStat.VolumeTodayUSD = (companyStat.VolumeToday * companyStat.Price);
                    companyStat.VolumeAverage10d = decimal.Parse(quote.AverageDailyVolume10Day.ToString());
                    companyStat.VolumeAverage10dUSD = (companyStat.VolumeAverage10d * companyStat.Price);
                    companyStat.VolumeAverage3m = decimal.Parse(quote.AverageDailyVolume3Month.ToString());
                    companyStat.VolumeAverage3mUSD = (companyStat.VolumeAverage3m * companyStat.Price);

                    // Could get more trade data from IEX possibly
                    companyStat.TradeData = new TradeDataYF
                    {
                        BidPrice = decimal.Parse(quote.Bid.ToString()),
                        BidSize = decimal.Parse(quote.BidSize.ToString()),
                        AskPrice = decimal.Parse(quote.Ask.ToString()),
                        AskSize = decimal.Parse(quote.AskSize.ToString())
                    };
                }

                //Yahoo Dividends for the last year
                //You should be able to query data from various markets including US, HK, TW

                /*var dividends = await Yahoo.GetDividendsAsync(symbol, DateTime.Now.AddYears(-1), DateTime.Now);
                foreach (DividendTick div in dividends)
                    companyStat.Dividends.Add(div);*/

                //Yahoo historical trade data and splits data
                /*var splits = await Yahoo.GetSplitsAsync(symbol, DateTime.Now.AddYears(-1), DateTime.Now);
                foreach (var split in splits)
                    companyStat.Splits.Add(split);*/

                //Composite Score, this gets the YF quote twice right now
                //TODO: Update this method to not get YF quote twice during cache loading
                var score = Controllers.SearchController.GetCompositeScoreInternal(symbol, quote);
                if (score.CompositeScoreValue > 0)
                    companyStat.CompositeScoreResult = score;
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION CAUGHT: YF.cs YF.GetComanyStatsAsync for symbol " + symbol + ", message: " + e.Message + ", StackTrace: " + e.StackTrace);
            }
            return await Task.FromResult(companyStat);
        }

        // With YahooFinanceApi
        public static async Task<YahooFinanceApi.Security> GetQuoteAsync1(string symbol)
        {
            // You could query multiple symbols with multiple fields through the following steps:
            var securities = await Yahoo.Symbols(symbol).QueryAsync();
            var quote = securities[symbol];
            return quote;
        }

        // With YahooQuotesApi
        public static async Task<YahooQuotesApi.Security> GetQuoteAsync(string symbol)
        {
            // You could query multiple symbols with multiple fields through the following steps:
            YahooQuotes yahooQuotes = new YahooQuotesBuilder().Build();

            Dictionary<string, YahooQuotesApi.Security?> securities = await yahooQuotes.GetAsync(new[] { symbol });

            YahooQuotesApi.Security security = securities[symbol] ?? throw new ArgumentException("Unknown symbol");
            return security;
        }

        // With YahooFinanceApi
        public static async Task<IReadOnlyList<YahooFinanceApi.Candle>> GetHistoryAsync1(string symbol, int days)
        {
            // You should be able to query data from various markets including US, HK, TW
            // The startTime & endTime here defaults to EST timezone
            //var history = await Yahoo.GetHistoricalAsync(symbol, DateTime.Now.AddDays(-1 * (days + 1)), DateTime.Now, Period.Daily);
            return null;//history;
        }

        // With YahooQuotesApi
        public static async Task<List<PriceTick>> GetHistoryAsync(string symbol, int days)
        {
            // You should be able to query data from various markets including US, HK, TW
            // The timezone here may or may not impact accuracy
            days *= -1;
            var timeZone = DateTimeZoneProviders.Bcl.GetSystemDefault();
            var localTime = LocalDateTime.FromDateTime(DateTime.Now.AddDays(days));
            var zonedTimeInstant = localTime.InZoneStrictly(timeZone).ToInstant();

            YahooQuotes yahooQuotes = new YahooQuotesBuilder()
                .WithHistoryStartDate(zonedTimeInstant)
                .Build();

            YahooQuotesApi.Security security = await yahooQuotes.GetAsync(symbol, Histories.PriceHistory)
                ?? throw new ArgumentException("Unknown symbol.");

            var history = security.PriceHistory.Value.ToList();
            return history;
        }

        public static async Task<CompaniesListYF> GetScreenedCompaniesAsync(CompaniesListYF allCompanies, string screenId)
        {
            CompaniesListYF screened = new CompaniesListYF
            {
                SymbolsToCompanies = new Dictionary<string, CompanyYF>()
            };

            foreach (var company in allCompanies.SymbolsToCompanies)
            {
                string symbol = company.Key;
                CompanyYF companyObject = company.Value;
                CompanyStatsYF stats = companyObject.Stats;
                if (stats.Price < 20 && stats.Price > .01M && stats.VolumeAverage10dUSD > 1000000
                    /*&& !String.IsNullOrEmpty(stats.Earnings.EPSReportDate)*/)
                {
                    screened.SymbolsToCompanies.Add(symbol, companyObject);
                }
            }
            return await Task.FromResult(screened);
        }
    }
}
