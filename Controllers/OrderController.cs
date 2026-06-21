using AcceptingFoodOrders.Models;
using Microsoft.AspNetCore.Mvc;
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
            return RedirectToAction("#");
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

        public IActionResult Cart()
        {
            var cart = GetCart();
            ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
            return View(cart);
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
