using AcceptingFoodOrders.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AcceptingFoodOrders.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext context;

        public HomeController(ApplicationDbContext _context)
        {
            context = _context;
        }

        public IActionResult Index()
        {
            var footitems = context.FoodItems
                .Where(f => f.IsAvailable)
                .Take(6)
                .ToList();

            return View(footitems);
        }
    }
}
