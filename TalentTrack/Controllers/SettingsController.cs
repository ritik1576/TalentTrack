using Microsoft.AspNetCore.Mvc;

namespace TalentTrack.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role)) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New password and confirm password do not match.";
                return RedirectToAction("Index");
            }
            // In a real app, verify currentPassword and update in DB
            TempData["Success"] = "Password changed successfully! (Demo mode: no actual change)";
            return RedirectToAction("Index");
        }
    }
}
