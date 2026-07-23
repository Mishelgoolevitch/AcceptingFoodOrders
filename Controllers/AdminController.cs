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

        [HttpGet]
        public IActionResult AddFoodItem()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult AddFoodItem(FoodItem item)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            _context.FoodItems.Add(item);
            _context.SaveChanges();
            return RedirectToAction("ManageMenu");
        }

        [HttpGet]
        public IActionResult EditFoodItem(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var item = _context.FoodItems.Find(id);
            if (item == null) return NotFound();

            ViewBag.Categories = _context.Categories.ToList();
            return View(item);
        }

        [HttpPost]
        public IActionResult EditFoodItem([Bind("Id,Name,Description,Price,CategoryId,ImageUrl,IsAvailable")] FoodItem item)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

          
                var existingItem = _context.FoodItems.Find(item.Id);
                if (existingItem == null) return NotFound();

                // Обновляйте только те поля, которые нам нужны
                existingItem.Name = item.Name;
                existingItem.Description = item.Description;
                existingItem.Price = item.Price;
                existingItem.CategoryId = item.CategoryId;
                existingItem.ImageUrl = item.ImageUrl;
                existingItem.IsAvailable = item.IsAvailable;

                _context.SaveChanges();
                TempData["Success"] = $"{item.Name} был успешно обновлен!";
                return RedirectToAction("ManageMenu");
            

            ViewBag.Categories = _context.Categories.ToList();
            return View(item);
        }

        public IActionResult Orders(string status, string searchString, DateTime? fromDate, DateTime? toDate, string sortOrder)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // Сохранять текущий порядок сортировки для просмотра
            ViewBag.CurrentSort = sortOrder;
            ViewBag.DateSortParam = sortOrder == "date_asc" ? "date_desc" : "date_asc";
            ViewBag.TotalSortParam = sortOrder == "total_asc" ? "total_desc" : "total_asc";
            ViewBag.StatusSortParam = sortOrder == "status" ? "status_desc" : "status";

            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .AsQueryable();

            // Фильтровать по статусу
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                orders = orders.Where(o => o.Status == status);
                ViewBag.CurrentStatus = status;
            }

            // Фильтровать по имени клиента
            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o =>
                    o.User.FullName.Contains(searchString) ||
                    o.User.Username.Contains(searchString) ||
                    o.User.Email.Contains(searchString));
                ViewBag.CurrentSearch = searchString;
            }

            // Фильтровать по диапазону дат
            if (fromDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= fromDate.Value);
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }
            if (toDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= toDate.Value.AddDays(1));
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }

            // Сортировка
            orders = sortOrder switch
            {
                "date_asc" => orders.OrderBy(o => o.OrderDate),
                "date_desc" => orders.OrderByDescending(o => o.OrderDate),
                "total_asc" => orders.OrderBy(o => o.TotalAmount),
                "total_desc" => orders.OrderByDescending(o => o.TotalAmount),
                "status" => orders.OrderBy(o => o.Status),
                "status_desc" => orders.OrderByDescending(o => o.Status),
                _ => orders.OrderByDescending(o => o.OrderDate) 
            };

            // Статус учитывается в статистике панели мониторинга
            ViewBag.PendingCount = _context.Orders.Count(o => o.Status == "Pending");
            ViewBag.ConfirmedCount = _context.Orders.Count(o => o.Status == "Confirmed");
            ViewBag.TodayCount = _context.Orders.Count(o => o.OrderDate.Date == DateTime.Today);
            ViewBag.TotalRevenue = _context.Orders.Where(o => o.Status != "Cancelled").Sum(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.StatusList = new List<string> { "Все", "Ожидаемый", "Подтвержденный", "Подготовка", "Срочная доставка", "Доставлен", "Отмененный" };

            return View(orders.ToList());
        }
    }
}
