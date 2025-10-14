using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OrderManagementSystem.Helpers
{
    public class AuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[]? _roles;

        public AuthorizeAttribute(params string[] roles)
        {
            _roles = roles.Length > 0 ? roles : null;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            if (_roles != null && _roles.Length > 0)
            {
                var userRole = context.HttpContext.Session.GetString("UserRole");
                if (string.IsNullOrEmpty(userRole) || !_roles.Contains(userRole))
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}