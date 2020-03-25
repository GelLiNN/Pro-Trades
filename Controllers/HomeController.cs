using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sociosearch.NET.Middleware;
using Sociosearch.NET.Models;

namespace Sociosearch.NET.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/")]
        [HttpGet("/Home")]
        public IActionResult Home()
        {
            return View("Home");
        }

        [HttpGet("/Privacy")]
        public IActionResult Privacy()
        {
            return View("Privacy");
        }

        [HttpGet("/TestEmail")]
        public IActionResult TestEmail()
        {
            EmailTest.Send();
            return new ContentResult
            {
                StatusCode = 200,
                Content = "Success!"
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
