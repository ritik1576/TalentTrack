using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentTrack.Data;
using TalentTrack.Models;
using TalentTrack.Models.DTOs;

namespace TalentTrack.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin") return RedirectToAction("Login", "Account");

            var pendingUsers = await _context.Recruiters
                .Where(u => u.Status == "Pending" || !u.IsApproved)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var approvedUsersQuery = _context.Recruiters
                .Where(u => u.IsApproved && u.Status == "Approved")
                .OrderByDescending(u => u.CreatedAt);

            var totalApprovedItems = await approvedUsersQuery.CountAsync();

            // Ensure bounds for page
            if (page < 1) page = 1;

            if (pageSize == -1)
            {
                pageSize = totalApprovedItems > 0 ? totalApprovedItems : 10;
            }
            else if (pageSize < 1)
            {
                pageSize = 10;
            }

            var totalApprovedPages = (int)Math.Ceiling(totalApprovedItems / (double)pageSize);
            if (page > totalApprovedPages && totalApprovedPages > 0) page = totalApprovedPages;

            var approvedItems = await approvedUsersQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedApprovedList = new PagedList<Recruiter>(approvedItems, totalApprovedItems, page, pageSize);

            ViewBag.PendingUsers = pendingUsers;
            ViewBag.ApprovedUsersPaged = pagedApprovedList;
            ViewBag.TotalCandidates = await _context.Candidates.CountAsync();
            ViewBag.TotalJobs = await _context.Jobs.CountAsync();
            ViewBag.TotalInterviews = await _context.Interviews.CountAsync();

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
