using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Services.Interfaces;
using OrderManagementSystem.Models.ViewModels;
using OrderManagementSystem.Helpers;

namespace OrderManagementSystem.Controllers
{
    public class UsersController : Controller
    {
        private readonly IAuthService _authService;

        public UsersController(IAuthService authService)
        {
            _authService = authService;
        }

        [Authorize("admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _authService.GetAllUsers();
            return View(users);
        }

        [Authorize("admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize("admin")]
        [HttpPost]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.Register(model);

            if (result)
            {
                TempData["Success"] = "User created successfully!";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Email already exists";
            return View(model);
        }

        [Authorize("admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateRole(Guid id, string role)
        {
            var result = await _authService.UpdateUserRole(id, role);

            if (result)
                return Json(new { success = true, message = "Role updated successfully" });

            return Json(new { success = false, message = "Failed to update role" });
        }

        [Authorize("admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var result = await _authService.ToggleUserStatus(id);

            if (result)
                return Json(new { success = true, message = "User status updated successfully" });

            return Json(new { success = false, message = "Failed to update status" });
        }
    }
}