using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TalentTrack.Data;
using TalentTrack.Models;

namespace TalentTrack.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Public landing page — no login required
        public IActionResult Landing()
        {
            return View();
        }

        public IActionResult Index()
        {
            // Only Recruiter can see recruiter dashboard
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");
            if (role == "Interviewer")
                return RedirectToAction("Dashboard", "Interviewer");

            var today = DateTime.Today;

            var viewModel = new DashboardViewModel
            {
                TotalJobs = _context.Jobs.Count(),
                TotalCandidates = _context.Candidates.Count(),
                TotalInterviews = _context.Interviews.Count(),
                OpenPositions = _context.Jobs.Count(j => j.Status == "Active" || j.Status == "Open" || j.Status == "Hiring"),
                RecentJobs = _context.Jobs.OrderByDescending(j => j.JobId).Take(5).ToList(),
                RecentCandidates = _context.Candidates.OrderByDescending(c => c.CandidateId).Take(5).ToList(),
                UpcomingInterviews = _context.Interviews
                    .Include(i => i.Candidate)
                    .Include(i => i.Job)
                    .Include(i => i.Interviewer)
                    .Where(i => i.InterviewDate >= DateTime.Now)
                    .OrderBy(i => i.InterviewDate)
                    .Take(5)
                    .ToList(),
                TodayInterviews = _context.Interviews
                    .Include(i => i.Candidate)
                    .Include(i => i.Job)
                    .Include(i => i.Interviewer)
                    .Where(i => i.InterviewDate.Date == today)
                    .OrderBy(i => i.InterviewDate)
                    .ToList()
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
