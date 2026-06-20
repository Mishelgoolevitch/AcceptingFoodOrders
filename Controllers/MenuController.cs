using AcceptingFoodOrders.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcceptingFoodOrders.Controllers
{
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? categoryId)
        {
            
            var categories = _context.Categories.ToList();
            ViewBag.Categories = categories;

            var foodItems = _context.FoodItems
                .Include(f => f.Category)
                .Where(f => f.IsAvailable && (categoryId == null || f.CategoryId == categoryId))
                .ToList();

            return View(foodItems);
        }
    }
}
