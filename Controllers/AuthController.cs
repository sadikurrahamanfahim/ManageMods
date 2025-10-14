using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Models.ViewModels;
using OrderManagementSystem.Services.Interfaces;

namespace OrderManagementSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to dashboard
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _authService.Login(model.Email, model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            // Set session
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserEmail", user.Email);

            TempData["Success"] = $"Welcome back, {user.FullName}!";
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Register()
        {
            // Only admin can register new users
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin")
                return RedirectToAction("AccessDenied");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin")
                return RedirectToAction("AccessDenied");

            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.Register(model);

            if (!result)
            {
                ModelState.AddModelError("", "Email already exists");
                return View(model);
            }

            TempData["Success"] = "User registered successfully";
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out successfully";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}