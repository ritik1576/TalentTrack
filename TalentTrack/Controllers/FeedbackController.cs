using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentTrack.Data;
using TalentTrack.Models;
using TalentTrack.Services;
using TalentTrack.Models.DTOs;

namespace TalentTrack.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService? _emailService;

        public FeedbackController(ApplicationDbContext context, IEmailService? emailService = null)
        {
            _context = context;
            _emailService = emailService;
        }

        // Recruiter & Admin view — all feedbacks or filtered by interviewer
        public async Task<IActionResult> Index(string? searchCandidate, string? filterRecommendation, int page = 1, int pageSize = 10)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role)) return RedirectToAction("Login", "Account");

            var query = _context.InterviewFeedbacks
                .Include(f => f.Candidate)
                .Include(f => f.Interviewer)
                .Include(f => f.SkillRatings)
                .Include(f => f.Interview)
                    .ThenInclude(i => i!.Job)
                .AsQueryable();

            // If logged in as Interviewer, only show their own feedbacks
            if (role == "Interviewer")
            {
                var idStr = HttpContext.Session.GetString("UserId");
                if (int.TryParse(idStr, out var interviewerId))
                {
                    query = query.Where(f => f.InterviewerId == interviewerId);
                }
            }

            if (!string.IsNullOrEmpty(searchCandidate))
            {
                query = query.Where(f => f.Candidate != null && f.Candidate.Name.Contains(searchCandidate));
            }

            if (!string.IsNullOrEmpty(filterRecommendation))
            {
                query = query.Where(f => f.Recommendation == filterRecommendation);
            }

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

            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedList = new PagedList<InterviewFeedback>(items, totalItems, page, pageSize);

            var applications = await _context.CandidateApplications.ToListAsync();
            ViewBag.Applications = applications;

            ViewBag.SearchCandidate = searchCandidate;
            ViewBag.FilterRecommendation = filterRecommendation;

            return View(pagedList);
        }

        // Interviewer submits feedback for an interview
        [HttpGet]
        public IActionResult Create(int interviewId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Interviewer") return RedirectToAction("Login", "Account");

            var interview = _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.Job)
                    .ThenInclude(j => j.JobSkills)
                .FirstOrDefault(i => i.InterviewId == interviewId);

            if (interview == null) return NotFound();

            var idStr = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(idStr, out var interviewerId))
                return RedirectToAction("Login", "Account");

            var existing = _context.InterviewFeedbacks.FirstOrDefault(f => f.InterviewId == interviewId && f.InterviewerId == interviewerId);
            if (existing != null)
            {
                TempData["Info"] = "You have already submitted feedback for this interview.";
                return RedirectToAction("Dashboard", "Interviewer");
            }

            ViewBag.Interview = interview;
            var model = new InterviewFeedback
            {
                InterviewId = interviewId,
                CandidateId = interview.CandidateId
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Create(InterviewFeedback feedback)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Interviewer") return RedirectToAction("Login", "Account");

            var idStr = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(idStr, out var interviewerId))
                return RedirectToAction("Login", "Account");

            feedback.InterviewerId = interviewerId;
            feedback.CreatedAt = DateTime.Now;

            _context.InterviewFeedbacks.Add(feedback);

            // Update interview status to Completed only when all assigned technical interviewers have submitted feedback
            var interview = _context.Interviews
                .Include(i => i.Participants)
                    .ThenInclude(p => p.Recruiter)
                .FirstOrDefault(i => i.InterviewId == feedback.InterviewId);
            if (interview != null)
            {
                var assignedInterviewerIds = _context.Interviews
                    .Include(i => i.Interviewers)
                    .FirstOrDefault(i => i.InterviewId == feedback.InterviewId)?
                    .Interviewers.Select(iv => iv.InterviewerId)
                    .ToList() ?? new List<int>();

                var submittedInterviewerIds = _context.InterviewFeedbacks
                    .Where(f => f.InterviewId == feedback.InterviewId)
                    .Select(f => f.InterviewerId)
                    .ToList();

                if (!submittedInterviewerIds.Contains(interviewerId))
                {
                    submittedInterviewerIds.Add(interviewerId);
                }

                bool allSubmitted = assignedInterviewerIds.All(id => submittedInterviewerIds.Contains(id));
                if (allSubmitted || !assignedInterviewerIds.Any())
                {
                    interview.Status = "Completed";
                    _context.Interviews.Update(interview);
                }
            }

            // Push notification to Recruiter & Admin
            var candidate = _context.Candidates.Find(feedback.CandidateId);
            var interviewer = _context.Interviewers.Find(interviewerId);

            _context.Notifications.Add(new Notification
            {
                TargetRole = "Recruiter",
                Title = "Interview Feedback Submitted",
                Message = $"Interviewer '{interviewer?.Name}' submitted a {feedback.OverallRating}-star rating ({feedback.Recommendation}) for candidate '{candidate?.Name}'.",
                CreatedAt = DateTime.Now,
                TargetUrl = $"/Feedback/Details/{feedback.FeedbackId}"
            });

            _context.SaveChanges();

            TempData["Success"] = "Feedback and rating submitted successfully!";
            return RedirectToAction("Dashboard", "Interviewer");
        }

        // Details of a single feedback
        public IActionResult Details(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role)) return RedirectToAction("Login", "Account");

            var feedback = _context.InterviewFeedbacks
                .Include(f => f.Candidate)
                .Include(f => f.Interviewer)
                .Include(f => f.SkillRatings)
                .Include(f => f.Interview)
                    .ThenInclude(i => i!.Job)
                .FirstOrDefault(f => f.FeedbackId == id);

            if (feedback == null) return NotFound();

            return View(feedback);
        }

        // POST: /Feedback/ProcessApplication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessApplication(int applicationId, string decision)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin")
            {
                return Challenge();
            }

            var application = await _context.CandidateApplications
                .Include(ca => ca.Candidate)
                .FirstOrDefaultAsync(ca => ca.ApplicationId == applicationId);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            if (decision == "Accept")
            {
                application.BackgroundVerificationStatus = "Pending";
                _context.CandidateApplications.Update(application);

                var candidate = application.Candidate;
                if (candidate != null)
                {
                    string emailBody = $@"
============================================================
SIMULATED EMAIL SENT
Date: {DateTime.Now:dd MMM yyyy, hh:mm tt}
To: {candidate.Email}
Subject: Onboarding Document Verification Portal Enabled
Body:
Dear {candidate.Name},

Congratulations! You have been shortlisted for onboarding at TalentTrack.
Your document verification portal is now active. Please log in to your dashboard and upload the required onboarding verification documents:
- ID Proof
- Address Proof
- Education Certificate
- Experience Letter

Please note:
- Your registered Contact Number ({candidate.Phone}) is your login password.
- You can upload your documents through the dashboard.
- You can change your password by clicking 'Forgot Password' on the login screen.

Best regards,
Recruitment Team
TalentTrack
============================================================
";
                    var emailLogPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "simulated_emails.txt");
                    var emailDirectory = Path.GetDirectoryName(emailLogPath);
                    if (!string.IsNullOrEmpty(emailDirectory) && !Directory.Exists(emailDirectory))
                    {
                        Directory.CreateDirectory(emailDirectory);
                    }
                    await System.IO.File.AppendAllTextAsync(emailLogPath, emailBody);

                    // Send actual email via SMTP if configured
                    try
                    {
                        if (_emailService != null && !string.IsNullOrEmpty(candidate.Email))
                        {
                            string subject = "Onboarding Document Verification Portal Enabled";
                            string htmlBody = $@"
                                <h2>Dear {candidate.Name},</h2>
                                <p><strong>Congratulations!</strong> You have been shortlisted for onboarding at TalentTrack.</p>
                                <p>Your document verification portal is now active. Please log in to your dashboard and upload the required onboarding verification documents:</p>
                                <ul>
                                    <li>ID Proof</li>
                                    <li>Address Proof</li>
                                    <li>Education Certificate</li>
                                    <li>Experience Letter</li>
                                </ul>
                                <p><strong>Login Details:</strong></p>
                                <ul>
                                    <li><strong>Username:</strong> {candidate.Email}</li>
                                    <li><strong>Password:</strong> {candidate.Phone} (Your registered Contact Number)</li>
                                </ul>
                                <p>You can change your password by clicking 'Forgot Password' on the login screen.</p>
                                <p>Best regards,<br/>Recruitment Team<br/>TalentTrack</p>";

                            await _emailService.SendEmailAsync(candidate.Email, subject, htmlBody);
                        }
                    }
                    catch (System.Exception)
                    {
                        // Fail silently or log error so local demo won't crash if SMTP is not configured
                    }

                    _context.Notifications.Add(new Notification
                    {
                        TargetRole = "Candidate",
                        TargetUserEmail = candidate.Email,
                        Title = "Shortlisted for Onboarding!",
                        Message = "Congratulations! Your document verification portal has been enabled. Please log in and upload your documents.",
                        CreatedAt = DateTime.Now,
                        TargetUrl = "/CandidateDocument/Portal"
                    });

                    TempData["Success"] = $"Candidate '{candidate.Name}' accepted and shortlisted! A notification has been sent to their email.";
                }
                else
                {
                    TempData["Success"] = "Candidate accepted successfully.";
                }
            }
            else if (decision == "Reject")
            {
                application.BackgroundVerificationStatus = "Rejected";
                application.Status = "Rejected";
                _context.CandidateApplications.Update(application);

                TempData["Warning"] = "Candidate rejected successfully. Application status set to Rejected.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
