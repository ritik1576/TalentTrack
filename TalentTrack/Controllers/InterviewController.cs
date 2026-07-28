using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TalentTrack.Data;
using TalentTrack.Models;

namespace TalentTrack.Controllers
{
    public class InterviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InterviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        // List all interviews (Recruiter / Admin view)
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role)) return RedirectToAction("Login", "Account");

            var interviews = _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.Job)
                .Include(i => i.Interviewer)
                .OrderByDescending(i => i.InterviewDate)
                .ToList();

            return View(interviews);
        }

        // Open Schedule Interview Page
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            ViewBag.Candidates = new SelectList(_context.Candidates, "CandidateId", "Name");
            ViewBag.Jobs = new SelectList(_context.Jobs, "JobId", "JobTitle");
            ViewBag.Interviewers = new SelectList(_context.Interviewers, "InterviewerId", "Name");
            return View();
        }

        // Save Scheduled Interview
        [HttpPost]
        public IActionResult Create(Interview interview)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            // Requirement 1: Only future dates allowed (cannot schedule in the past)
            if (interview.InterviewDate < DateTime.Now)
            {
                ModelState.AddModelError("InterviewDate", "Interview date and time cannot be in the past! Please select a future date & time.");
            }

            if (ModelState.IsValid)
            {
                _context.Interviews.Add(interview);

                // Add Notification
                var candidate = _context.Candidates.Find(interview.CandidateId);
                var job = _context.Jobs.Find(interview.JobId);
                var interviewer = interview.InterviewerId.HasValue ? _context.Interviewers.Find(interview.InterviewerId.Value) : null;

                if (interviewer != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        TargetUserEmail = interviewer.Email,
                        TargetRole = "Interviewer",
                        Title = "New Interview Assigned!",
                        Message = $"You have been assigned to interview '{candidate?.Name}' for position '{job?.JobTitle}' on {interview.InterviewDate:dd MMM yyyy, hh:mm tt}.",
                        CreatedAt = DateTime.Now,
                        TargetUrl = "/Interviewer/Dashboard"
                    });
                }

                _context.SaveChanges();
                TempData["Success"] = "Interview scheduled successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Candidates = new SelectList(_context.Candidates, "CandidateId", "Name", interview.CandidateId);
            ViewBag.Jobs = new SelectList(_context.Jobs, "JobId", "JobTitle", interview.JobId);
            ViewBag.Interviewers = new SelectList(_context.Interviewers, "InterviewerId", "Name", interview.InterviewerId);
            return View(interview);
        }

        // Open Edit Interview Page
        public IActionResult Edit(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            var interview = _context.Interviews.Find(id);
            if (interview == null)
            {
                return NotFound();
            }

            ViewBag.Candidates = new SelectList(_context.Candidates, "CandidateId", "Name", interview.CandidateId);
            ViewBag.Jobs = new SelectList(_context.Jobs, "JobId", "JobTitle", interview.JobId);
            ViewBag.Interviewers = new SelectList(_context.Interviewers, "InterviewerId", "Name", interview.InterviewerId);
            return View(interview);
        }

        // Save Edited Interview
        [HttpPost]
        public IActionResult Edit(Interview interview)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            // Requirement 1: Only future dates allowed (or equal to current time)
            if (interview.InterviewDate < DateTime.Now.AddMinutes(-5))
            {
                ModelState.AddModelError("InterviewDate", "Interview date and time cannot be set to the past.");
            }

            if (ModelState.IsValid)
            {
                _context.Interviews.Update(interview);
                _context.SaveChanges();
                TempData["Success"] = "Interview schedule updated successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Candidates = new SelectList(_context.Candidates, "CandidateId", "Name", interview.CandidateId);
            ViewBag.Jobs = new SelectList(_context.Jobs, "JobId", "JobTitle", interview.JobId);
            ViewBag.Interviewers = new SelectList(_context.Interviewers, "InterviewerId", "Name", interview.InterviewerId);
            return View(interview);
        }

        // Delete Interview
        public IActionResult Delete(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            var interview = _context.Interviews.Find(id);
            if (interview != null)
            {
                _context.Interviews.Remove(interview);
                _context.SaveChanges();
                TempData["Info"] = "Interview schedule deleted.";
            }
            return RedirectToAction("Index");
        }
    }
}
