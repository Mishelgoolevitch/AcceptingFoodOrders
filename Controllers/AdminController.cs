using AcceptingFoodOrders.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcceptingFoodOrders.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("IsAdmin") == "True";
        }

        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalItems = _context.FoodItems.Count();
            return View();
        }

        public IActionResult ManageMenu(int? categoryId, string searchString, bool? isAvailable, decimal? minPrice, decimal? maxPrice, string sortOrder)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            //Сохраняйте порядок сортировки для просмотра
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParam = sortOrder == "name_desc" ? "name" : "name_desc";
            ViewBag.PriceSortParam = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            ViewBag.CategorySortParam = sortOrder == "category" ? "category_desc" : "category";

            var items = _context.FoodItems
                .Include(f => f.Category)
                .AsQueryable();

            // Фильтровать по категориям
            if (categoryId.HasValue && categoryId > 0)
            {
                items = items.Where(f => f.CategoryId == categoryId);
                ViewBag.CurrentCategory = categoryId;
            }

            // Фильтровать по строке поиска (названию или описанию)
            if (!string.IsNullOrEmpty(searchString))
            {
                items = items.Where(f =>
                    f.Name.Contains(searchString) ||
                    f.Description.Contains(searchString));
                ViewBag.CurrentSearch = searchString;
            }

            // Фильтровать по доступности
            if (isAvailable.HasValue)
            {
                items = items.Where(f => f.IsAvailable == isAvailable.Value);
                ViewBag.CurrentAvailability = isAvailable.Value;
            }

            // Фильтровать по ценовому диапазону
            if (minPrice.HasValue)
            {
                items = items.Where(f => f.Price >= minPrice.Value);
                ViewBag.MinPrice = minPrice.Value;
            }
            if (maxPrice.HasValue)
            {
                items = items.Where(f => f.Price <= maxPrice.Value);
                ViewBag.MaxPrice = maxPrice.Value;
            }

            // Сортировка
            items = sortOrder switch
            {
                "name_asc" => items.OrderBy(f => f.Name),
                "name_desc" => items.OrderByDescending(f => f.Name),
                "price_asc" => items.OrderBy(f => f.Price),
                "price_desc" => items.OrderByDescending(f => f.Price),
                "category" => items.OrderBy(f => f.Category.Name),
                "category_desc" => items.OrderByDescending(f => f.Category.Name),
                _ => items.OrderBy(f => f.CategoryId).ThenBy(f => f.Name)
            };

            // Статистика для панели мониторинга
            ViewBag.TotalItems = _context.FoodItems.Count();
            ViewBag.AvailableItems = _context.FoodItems.Count(f => f.IsAvailable);
            ViewBag.UnavailableItems = _context.FoodItems.Count(f => !f.IsAvailable);
            ViewBag.CategoryCount = _context.Categories.Count();

            ViewBag.Categories = _context.Categories.ToList();

            return View(items.ToList());
        }

        [HttpPost]
        public IActionResult ToggleAvailability(int id)
        {
            // Проверка безопасности: Только администраторы могут переключать доступность
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var item = _context.FoodItems.Find(id);
            if (item == null) return NotFound();

            // Логика изменения статуса доступности
            item.IsAvailable = !item.IsAvailable;
            _context.SaveChanges();

            // Установить уведомление для пользователя
            TempData["Success"] = $"{item.Name} is now {(item.IsAvailable ? "доступно" : "недоступно")}";

            return RedirectToAction("ManageMenu");
        }
    }
}
