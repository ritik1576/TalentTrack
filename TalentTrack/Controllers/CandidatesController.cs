using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TalentTrack.Data;
using TalentTrack.Models;
using TalentTrack.Models.DTOs;

namespace TalentTrack.Controllers
{
    public class CandidatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CandidatesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Show all candidates
        public IActionResult Index()
        {
            var candidates = _context.Candidates.Include(c => c.CandidateSkills).ToList();
            return View(candidates);
        }

        // Check duplicate candidate and return details - GET
        [HttpGet]
        public async Task<IActionResult> CheckDuplicate(string email, string phone, int? jobId)
        {
            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            {
                return Json(new { isDuplicate = false });
            }

            Candidate? existingCandidate = null;
            if (!string.IsNullOrEmpty(email))
            {
                existingCandidate = await _context.Candidates
                    .Include(c => c.Applications)
                        .ThenInclude(a => a.Job)
                    .FirstOrDefaultAsync(c => c.Email == email);
            }

            if (existingCandidate == null && !string.IsNullOrEmpty(phone))
            {
                var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length == 10)
                {
                    existingCandidate = await _context.Candidates
                        .Include(c => c.Applications)
                            .ThenInclude(a => a.Job)
                        .FirstOrDefaultAsync(c => c.Phone == digitsOnly);
                }
            }

            if (existingCandidate != null)
            {
                bool alreadyAppliedToCurrentJob = false;
                if (jobId.HasValue)
                {
                    alreadyAppliedToCurrentJob = existingCandidate.Applications.Any(a => a.JobId == jobId.Value);
                }

                var previousApplications = existingCandidate.Applications.Select(a => new
                {
                    jobTitle = a.Job?.JobTitle ?? "N/A",
                    appliedDate = a.AppliedDate.ToString("yyyy-MM-dd"),
                    status = a.Status
                }).ToList();

                return Json(new
                {
                    isDuplicate = true,
                    candidateId = existingCandidate.CandidateId,
                    name = existingCandidate.Name,
                    email = existingCandidate.Email,
                    phone = existingCandidate.Phone,
                    alreadyAppliedToCurrentJob = alreadyAppliedToCurrentJob,
                    previousApplications = previousApplications
                });
            }

