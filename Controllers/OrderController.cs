using AcceptingFoodOrders.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AcceptingFoodOrders.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult AddToCart(int foodItemId, int quantity = 1)
        {
            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var foodItem = _context.FoodItems.Find(foodItemId);
                cart.Add(new CartItem
                {
                    FoodItemId = foodItemId,
                    Name = foodItem.Name,
                    Price = foodItem.Price,
                    Quantity = quantity,
                    ImageUrl = foodItem.ImageUrl
                });
            }

            SaveCart(cart);
            return RedirectToAction("Cart");
        }
        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return cartJson == null ? new List<CartItem>() :
                JsonConvert.DeserializeObject<List<CartItem>>(cartJson);
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
        }

        public IActionResult RemoveFromCart(int foodItemId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction("Cart");
        }

        public IActionResult Cart()
        {
            var cart = GetCart();
            ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
            return View(cart);
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int foodItemId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

            if (item != null && quantity > 0)
            {
                item.Quantity = quantity;
                SaveCart(cart);
            }

            return RedirectToAction("Cart");
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index", "Menu");

            ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
            return View();
        }

        [HttpPost]
        public IActionResult Checkout(string deliveryAddress, string phoneNumber)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var cart = GetCart();

            var order = new Order
            {
                UserId = userId.Value,
                DeliveryAddress = deliveryAddress,
                PhoneNumber = phoneNumber,
                TotalAmount = cart.Sum(c => c.Price * c.Quantity),
                OrderItems = cart.Select(c => new OrderItem
                {
                    FoodItemId = c.FoodItemId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            HttpContext.Session.Remove("Cart");
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
        }

        public IActionResult OrderConfirmation(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        public IActionResult MyOrders(string status, string sortOrder, DateTime? fromDate, DateTime? toDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .Where(o => o.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                orders = orders.Where(o => o.Status == status);
                ViewBag.CurrentStatus = status;
            }

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

            ViewBag.CurrentSort = sortOrder;
            orders = sortOrder switch
            {
                "date_asc" => orders.OrderBy(o => o.OrderDate),
                "total_desc" => orders.OrderByDescending(o => o.TotalAmount),
                "total_asc" => orders.OrderBy(o => o.TotalAmount),
                _ => orders.OrderByDescending(o => o.OrderDate)
            };

            ViewBag.StatusList = new List<string> { "All", "Pending", "Confirmed", "Preparing", "OutForDelivery", "Delivered", "Cancelled" };


            return View(orders.ToList());
        }

        public IActionResult TrackOrder(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .FirstOrDefault(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();

            return View(order);
        }


    }
    public class CartItem
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
    }

}
