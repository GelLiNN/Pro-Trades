using System;
using System.Data;
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

        [HttpGet("/GetIndicatorForSymbol/{function}/{symbol}")] //indicator == function
        public IActionResult GetIndicatorForSymbol(string function, string symbol)
        {
            string avResponse = Common.CompleteAlphaVantageRequest(function, symbol).Result;
            decimal avCompositeScore = Common.GetCompositeScore(avResponse, function, 5);
            return new ContentResult
            {
                StatusCode = 200,
                Content = "Success! Composite Score " + avCompositeScore
            };
        }

        [HttpGet("/GetCompositeScoreForSymbol/{symbol}")]
        public CompositeScoreResult GetCompositeScoreForSymbol(string symbol)
        {
            string adxResponse = Common.CompleteAlphaVantageRequest("ADX", symbol).Result;
            decimal adxCompositeScore = Common.GetCompositeScore("ADX", adxResponse, 5);
            string aroonResponse = Common.CompleteAlphaVantageRequest("AROON", symbol).Result;
            decimal aroonCompositeScore = Common.GetCompositeScore("AROON", aroonResponse, 20);
            return new CompositeScoreResult
            {
                ADXComposite = adxCompositeScore,
                AROONComposite = aroonCompositeScore,
                CompositeScore = (adxCompositeScore + aroonCompositeScore) / 2
            };
        }
    }
}
