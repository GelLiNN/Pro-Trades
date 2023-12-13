using Microsoft.AspNetCore.Mvc;
using PT.Services;

namespace PT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [Route("api/swagger"), HttpGet]
        [ApiExplorerSettings(IgnoreApi = true)]
        public RedirectResult RedirectToSwaggerUi()
        {
            return Redirect("/swagger/");
        }

        [HttpGet("api/TestEmail")]
        public IActionResult TestEmail()
        {
            EmailTest.Send();
            return new ContentResult
            {
                StatusCode = 200,
                Content = "Success!"
            };
        }

        /*
        * Other endpoints
        */
        [HttpGet("api/Test")]
        public IActionResult Test()
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

            return new ContentResult
            {
                StatusCode = 200,
                Content = String.Join(",", combosArray)
            };
        }
    }
}
