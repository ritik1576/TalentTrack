using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentTrack.Data;
using TalentTrack.Models;

namespace TalentTrack.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin") return RedirectToAction("Login", "Account");

            var pendingUsers = _context.Recruiters
                .Where(u => u.Status == "Pending" || !u.IsApproved)
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            var approvedUsers = _context.Recruiters
                .Where(u => u.IsApproved && u.Status == "Approved")
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            ViewBag.PendingUsers = pendingUsers;
            ViewBag.ApprovedUsers = approvedUsers;
            ViewBag.TotalCandidates = _context.Candidates.Count();
            ViewBag.TotalJobs = _context.Jobs.Count();
            ViewBag.TotalInterviews = _context.Interviews.Count();

            return View();
        }

        [HttpPost]
        public IActionResult Approve(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin") return RedirectToAction("Login", "Account");

            var user = _context.Recruiters.Find(id);
            if (user != null)
            {
                user.IsApproved = true;
                user.Status = "Approved";
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = HttpContext.Session.GetString("UserName") ?? "Admin";

                // Push notification for the user
                _context.Notifications.Add(new Notification
                {
                    TargetUserEmail = user.Email,
                    TargetRole = user.Role,
                    Title = "Account Approved!",
                    Message = $"Congratulations {user.Name}! Your {user.Role} account has been approved by the Admin. You can now log in.",
                    CreatedAt = DateTime.Now,
                    TargetUrl = "/Account/Login"
                });

                _context.SaveChanges();
                TempData["Success"] = $"Account for '{user.Name}' ({user.Email}) has been APPROVED successfully!";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Reject(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin") return RedirectToAction("Login", "Account");

            var user = _context.Recruiters.Find(id);
            if (user != null)
            {
                user.IsApproved = false;
                user.Status = "Rejected";
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = HttpContext.Session.GetString("UserName") ?? "Admin";
                _context.SaveChanges();
                TempData["Info"] = $"Account request for '{user.Name}' has been REJECTED.";
            }
            return RedirectToAction("Index");
        }
    }
}
