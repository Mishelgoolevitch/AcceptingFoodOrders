using Microsoft.AspNetCore.Mvc;

namespace AcceptingFoodOrders.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
    }
}
