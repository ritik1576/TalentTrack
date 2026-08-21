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
        public async Task<IActionResult> Index(string? searchCandidate, int? filterJob, int page = 1, int pageSize = 10)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            var query = _context.CandidateApplications
                .Include(ca => ca.Candidate)
                .Include(ca => ca.Job)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchCandidate))
            {
                var lowerSearch = searchCandidate.ToLower().Trim();
                query = query.Where(ca => ca.Candidate != null && ca.Candidate.Name.ToLower().Contains(lowerSearch));
            }

            if (filterJob.HasValue)
            {
                query = query.Where(ca => ca.JobId == filterJob.Value);
            }

            query = query.OrderByDescending(ca => ca.AppliedDate);

            var totalItems = await query.CountAsync();

            // Ensure bounds for page
            if (page < 1) page = 1;

            if (pageSize == -1)
            {
                pageSize = totalItems > 0 ? totalItems : 10;
            }
            else if (pageSize < 1)
            {
                pageSize = 10;
            }

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page > totalPages && totalPages > 0) page = totalPages;

            var applications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var screenings = await _context.Screenings.ToListAsync();
            var candidates = await _context.Candidates.ToListAsync();

            var dashboardItems = applications.Select(a => {
                var duplicateReason = "";
                var isDup = false;
                if (a.Candidate != null) {
                    var dupEmail = !string.IsNullOrEmpty(a.Candidate.Email) && 
                        candidates.Any(c => c.CandidateId != a.Candidate.CandidateId && c.Email.ToLower() == a.Candidate.Email.ToLower());
                    var dupPhone = !string.IsNullOrEmpty(a.Candidate.Phone) && 
                        candidates.Any(c => c.CandidateId != a.Candidate.CandidateId && c.Phone == a.Candidate.Phone);
                    if (dupEmail && dupPhone) {
                        duplicateReason = "Email & Contact No. already exist";
                        isDup = true;
                    } else if (dupEmail) {
                        duplicateReason = "Email already exists";
                        isDup = true;
                    } else if (dupPhone) {
                        duplicateReason = "Contact No. already exists";
                        isDup = true;
                    }
                }
                return new ScreeningDashboardItem
                {
                    ApplicationId = a.ApplicationId,
                    CandidateName = a.Candidate?.Name ?? "N/A",
                    JobTitle = a.Job?.JobTitle ?? "N/A",
                    AppliedDate = a.AppliedDate,
                    ApplicationStatus = a.Status,
                    ScreeningStatus = screenings.Any(s => s.ApplicationId == a.ApplicationId)
                        ? screenings.Where(s => s.ApplicationId == a.ApplicationId)
                            .OrderByDescending(s => s.ScreeningDate).First().Status
                        : "Not Started",
                    IsDuplicate = isDup,
                    DuplicateReason = duplicateReason
                };
            }).ToList();

            var vm = new ScreeningDashboardViewModel
            {
                Applications = dashboardItems,
                Metadata = new PaginationMetadata
                {
                    TotalItems = totalItems,
                    PageSize = pageSize,
                    CurrentPage = page,
                    TotalPages = totalPages
                }
            };

            ViewBag.Jobs = await _context.Jobs.ToListAsync();
            ViewBag.SearchCandidate = searchCandidate;
            ViewBag.FilterJob = filterJob;

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
            
            var previousScreenings = _context.Screenings
                .Include(s => s.Application)
                    .ThenInclude(a => a!.Job)
                .Where(s => s.Application != null && s.Application.CandidateId == candidateId && s.Status == "Completed")
                .OrderByDescending(s => s.ScreeningDate)
                .ToList();

            var previousInterviews = _context.Interviews
                .Include(i => i.Job)
                .Where(i => i.CandidateId == candidateId)
                .OrderByDescending(i => i.InterviewDate)
                .ToList();

            var hasPreviousScreening = previousScreenings.Any() || previousInterviews.Any();

            // Build skill inputs from job's required skills - starts unchecked (HasSkill = false, Experience = 0)
            var skillInputs = application.Job?.JobSkills.Select(js =>
            {
                return new ScreeningSkillInput
                {
                    SkillName = js.SkillName,
                    HasSkill = false,
                    ExperienceYears = 0
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
                HasPreviousScreening = hasPreviousScreening,
                PreviousScreenings = previousScreenings,
                PreviousInterviews = previousInterviews
            };

            return View(vm);
        }

        // Screening Page - POST
        [HttpPost]
        public IActionResult Screen(int applicationId, List<ScreeningSkillInput> SkillInputs, string? Notes)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            var application = _context.CandidateApplications
                .Include(ca => ca.Job)
                    .ThenInclude(j => j!.JobSkills)
                .FirstOrDefault(ca => ca.ApplicationId == applicationId);
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

            // Check if candidate matches all required skills for the job
            bool skillsMatch = true;
            if (application.Job != null && application.Job.JobSkills != null && SkillInputs != null)
            {
                foreach (var requiredSkill in application.Job.JobSkills)
                {
                    var matchingInput = SkillInputs.FirstOrDefault(si => si.SkillName.Trim().ToLower() == requiredSkill.SkillName.Trim().ToLower());
                    if (matchingInput == null || !matchingInput.HasSkill || matchingInput.ExperienceYears < requiredSkill.RequiredExperience)
                    {
                        skillsMatch = false;
                        break;
                    }
                }
            }

            // Update application status
            var candidate = _context.Candidates.Find(application.CandidateId);
            bool isDup = false;
            if (candidate != null)
            {
                bool dupEmail = !string.IsNullOrEmpty(candidate.Email) && 
                    _context.Candidates.Any(c => c.CandidateId != candidate.CandidateId && c.Email.ToLower() == candidate.Email.ToLower());
                bool dupPhone = !string.IsNullOrEmpty(candidate.Phone) && 
                    _context.Candidates.Any(c => c.CandidateId != candidate.CandidateId && c.Phone == candidate.Phone);
                isDup = dupEmail || dupPhone;
            }

            application.Status = isDup ? "Pending" : "Screened";
            _context.SaveChanges();

            if (!skillsMatch)
            {
                TempData["Success"] = isDup 
                    ? "Screening completed. Duplicate candidate detected, status set to Pending. Note: Candidate does not meet all required skills/experience." 
                    : "Screening completed successfully! Note: Candidate does not meet all required skills/experience.";
            }
            else
            {
                TempData["Success"] = isDup 
                    ? "Screening completed. Duplicate candidate detected, status set to Pending." 
                    : "Screening completed successfully! Candidate matched all required skills.";
            }

            return RedirectToAction("Index");
        }
    }
}
