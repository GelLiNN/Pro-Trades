using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sociosearch.NET.Middleware;
using Sociosearch.NET.Models;

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

        [HttpGet("/GetIndicatorForSymbol/{function}/{symbol}/{days}")] //indicator == function
        public IActionResult GetIndicatorForSymbol(string function, string symbol, string days)
        {
            int numOfDays = Int32.Parse(days);
            string avResponse = Utility.CompleteAlphaVantageRequest(function, symbol).Result;
            decimal avCompositeScore = Utility.GetCompositeScore(avResponse, function, numOfDays);
            return new ContentResult
            {
                StatusCode = 200,
                Content = "Success! Composite Score " + avCompositeScore
            };
        }

        [HttpGet("/GetCompositeScoreForSymbol/{symbol}")]
        public CompositeScoreResult GetCompositeScoreForSymbol(string symbol)
        {
            string adxResponse = Utility.CompleteAlphaVantageRequest("ADX", symbol).Result;
            decimal adxCompositeScore = Utility.GetCompositeScore("ADX", adxResponse, 5);
            string aroonResponse = Utility.CompleteAlphaVantageRequest("AROON", symbol).Result;
            decimal aroonCompositeScore = Utility.GetCompositeScore("AROON", aroonResponse, 20);

            return new CompositeScoreResult
            {
                ADXComposite = adxCompositeScore,
                AROONComposite = aroonCompositeScore,
                CompositeScore = (adxCompositeScore + aroonCompositeScore) / 2
            };
        }

        [HttpGet("/GetCompanyStatsForSymbol/{symbol}")]
        public CompanyStats GetCompanyStatsForSymbol(string symbol)
        {
            return IEX.GetCompanyStatsAsync(symbol).Result;
        }

        [HttpGet("/GetQuoteForSymbol/{symbol}")]
        public VSLee.IEXSharp.Model.Shared.Response.Quote GetQuoteForSymbol(string symbol)
        {
            return IEX.GetQuoteAsync(symbol).Result;
        }

        [HttpGet("/GetAllCompanies")]
        public AllCompanies GetAllCompanies()
        {
            return Companies.GetAllCompaniesAsync().Result;
        }

        [HttpGet("/Test")]
        public string Test()
        {
            return string.Empty;
        }
    }
}
