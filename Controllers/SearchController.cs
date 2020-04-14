﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sociosearch.NET.Middleware;
using Sociosearch.NET.Models;
using YahooFinanceApi;

namespace Sociosearch.NET.Controllers
{
    public class SearchController : Controller
    {
        private readonly ILogger<SearchController> _logger;

        public SearchController(ILogger<SearchController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/Search")]
        public IActionResult Search()
        {
            return View("Search");
        }

        /*
         * Composite Score Endpoints
         */
        [HttpGet("/GetIndicatorForSymbol/{function}/{symbol}/{days}")] //indicator == function
        public IActionResult GetIndicatorForSymbol(string function, string symbol, string days)
        {
            int numOfDays = Int32.Parse(days);
            string avResponse = AV.CompleteAlphaVantageRequest(function, symbol).Result;
            decimal avCompositeScore = AV.GetCompositeScore(function, avResponse, numOfDays);
            return new ContentResult
            {
                StatusCode = 200,
                Content = "Success! Composite Score for function " + function + ": " + avCompositeScore
            };
        }

        [HttpGet("/GetCompositeScoreForSymbol/{symbol}")]
        public CompositeScoreResult GetCompositeScoreForSymbol(string symbol)
        {
            string adxResponse = AV.CompleteAlphaVantageRequest("ADX", symbol).Result;
            decimal adxCompositeScore = AV.GetCompositeScore("ADX", adxResponse, 7);
            string aroonResponse = AV.CompleteAlphaVantageRequest("AROON", symbol).Result;
            decimal aroonCompositeScore = AV.GetCompositeScore("AROON", aroonResponse, 14);
            string macdResponse = AV.CompleteAlphaVantageRequest("MACD", symbol).Result;
            decimal macdCompositeScore = AV.GetCompositeScore("MACD", macdResponse, 7);

            //QUANDL calls slightly different due to QUANDL Codes
            string shortResponse = string.Empty;
            foreach (string code in Q.FinraCodes)
            {
                shortResponse = Q.CompleteQuandlRequest("SHORT", String.Format("FINRA/{0}_{1}", code, symbol)).Result;
                if (!String.IsNullOrEmpty(shortResponse))
                    break;
            }
            ShortInterestResult shortResult = Q.GetShortInterest(shortResponse, 7);

            return new CompositeScoreResult
            {
                Symbol = symbol,
                ADXComposite = adxCompositeScore,
                AROONComposite = aroonCompositeScore,
                MACDComposite = macdCompositeScore,
                CompositeScore = (adxCompositeScore + aroonCompositeScore + macdCompositeScore + shortResult.ShortInterestCompositeScore) / 4,
                ShortInterest = shortResult
            };
        }

        /*
         * Investors Exchange dependent endpoints
         */
        [HttpGet("/GetCompanyStatsIEX/{symbol}")]
        public CompanyStatsIEX GetCompanyStatsIEX(string symbol)
        {
            return IEX.GetCompanyStatsAsync(symbol).Result;
        }

        [HttpGet("/GetQuoteIEX/{symbol}")]
        public VSLee.IEXSharp.Model.Shared.Response.Quote GetQuoteIEX(string symbol)
        {
            return IEX.GetQuoteAsync(symbol).Result;
        }

        [HttpGet("/GetAllCompaniesIEX")]
        public CompaniesListIEX GetAllCompaniesIEX()
        {
            return IEX.GetAllCompaniesAsync().Result;
        }

        [HttpGet("/GetScreenedCompaniesIEX/{screenId}")]
        public CompaniesListIEX GetScreenedCompaniesIEX(string screenId)
        {
            CompaniesListIEX companies = IEX.GetAllCompaniesAsync().Result;
            return IEX.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

        /*
         * Financial Modeling Prep dependent endpoints
         */
        [HttpGet("/GetCompanyStatsFMP/{symbol}")]
        public CompanyStatsFMP GetCompanyStatsFMP(string symbol)
        {
            return FMP.GetCompanyStatsAsync(symbol).Result;
        }

        [HttpGet("/GetQuoteFMP/{symbol}")]
        public string GetQuoteFMP(string symbol)
        {
            return FMP.GetQuote(symbol);
        }

        [HttpGet("/GetAllCompaniesFMP")]
        public CompaniesListFMP GetAllCompaniesFMP()
        {
            return FMP.GetAllCompaniesAsync().Result;
        }

        [HttpGet("/GetScreenedCompaniesFMP/{screenId}")]
        public CompaniesListFMP GetScreenedCompaniesFMP(string screenId)
        {
            CompaniesListFMP companies = FMP.GetAllCompaniesAsync().Result;
            return FMP.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

        /*
         * Yahoo Finance dependent endpoints
         */
        [HttpGet("/GetCompanyStatsYF/{symbol}")]
        public CompanyStatsYF GetCompanyStatsYF(string symbol)
        {
            return YF.GetCompanyStatsAsync(symbol).Result;
        }

        [HttpGet("/GetQuoteYF/{symbol}")]
        public Security GetQuoteYahoo(string symbol)
        {
            return YF.GetQuoteAsync(symbol).Result;
        }

        [HttpGet("/GetAllCompaniesYF")]
        public CompaniesListYF GetAllCompaniesYF()
        {
            return YF.GetAllCompaniesAsync().Result;
        }

        [HttpGet("/GetScreenedCompaniesYF/{screenId}")]
        public CompaniesListYF GetScreenedCompaniesYahoo(string screenId)
        {
            CompaniesListYF companies = YF.GetAllCompaniesAsync().Result;
            return YF.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

        /*
         * Other endpoints
         */
        [HttpGet("/Test")]
        public string Test()
        {
            //Stuff from my codillity assessment for Sameksha
            //Job links

            /*int[] A = new int[5] { 1, 2, 3, 4, 5 };
            int[] sortA = new int[5];
            int len = A.Length;
            A.CopyTo(sortA, 0);
            Array.Sort(sortA);
            Console.WriteLine();

            HashSet<int> ints = new HashSet<int>();
            foreach (int val in A)
            {
                if (!ints.Contains(val))
                    ints.Add(val);
            }*/

            string s = "abc";
            char[] chars = s.ToCharArray();
            List<string> combos = new List<string>();
            for (int i = 0; i < chars.Length; i++)
            {
                char toRemove = chars[i];
                string combo = string.Empty;

                for (int y = 0; y < chars.Length; i++)
                {
                    char current = chars[y];
                    if (current != toRemove)
                    {
                        combo += current;
                    }
                }
                Console.WriteLine(combo);
                combos.Add(combo);
            }
            string[] combosArray = combos.ToArray();
            Array.Sort(combosArray);
            return String.Join(",", combosArray);


            //Stuff from 2-4 Interview for Jasleen / Collabera
            //Interviewees Veronica and Brad


            //return string.Empty;
        }
    }
}
