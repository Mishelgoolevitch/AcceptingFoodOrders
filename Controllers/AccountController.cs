using AcceptingFoodOrders.Models;
using AcceptingFoodOrders.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcceptingFoodOrders.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = model.Password, 
                    FullName = model.FullName,
                    Address = model.Address,
                    Phone = model.Phone
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            var user = _context.Users.FirstOrDefault(u =>
                u.Username == model.Username && u.Password == model.Password);

            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Неверное имя пользователя или пароль");
            return View(model);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = _context.Users.Find(userId);
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Address = user.Address,
                Phone = user.Phone
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Profile(ProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            if (ModelState.IsValid)
            {
                var user = _context.Users.Find(userId);
                if (user == null) return NotFound();

                var emailExists = _context.Users.Any(u => u.Email == model.Email && u.Id != userId);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email is already registered to another account.");
                    return View(model);
                }

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Phone = model.Phone;
                user.Address = model.Address;

                _context.SaveChanges();
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult DeleteAccount()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = _context.Users.Find(userId);
            if (user == null) return NotFound();

            // Необязательно: Проверьте, есть ли у пользователя отложенные ордера
            var pendingOrders = _context.Orders.Any(o => o.UserId == userId &&
                (o.Status == "Pending" || o.Status == "Confirmed" || o.Status == "Preparing"));

            if (pendingOrders)
            {
                TempData["Error"] = "Невозможно удалить учетную запись, пока у вас есть активные заказы!";
                return RedirectToAction("Profile");
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            HttpContext.Session.Clear();
            TempData["Success"] = "Ваша учетная запись была удалена.";
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index","Home");
        }
    }
}
