using Microsoft.AspNetCore.Mvc;

namespace TalentTrack.Controllers
{
    public class HelpController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role)) return RedirectToAction("Login", "Account");
            if (role == "Candidate") return RedirectToAction("Portal", "CandidateDocument");
            return View();
        }
    }
}
