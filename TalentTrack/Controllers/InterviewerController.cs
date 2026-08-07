using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentTrack.Data;
using TalentTrack.Models;

namespace TalentTrack.Controllers
{
    public class InterviewerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InterviewerController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Interviewer? GetCurrentInterviewer()
        {
            var idStr = HttpContext.Session.GetString("UserId");
            var email = HttpContext.Session.GetString("UserEmail");

            if (!string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out var id))
            {
                var found = _context.Interviewers.Find(id);
                if (found != null) return found;
            }

            if (!string.IsNullOrEmpty(email))
            {
                var foundByEmail = _context.Interviewers.FirstOrDefault(i => i.Email.ToLower() == email.ToLower());
                if (foundByEmail != null)
                {
                    HttpContext.Session.SetString("UserId", foundByEmail.InterviewerId.ToString());
                    return foundByEmail;
                }
            }

            return null;
        }

        public IActionResult Dashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Interviewer") return RedirectToAction("Login", "Account");

            var interviewer = GetCurrentInterviewer();
            if (interviewer == null) return RedirectToAction("Login", "Account");

            var today = DateTime.Today;

            // Requirement 6 & 7: STRICT DATA ISOLATION & UPCOMING SCHEDULE FIX
            // Only interviews assigned to THIS interviewer
            var myInterviewsQuery = _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.Job)
                .Include(i => i.Feedbacks)
                .Include(i => i.Interviewers)
                .Include(i => i.Participants)
                    .ThenInclude(p => p.Recruiter)
                .Where(i => i.Interviewers.Any(iv => iv.InterviewerId == interviewer.InterviewerId));

            var todayInterviews = myInterviewsQuery
                .Where(i => i.InterviewDate.Date == today)
                .OrderBy(i => i.InterviewDate)
                .ToList();

            var upcomingInterviews = myInterviewsQuery
                .Where(i => i.InterviewDate > DateTime.Now)
                .OrderBy(i => i.InterviewDate)
                .ToList();

            var totalConducted = myInterviewsQuery
                .Count(i => i.InterviewDate < DateTime.Now || i.Status == "Completed");

            var feedbacksGiven = _context.InterviewFeedbacks
                .Count(f => f.InterviewerId == interviewer.InterviewerId);

            var vm = new InterviewerDashboardViewModel
            {
                CurrentInterviewer = interviewer,
                TodayInterviews = todayInterviews,
                UpcomingInterviews = upcomingInterviews,
                TotalInterviewsConducted = totalConducted,
                FeedbacksGiven = feedbacksGiven
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Interviewer") return RedirectToAction("Login", "Account");

            var interviewer = GetCurrentInterviewer();
            if (interviewer == null) return RedirectToAction("Login", "Account");

            return View(interviewer);
        }

        [HttpPost]
        public IActionResult Profile(Interviewer model)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Interviewer") return RedirectToAction("Login", "Account");

            if (!string.IsNullOrEmpty(model.Phone))
            {
                var digitsOnly = new string(model.Phone.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length != 10)
                {
                    TempData["Error"] = "Phone number must be exactly 10 digits.";
                    return RedirectToAction("Profile");
                }
                model.Phone = digitsOnly;
            }

            var existing = _context.Interviewers.Find(model.InterviewerId);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.Phone = model.Phone;
            existing.Department = model.Department;
            existing.Specialization = model.Specialization;
            existing.LinkedIn = model.LinkedIn;
            existing.Bio = model.Bio;
            existing.YearsExperience = model.YearsExperience;

            var parts = model.Name?.Split(' ') ?? new[] { "I" };
            existing.AvatarInitials = string.Concat(parts.Take(2).Select(w => w.Length > 0 ? w[0].ToString().ToUpper() : ""));

            _context.Interviewers.Update(existing);
            _context.SaveChanges();

            HttpContext.Session.SetString("UserName", existing.Name);
            HttpContext.Session.SetString("UserInitials", existing.AvatarInitials ?? "I");

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }
    }
}
