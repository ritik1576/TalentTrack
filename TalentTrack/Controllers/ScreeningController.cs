using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentTrack.Data;
using TalentTrack.Models;
using TalentTrack.Models.DTOs;

namespace TalentTrack.Controllers
{
    public class ScreeningController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScreeningController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Screening Dashboard
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            var applications = _context.CandidateApplications
                .Include(ca => ca.Candidate)
                .Include(ca => ca.Job)
                .OrderByDescending(ca => ca.AppliedDate)
                .ToList();

            var screenings = _context.Screenings.ToList();

            var vm = new ScreeningDashboardViewModel
            {
                Applications = applications.Select(a => new ScreeningDashboardItem
                {
                    ApplicationId = a.ApplicationId,
                    CandidateName = a.Candidate?.Name ?? "N/A",
                    JobTitle = a.Job?.JobTitle ?? "N/A",
                    AppliedDate = a.AppliedDate,
                    ApplicationStatus = a.Status,
                    ScreeningStatus = screenings.Any(s => s.ApplicationId == a.ApplicationId)
                        ? screenings.Where(s => s.ApplicationId == a.ApplicationId)
                            .OrderByDescending(s => s.ScreeningDate).First().Status
                        : "Not Started"
                }).ToList()
            };

            return View(vm);
        }

        // Screening Page - GET
        public IActionResult Screen(int applicationId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            var application = _context.CandidateApplications
                .Include(ca => ca.Candidate)
                    .ThenInclude(c => c!.CandidateSkills)
                .Include(ca => ca.Job)
                    .ThenInclude(j => j!.JobSkills)
                .FirstOrDefault(ca => ca.ApplicationId == applicationId);

            if (application == null) return NotFound();

            // Check if application has already been screened
            var existingScreening = _context.Screenings.Any(s => s.ApplicationId == applicationId && s.Status == "Completed");
            if (existingScreening)
            {
                TempData["Error"] = "This application has already been screened.";
                return RedirectToAction("Index");
            }

            // Check if candidate was screened or interviewed before (duplicate detection)
            var candidateId = application.CandidateId;
            var hasPreviousScreening = _context.Screenings
                .Include(s => s.Application)
                .Any(s => s.Application != null && s.Application.CandidateId == candidateId && s.Status == "Completed");

            var hasPreviousInterview = _context.Interviews
                .Any(i => i.CandidateId == candidateId);

            // Build skill inputs from job's required skills
            var skillInputs = application.Job?.JobSkills.Select(js =>
            {
                var candidateSkill = application.Candidate?.CandidateSkills
                    .FirstOrDefault(cs => cs.SkillName == js.SkillName);
                return new ScreeningSkillInput
                {
                    SkillName = js.SkillName,
                    HasSkill = candidateSkill != null,
                    ExperienceYears = candidateSkill?.ExperienceYears ?? 0
                };
            }).ToList() ?? new List<ScreeningSkillInput>();

            var vm = new ScreeningViewModel
            {
                Application = application,
                Candidate = application.Candidate!,
                Job = application.Job!,
                RequiredSkills = application.Job?.JobSkills.ToList() ?? new List<JobSkill>(),
                CandidateSkills = application.Candidate?.CandidateSkills.ToList() ?? new List<CandidateSkill>(),
                SkillInputs = skillInputs,
                HasPreviousScreening = hasPreviousScreening || hasPreviousInterview
            };

            return View(vm);
        }

        // Screening Page - POST
        [HttpPost]
        public IActionResult Screen(int applicationId, List<ScreeningSkillInput> SkillInputs, string? Notes)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            var application = _context.CandidateApplications.Find(applicationId);
            if (application == null) return NotFound();

            var existingScreening = _context.Screenings.Any(s => s.ApplicationId == applicationId && s.Status == "Completed");
            if (existingScreening)
            {
                TempData["Error"] = "This application has already been screened.";
                return RedirectToAction("Index");
            }

            var userIdStr = HttpContext.Session.GetString("UserId");
            int.TryParse(userIdStr, out int userId);

            // Create screening record
            var screening = new Screening
            {
                ApplicationId = applicationId,
                ScreenedByUserId = userId,
                ScreeningDate = DateTime.Now,
                Status = "Completed",
                Notes = Notes
            };

            _context.Screenings.Add(screening);
            _context.SaveChanges();

            // Save skill evaluations
            if (SkillInputs != null)
            {
                foreach (var input in SkillInputs)
                {
                    _context.ScreeningSkillEvaluations.Add(new ScreeningSkillEvaluation
                    {
                        ScreeningId = screening.ScreeningId,
                        SkillName = input.SkillName,
                        HasSkill = input.HasSkill,
                        ExperienceYears = input.HasSkill ? input.ExperienceYears : null
                    });
                }
                _context.SaveChanges();
            }

            // Update application status
            application.Status = "Screened";
            _context.SaveChanges();

            TempData["Success"] = "Screening completed successfully!";
            return RedirectToAction("Index");
        }
    }
}
