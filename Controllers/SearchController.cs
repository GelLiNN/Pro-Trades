using System;
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
            decimal avCompositeScore = AV.GetCompositeScore(avResponse, function, numOfDays);
            return new ContentResult
            {
                StatusCode = 200,
                Content = "Success! Composite Score " + avCompositeScore
            };
        }

        [HttpGet("/GetCompositeScoreForSymbol/{symbol}")]
        public CompositeScoreResult GetCompositeScoreForSymbol(string symbol)
        {
            string adxResponse = AV.CompleteAlphaVantageRequest("ADX", symbol).Result;
            decimal adxCompositeScore = AV.GetCompositeScore("ADX", adxResponse, 5);
            string aroonResponse = AV.CompleteAlphaVantageRequest("AROON", symbol).Result;
            decimal aroonCompositeScore = AV.GetCompositeScore("AROON", aroonResponse, 20);

            return new CompositeScoreResult
            {
                ADXComposite = adxCompositeScore,
                AROONComposite = aroonCompositeScore,
                CompositeScore = (adxCompositeScore + aroonCompositeScore) / 2
            };
        }

        /*
         * IEX dependent endpoints
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
         * FMP dependent endpoints
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
         * Yahoo dependent endpoints
         */
        [HttpGet("/GetCompanyStatsYahoo/{symbol}")]
        public CompanyStatsYahoo GetCompanyStatsYahoo(string symbol)
        {
            return YF.GetCompanyStatsAsync(symbol).Result;
        }

        [HttpGet("/GetQuoteYahoo/{symbol}")]
        public Security GetQuoteYahoo(string symbol)
        {
            return YF.GetQuoteAsync(symbol).Result;
        }

        [HttpGet("/GetAllCompaniesYahoo")]
        public CompaniesListYahoo GetAllCompaniesYahoo()
        {
            return YF.GetAllCompaniesAsync().Result;
        }

        [HttpGet("/GetScreenedCompaniesYahoo/{screenId}")]
        public CompaniesListYahoo GetScreenedCompaniesYahoo(string screenId)
        {
            CompaniesListYahoo companies = YF.GetAllCompaniesAsync().Result;
            return YF.GetScreenedCompaniesAsync(companies, screenId).Result;
        }

        /*
         * Other endpoints
         */
        [HttpGet("/Test")]
        public string Test()
        {
            return string.Empty;
        }
    }
}
