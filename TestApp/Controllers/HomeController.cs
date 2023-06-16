using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TestApp.Models;
using TestApp.Proxy;

namespace TestApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly TestServiceProxy testService;

        public HomeController(TestServiceProxy testService)
        {
            this.testService = testService;
        }
       /* public HomeController()
        {
                
        }*/
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Test()
        {
        
            ViewData["Message"] = $"Hello fellow {User.FindFirst("emails").Value}!";
            var forecast = await testService.GetWeatherForecastAsync();
             return View(forecast);
             //return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