            return Json(new { isDuplicate = false });
        }

        // Open Add Candidate Page
        public IActionResult Create()
        {
            ViewBag.Jobs = new SelectList(_context.Jobs.Where(j => j.Status == "Active" || j.Status == "Open" || j.Status == "Hiring"), "JobId", "JobTitle");
            return View();
        }

        // Save Candidate Data + Resume PDF File Upload
        [HttpPost]
        public async Task<IActionResult> Create(Candidate candidate, IFormFile? resumeFile, string? skillNames, string? skillExperiences, int? jobId, int? useExistingCandidateId)
        {
            if (useExistingCandidateId.HasValue && useExistingCandidateId.Value > 0)
            {
                var existing = await _context.Candidates.Include(c => c.CandidateSkills).FirstOrDefaultAsync(c => c.CandidateId == useExistingCandidateId.Value);
                if (existing != null)
                {
                    // Update existing profile details with form fields if provided
                    existing.FirstName = candidate.FirstName;
                    existing.LastName = candidate.LastName;
                    existing.Name = $"{candidate.FirstName} {candidate.LastName}".Trim();
                    if (!string.IsNullOrEmpty(candidate.Phone))
                    {
                        var digitsOnly = new string(candidate.Phone.Where(char.IsDigit).ToArray());
                        if (digitsOnly.Length == 10) existing.Phone = digitsOnly;
                    }
                    existing.Experience = candidate.Experience;

                    if (resumeFile != null && resumeFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(resumeFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await resumeFile.CopyToAsync(stream);
                        }
                        existing.Resume = $"/uploads/resumes/{uniqueFileName}";
                    }

                    _context.Candidates.Update(existing);
                    await _context.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(skillNames))
                    {
                        var oldSkills = _context.CandidateSkills.Where(cs => cs.CandidateId == existing.CandidateId);
                        _context.CandidateSkills.RemoveRange(oldSkills);
                        await _context.SaveChangesAsync();
                        SaveCandidateSkills(existing.CandidateId, skillNames, skillExperiences);
                        UpdateLegacySkills(existing);
                    }

                    if (jobId.HasValue)
                    {
                        var existingApp = await _context.CandidateApplications.FirstOrDefaultAsync(a => a.CandidateId == existing.CandidateId && a.JobId == jobId.Value);
                        if (existingApp == null)
                        {
                            var app = new CandidateApplication
                            {
                                CandidateId = existing.CandidateId,
                                JobId = jobId.Value,
                                AppliedDate = DateTime.Now,
                                Status = "Applied"
                            };
                            _context.CandidateApplications.Add(app);
                            await _context.SaveChangesAsync();

                            PerformAutomatedScreeningForJob(existing.CandidateId, jobId.Value);
                            TempData["Success"] = "Application submitted and automated screening executed successfully!";
                        }
                        else
                        {
                            TempData["Warning"] = "Warning: Candidate has already applied for this job.";
                        }
                    }
                    else
                    {
                        TempData["Success"] = "Candidate details updated successfully!";
                    }

                    return RedirectToAction("Index");
                }
            }

            if (!string.IsNullOrEmpty(candidate.Email))
            {
                var existingCandidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Email == candidate.Email);
                if (existingCandidate != null)
                {
                    TempData["Warning"] = "Warning: A candidate with this email already exists!";
                    ModelState.AddModelError("Email", "Candidate with this email already exists!");
                    ViewBag.Jobs = new SelectList(_context.Jobs.Where(j => j.Status == "Active" || j.Status == "Open" || j.Status == "Hiring"), "JobId", "JobTitle", jobId);
                    return View(candidate);
                }
            }

            if (!string.IsNullOrEmpty(candidate.Phone))
            {
                var digitsOnly = new string(candidate.Phone.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length != 10)
                {
                    ModelState.AddModelError("Phone", "Phone number must be exactly 10 digits.");
                }
                else
                {
                    candidate.Phone = digitsOnly;
                    var existingCandidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Phone == digitsOnly);
                    if (existingCandidate != null)
                    {
                        TempData["Warning"] = "Warning: A candidate with this phone number already exists!";
                        ModelState.AddModelError("Phone", "Candidate with this phone number already exists!");
                        ViewBag.Jobs = new SelectList(_context.Jobs.Where(j => j.Status == "Active" || j.Status == "Open" || j.Status == "Hiring"), "JobId", "JobTitle", jobId);
                        return View(candidate);
                    }
                }
            }

            if (resumeFile != null && resumeFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(resumeFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await resumeFile.CopyToAsync(stream);
                }

                candidate.Resume = $"/uploads/resumes/{uniqueFileName}";
            }
            else if (string.IsNullOrEmpty(candidate.Resume))
            {
                candidate.Resume = "Not Uploaded";
            }

            if (ModelState.IsValid)
            {
                candidate.Name = $"{candidate.FirstName} {candidate.LastName}".Trim();
                _context.Candidates.Add(candidate);
                await _context.SaveChangesAsync();

                // Save per-skill experience
                SaveCandidateSkills(candidate.CandidateId, skillNames, skillExperiences);

                // Update legacy Skills field
                UpdateLegacySkills(candidate);

                // Perform automated screening for selected job
                if (jobId.HasValue)
                {
                    var app = new CandidateApplication
                    {
                        CandidateId = candidate.CandidateId,
                        JobId = jobId.Value,
                        AppliedDate = DateTime.Now,
                        Status = "Applied"
                    };
                    _context.CandidateApplications.Add(app);
                    await _context.SaveChangesAsync();

                    PerformAutomatedScreeningForJob(candidate.CandidateId, jobId.Value);
                }

                TempData["Success"] = "Candidate added and automatically screened successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Jobs = new SelectList(_context.Jobs.Where(j => j.Status == "Active" || j.Status == "Open" || j.Status == "Hiring"), "JobId", "JobTitle", jobId);
            return View(candidate);
        }

        // Open Edit Candidate Page
        public IActionResult Edit(int id)
        {
            var candidate = _context.Candidates.Include(c => c.CandidateSkills).FirstOrDefault(c => c.CandidateId == id);
            if (candidate == null)
            {
                return NotFound();
            }
            return View(candidate);
        }

        // Save Edited Candidate Data + Resume PDF update
        [HttpPost]
        public async Task<IActionResult> Edit(Candidate candidate, IFormFile? resumeFile, string? skillNames, string? skillExperiences)
        {
            if (!string.IsNullOrEmpty(candidate.Email))
            {
                var existingWithSameEmail = await _context.Candidates
                    .FirstOrDefaultAsync(c => c.Email == candidate.Email && c.CandidateId != candidate.CandidateId);
                if (existingWithSameEmail != null)
                {
                    TempData["Warning"] = "Warning: Another candidate with this email already exists!";
                    ModelState.AddModelError("Email", "Another candidate with this email already exists!");
                    return View(candidate);
                }
            }

            if (!string.IsNullOrEmpty(candidate.Phone))
            {
                var digitsOnly = new string(candidate.Phone.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length != 10)
                {
                    ModelState.AddModelError("Phone", "Phone number must be exactly 10 digits.");
                }
                else
                {
                    candidate.Phone = digitsOnly;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(candidate);
            }

            var existing = await _context.Candidates.FindAsync(candidate.CandidateId);
            if (existing == null) return NotFound();

            if (resumeFile != null && resumeFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(resumeFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await resumeFile.CopyToAsync(stream);
                }

                existing.Resume = $"/uploads/resumes/{uniqueFileName}";
            }

            existing.FirstName = candidate.FirstName;
            existing.LastName = candidate.LastName;
            existing.Name = $"{candidate.FirstName} {candidate.LastName}".Trim();
            existing.Email = candidate.Email;
            existing.Phone = candidate.Phone;
            existing.Skills = candidate.Skills;
            existing.Experience = candidate.Experience;

            _context.Candidates.Update(existing);
            await _context.SaveChangesAsync();

            // Update per-skill experience
            var oldSkills = _context.CandidateSkills.Where(cs => cs.CandidateId == existing.CandidateId);
            _context.CandidateSkills.RemoveRange(oldSkills);
            await _context.SaveChangesAsync();

            SaveCandidateSkills(existing.CandidateId, skillNames, skillExperiences);
            UpdateLegacySkills(existing);

            // Re-run automated screening on update
            PerformAutomatedScreening(existing);

            TempData["Success"] = "Candidate profile updated and screened successfully!";
            return RedirectToAction("Index");
        }

        // Candidate Profile Page
        public IActionResult Profile(int id)
        {
            var candidate = _context.Candidates
                .Include(c => c.CandidateSkills)
                .Include(c => c.Applications)
                    .ThenInclude(a => a.Job)
                .FirstOrDefault(c => c.CandidateId == id);

            if (candidate == null) return NotFound();

            var vm = new CandidateProfileViewModel
            {
                Candidate = candidate,
                Skills = candidate.CandidateSkills.ToList(),
                Applications = candidate.Applications.ToList()
            };

            return View(vm);
        }

        // Apply for Job - GET
        public IActionResult Apply()
        {
            ViewBag.Candidates = new SelectList(_context.Candidates, "CandidateId", "Name");
            ViewBag.Jobs = new SelectList(_context.Jobs.Where(j => j.Status == "Active" || j.Status == "Open" || j.Status == "Hiring"), "JobId", "JobTitle");
            return View();
        }

        // Apply for Job - POST
        [HttpPost]
        public IActionResult Apply(int candidateId, int jobId)
        {
            // Check duplicate application
            var existingApplication = _context.CandidateApplications
                .FirstOrDefault(ca => ca.CandidateId == candidateId && ca.JobId == jobId);

            if (existingApplication != null)
            {
                TempData["Warning"] = "Candidate has already applied for this job.";
                ViewBag.Candidates = new SelectList(_context.Candidates, "CandidateId", "Name", candidateId);
                ViewBag.Jobs = new SelectList(_context.Jobs.Where(j => j.Status == "Active" || j.Status == "Open" || j.Status == "Hiring"), "JobId", "JobTitle", jobId);
                return View();
            }

            var application = new CandidateApplication
            {
                CandidateId = candidateId,
                JobId = jobId,
                AppliedDate = DateTime.Now,
                Status = "Applied"
            };

            _context.CandidateApplications.Add(application);
            _context.SaveChanges();

            // Run automated screening for this specific job application
            var candidate = _context.Candidates.Include(c => c.CandidateSkills).FirstOrDefault(c => c.CandidateId == candidateId);
            var job = _context.Jobs.Include(j => j.JobSkills).FirstOrDefault(j => j.JobId == jobId);

            if (candidate != null && job != null)
            {
                bool passed = true;
                var skillInputs = new List<ScreeningSkillEvaluation>();

                foreach (var reqSkill in job.JobSkills)
                {
                    var candSkill = candidate.CandidateSkills.FirstOrDefault(cs => cs.SkillName.Equals(reqSkill.SkillName, StringComparison.OrdinalIgnoreCase));
                    bool hasSkill = candSkill != null && candSkill.ExperienceYears >= reqSkill.RequiredExperience;
                    if (!hasSkill)
                    {
                        passed = false;
                    }
                    skillInputs.Add(new ScreeningSkillEvaluation
                    {
                        SkillName = reqSkill.SkillName,
                        HasSkill = candSkill != null,
                        ExperienceYears = candSkill?.ExperienceYears
                    });
                }

                // Check if a screening already exists for this application
                var existingScreening = _context.Screenings.FirstOrDefault(s => s.ApplicationId == application.ApplicationId);
                if (existingScreening != null)
                {
                    _context.Screenings.Remove(existingScreening);
                }

                var screening = new Screening
                {
                    ApplicationId = application.ApplicationId,
                    ScreenedByUserId = 1, // System Admin
                    ScreeningDate = DateTime.Now,
                    Status = "Completed",
                    Notes = passed ? "Automated screening: PASSED" : "Automated screening: FAILED (Insufficient skills/experience)"
                };
                _context.Screenings.Add(screening);
                _context.SaveChanges();

                foreach (var eval in skillInputs)
                {
                    eval.ScreeningId = screening.ScreeningId;
                    _context.ScreeningSkillEvaluations.Add(eval);
                }

                if (IsDuplicateCandidate(candidate))
                {
                    application.Status = "Pending";
                    _context.SaveChanges();
                    TempData["Warning"] = "Application submitted. Duplicate candidate details detected. Status set to Pending.";
                }
                else
                {
                    application.Status = "Screened";
                    _context.SaveChanges();
                    if (passed)
                    {
                        TempData["Success"] = "Application submitted and automated screening PASSED!";
                    }
                    else
                    {
                        TempData["Warning"] = "Application submitted. Automated screening completed (some requirements not met).";
                    }
                }
            }
            else
            {
                TempData["Success"] = "Application submitted successfully!";
            }

            return RedirectToAction("Index");
        }

        // Delete Candidate
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var candidate = await _context.Candidates
                .Include(c => c.Applications)
                .Include(c => c.CandidateSkills)
                .FirstOrDefaultAsync(c => c.CandidateId == id);

            if (candidate == null)
            {
                return NotFound();
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Delete Screenings & ScreeningSkillEvaluations
                    var appIds = candidate.Applications.Select(a => a.ApplicationId).ToList();
                    var screenings = await _context.Screenings
                        .Where(s => appIds.Contains(s.ApplicationId))
                        .ToListAsync();
                    var screeningIds = screenings.Select(s => s.ScreeningId).ToList();

                    var evaluations = await _context.ScreeningSkillEvaluations
                        .Where(e => screeningIds.Contains(e.ScreeningId))
                        .ToListAsync();
                    _context.ScreeningSkillEvaluations.RemoveRange(evaluations);
                    _context.Screenings.RemoveRange(screenings);

                    // 2. Delete Interviews, Feedbacks, Participants
                    var interviews = await _context.Interviews
                        .Where(i => i.CandidateId == id)
                        .ToListAsync();
                    var interviewIds = interviews.Select(i => i.InterviewId).ToList();

                    var participants = await _context.InterviewParticipants
                        .Where(p => interviewIds.Contains(p.InterviewId))
                        .ToListAsync();
                    _context.InterviewParticipants.RemoveRange(participants);

                    var feedbacks = await _context.InterviewFeedbacks
                        .Where(f => interviewIds.Contains(f.InterviewId) || f.CandidateId == id)
                        .ToListAsync();
                    _context.InterviewFeedbacks.RemoveRange(feedbacks);

                    _context.Interviews.RemoveRange(interviews);

                    // 3. Delete CandidateApplications
                    _context.CandidateApplications.RemoveRange(candidate.Applications);

                    // 4. Delete CandidateSkills
                    _context.CandidateSkills.RemoveRange(candidate.CandidateSkills);

                    // 5. Delete Candidate
                    _context.Candidates.Remove(candidate);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = $"Candidate '{candidate.Name}' and all associated records deleted successfully!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = $"Error deleting candidate: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper: Save candidate skills from form data
        private void SaveCandidateSkills(int candidateId, string? skillNames, string? skillExperiences)
        {
            if (string.IsNullOrEmpty(skillNames)) return;

            var names = skillNames.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var exps = (skillExperiences ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i].Trim();
                int exp = 0;
                if (i < exps.Length) int.TryParse(exps[i].Trim(), out exp);

                if (!string.IsNullOrEmpty(name))
                {
                    _context.CandidateSkills.Add(new CandidateSkill
                    {
                        CandidateId = candidateId,
                        SkillName = name,
                        ExperienceYears = exp
                    });
                }
            }
            _context.SaveChanges();
        }

        // Helper: Update legacy Skills field from CandidateSkills
        private void UpdateLegacySkills(Candidate candidate)
        {
            var skills = _context.CandidateSkills
                .Where(cs => cs.CandidateId == candidate.CandidateId)
                .Select(cs => cs.SkillName)
                .ToList();

            if (skills.Any())
            {
                candidate.Skills = string.Join(", ", skills);
                _context.SaveChanges();
            }
        }

        // Helper: Automated screening for candidate's actual applications
        private void PerformAutomatedScreening(Candidate candidate)
        {
            var applications = _context.CandidateApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j!.JobSkills)
                .Where(ca => ca.CandidateId == candidate.CandidateId)
                .ToList();
            var candidateSkills = _context.CandidateSkills.Where(cs => cs.CandidateId == candidate.CandidateId).ToList();

            foreach (var app in applications)
            {
                var job = app.Job;
                if (job == null) continue;

                // Evaluate skills
                bool passed = true;
                var skillInputs = new List<ScreeningSkillEvaluation>();

                foreach (var reqSkill in job.JobSkills)
                {
                    var candSkill = candidateSkills.FirstOrDefault(cs => cs.SkillName.Equals(reqSkill.SkillName, StringComparison.OrdinalIgnoreCase));
                    bool hasSkill = candSkill != null && candSkill.ExperienceYears >= reqSkill.RequiredExperience;
                    if (!hasSkill)
                    {
                        passed = false;
                    }
                    skillInputs.Add(new ScreeningSkillEvaluation
                    {
                        SkillName = reqSkill.SkillName,
                        HasSkill = candSkill != null,
                        ExperienceYears = candSkill?.ExperienceYears
                    });
                }

                // Check if screening record already exists
                var screening = _context.Screenings.FirstOrDefault(s => s.ApplicationId == app.ApplicationId);
                if (screening == null)
                {
                    screening = new Screening
                    {
                        ApplicationId = app.ApplicationId,
                        ScreenedByUserId = 1, // System Admin
                        ScreeningDate = DateTime.Now,
                        Status = "Completed"
                    };
                    _context.Screenings.Add(screening);
                    _context.SaveChanges();
                }

                screening.Notes = passed ? "Automated screening: PASSED" : "Automated screening: FAILED (Insufficient skills/experience)";
                _context.SaveChanges();

                // Clear old evaluations if any
                var oldEvals = _context.ScreeningSkillEvaluations.Where(e => e.ScreeningId == screening.ScreeningId);
                _context.ScreeningSkillEvaluations.RemoveRange(oldEvals);

                foreach (var eval in skillInputs)
                {
                    eval.ScreeningId = screening.ScreeningId;
                    _context.ScreeningSkillEvaluations.Add(eval);
                }

                if (IsDuplicateCandidate(candidate))
                {
                    app.Status = "Pending";
                }
                else
                {
                    app.Status = "Screened";
                }
                _context.SaveChanges();
            }
        }

        // Helper: Automated screening for specific job application
        private void PerformAutomatedScreeningForJob(int candidateId, int jobId)
        {
            var candidate = _context.Candidates.Include(c => c.CandidateSkills).FirstOrDefault(c => c.CandidateId == candidateId);
            var job = _context.Jobs.Include(j => j.JobSkills).FirstOrDefault(j => j.JobId == jobId);
            if (candidate == null || job == null) return;

            var app = _context.CandidateApplications.FirstOrDefault(ca => ca.CandidateId == candidateId && ca.JobId == jobId);
            if (app == null) return;

            bool passed = true;
            var skillInputs = new List<ScreeningSkillEvaluation>();

            foreach (var reqSkill in job.JobSkills)
            {
                var candSkill = candidate.CandidateSkills.FirstOrDefault(cs => cs.SkillName.Equals(reqSkill.SkillName, StringComparison.OrdinalIgnoreCase));
                bool hasSkill = candSkill != null && candSkill.ExperienceYears >= reqSkill.RequiredExperience;
                if (!hasSkill)
                {
                    passed = false;
                }
                skillInputs.Add(new ScreeningSkillEvaluation
                {
                    SkillName = reqSkill.SkillName,
                    HasSkill = candSkill != null,
                    ExperienceYears = candSkill?.ExperienceYears
                });
            }

            var screening = _context.Screenings.FirstOrDefault(s => s.ApplicationId == app.ApplicationId);
            if (screening == null)
            {
                screening = new Screening
                {
                    ApplicationId = app.ApplicationId,
                    ScreenedByUserId = 1, // System Admin
                    ScreeningDate = DateTime.Now,
                    Status = "Completed"
                };
                _context.Screenings.Add(screening);
                _context.SaveChanges();
            }

            screening.Notes = passed ? "Automated screening: PASSED" : "Automated screening: FAILED (Insufficient skills/experience)";
            _context.SaveChanges();

            var oldEvals = _context.ScreeningSkillEvaluations.Where(e => e.ScreeningId == screening.ScreeningId);
            _context.ScreeningSkillEvaluations.RemoveRange(oldEvals);

            foreach (var eval in skillInputs)
            {
                eval.ScreeningId = screening.ScreeningId;
                _context.ScreeningSkillEvaluations.Add(eval);
            }

            if (IsDuplicateCandidate(candidate))
            {
                app.Status = "Pending";
            }
            else
            {
                app.Status = "Screened";
            }
            _context.SaveChanges();
        }

        private bool IsDuplicateCandidate(Candidate candidate)
        {
            if (candidate == null) return false;
            bool dupEmail = !string.IsNullOrEmpty(candidate.Email) && 
                _context.Candidates.Any(c => c.CandidateId != candidate.CandidateId && c.Email.ToLower() == candidate.Email.ToLower());
            bool dupPhone = !string.IsNullOrEmpty(candidate.Phone) && 
                _context.Candidates.Any(c => c.CandidateId != candidate.CandidateId && c.Phone == candidate.Phone);
            return dupEmail || dupPhone;
        }
    }
}