using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TalentTrack.Data;
using TalentTrack.Models;
using TalentTrack.Services;

namespace TalentTrack.Controllers
{
    public class InterviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public InterviewController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // List all interviews (Recruiter / Admin view) with search and filtering
        public IActionResult Index(string? searchCandidate, string? searchInterviewer, string? searchRecruiter, DateTime? filterDate, string? filterStatus)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role)) return RedirectToAction("Login", "Account");

            var query = _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.Job)
                .Include(i => i.Interviewers)
                .Include(i => i.Participants)
                    .ThenInclude(p => p.Recruiter)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchCandidate))
            {
                query = query.Where(i => i.Candidate != null && i.Candidate.Name.Contains(searchCandidate));
            }

            if (!string.IsNullOrEmpty(searchInterviewer))
            {
                query = query.Where(i => i.Interviewers.Any(iv => iv.Name.Contains(searchInterviewer)));
            }

            if (!string.IsNullOrEmpty(searchRecruiter))
            {
                query = query.Where(i => i.Participants.Any(p => p.Role == "recruiter" && p.Recruiter != null && p.Recruiter.Name.Contains(searchRecruiter)));
            }

            if (filterDate.HasValue)
            {
                query = query.Where(i => i.InterviewDate.Date == filterDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(filterStatus))
            {
                query = query.Where(i => i.Status == filterStatus);
            }

            var interviews = query.OrderByDescending(i => i.InterviewDate).ToList();
            return View(interviews);
        }

        // Open Schedule Interview Page
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            var screenedCandidates = GetEligibleCandidatesForInterview();

            ViewBag.Candidates = new SelectList(screenedCandidates, "CandidateId", "Name");

            var recruiters = _context.Recruiters
                .Where(u => u.Role == "Recruiter" && u.Status == "Approved" && u.IsApproved)
                .OrderBy(u => u.Name)
                .ToList();

            var interviewers = _context.Interviewers
                .OrderBy(u => u.Name)
                .ToList();

            ViewBag.Recruiters = new SelectList(recruiters, "RecruiterId", "Name");
            ViewBag.Interviewers = new MultiSelectList(interviewers, "InterviewerId", "Name");

            return View(new Interview { Duration = 30 });
        }

        // Save Scheduled Interview
        [HttpPost]
        public async Task<IActionResult> Create(Interview interview, int? SelectedRecruiterId, int[] SelectedInterviewers)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            interview.Mode = "Offline";
            var candidateApp = _context.CandidateApplications
                .Where(ca => ca.CandidateId == interview.CandidateId)
                .OrderByDescending(ca => ca.AppliedDate)
                .FirstOrDefault();
            if (candidateApp != null)
            {
                interview.JobId = candidateApp.JobId;
            }

            ValidateInterview(interview, SelectedRecruiterId, SelectedInterviewers, isEdit: false);

            if (ModelState.IsValid)
            {
                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    _context.Interviews.Add(interview);
                    _context.SaveChanges();

                    if (SelectedInterviewers != null)
                    {
                        var assignedInterviewers = _context.Interviewers.Where(iv => SelectedInterviewers.Contains(iv.InterviewerId)).ToList();
                        foreach (var iv in assignedInterviewers)
                        {
                            interview.Interviewers.Add(iv);
                        }
                    }

                    if (SelectedRecruiterId.HasValue)
                    {
                        _context.InterviewParticipants.Add(new InterviewParticipant
                        {
                            InterviewId = interview.InterviewId,
                            RecruiterId = SelectedRecruiterId.Value,
                            Role = "recruiter"
                        });
                    }

                    _context.SaveChanges();

                    await SendNotificationsAndMailsAsync(interview, "Invitation: New Panel Interview Scheduled");
                    
                    transaction.Commit();
                    Console.WriteLine("[LOG] Interview Created");

                    TempData["Success"] = "Interview scheduled successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "An error occurred while scheduling the interview: " + ex.Message);
                }
            }

            // Reload ViewBags if validation failed
            var screenedCandidates = GetEligibleCandidatesForInterview();

            ViewBag.Candidates = new SelectList(screenedCandidates, "CandidateId", "Name", interview.CandidateId);

            var recruiters = _context.Recruiters
                .Where(u => u.Role == "Recruiter" && u.Status == "Approved" && u.IsApproved)
                .OrderBy(u => u.Name)
                .ToList();

            var interviewers = _context.Interviewers
                .OrderBy(u => u.Name)
                .ToList();

            ViewBag.Recruiters = new SelectList(recruiters, "RecruiterId", "Name", SelectedRecruiterId);
            ViewBag.Interviewers = new MultiSelectList(interviewers, "InterviewerId", "Name", SelectedInterviewers);

            return View(interview);
        }

        // Open Edit Interview Page
        public IActionResult Edit(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            var interview = _context.Interviews
                .Include(i => i.Participants)
                .Include(i => i.Interviewers)
                .FirstOrDefault(i => i.InterviewId == id);

            if (interview == null)
            {
                return NotFound();
            }

            var screenedCandidates = GetEligibleCandidatesForInterview();

            ViewBag.Candidates = new SelectList(screenedCandidates, "CandidateId", "Name", interview.CandidateId);

            var currentRecruiterId = interview.Participants.FirstOrDefault(p => p.Role == "recruiter")?.RecruiterId;
            var currentInterviewers = interview.Interviewers.Select(iv => iv.InterviewerId).ToArray();

            var recruiters = _context.Recruiters
                .Where(u => u.Role == "Recruiter" && u.Status == "Approved" && u.IsApproved)
                .OrderBy(u => u.Name)
                .ToList();

            var interviewers = _context.Interviewers
                .OrderBy(u => u.Name)
                .ToList();

            ViewBag.Recruiters = new SelectList(recruiters, "RecruiterId", "Name", currentRecruiterId);
            ViewBag.Interviewers = new MultiSelectList(interviewers, "InterviewerId", "Name", currentInterviewers);

            return View(interview);
        }

        // Save Edited Interview
        [HttpPost]
        public async Task<IActionResult> Edit(Interview interview, int? SelectedRecruiterId, int[] SelectedInterviewers)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            interview.Mode = "Offline";
            interview.JobId = null;
            interview.MeetingLink = null;

            ValidateInterview(interview, SelectedRecruiterId, SelectedInterviewers, isEdit: true);

            if (ModelState.IsValid)
            {
                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    var existingInterview = _context.Interviews
                        .Include(i => i.Participants)
                        .Include(i => i.Interviewers)
                        .FirstOrDefault(i => i.InterviewId == interview.InterviewId);

                    if (existingInterview == null)
                    {
                        return NotFound();
                    }

                    bool isRescheduled = existingInterview.InterviewDate != interview.InterviewDate;

                    // Update fields
                    existingInterview.CandidateId = interview.CandidateId;
                    var candidateApp = _context.CandidateApplications
                        .Where(ca => ca.CandidateId == interview.CandidateId)
                        .OrderByDescending(ca => ca.AppliedDate)
                        .FirstOrDefault();
                    if (candidateApp != null)
                    {
                        existingInterview.JobId = candidateApp.JobId;
                    }
                    existingInterview.InterviewDate = interview.InterviewDate;
                    existingInterview.Status = interview.Status;
                    existingInterview.Mode = "Offline";
                    existingInterview.Notes = interview.Notes;
                    existingInterview.Duration = interview.Duration;
                    existingInterview.MeetingLink = null;

                    // Remove current recruiter participants
                    var recruiterParticipants = existingInterview.Participants.Where(p => p.Role == "recruiter").ToList();
                    _context.InterviewParticipants.RemoveRange(recruiterParticipants);

                    // Update interviewers junction table
                    existingInterview.Interviewers.Clear();
                    if (SelectedInterviewers != null)
                    {
                        var assignedInterviewers = _context.Interviewers.Where(iv => SelectedInterviewers.Contains(iv.InterviewerId)).ToList();
                        foreach (var iv in assignedInterviewers)
                        {
                            existingInterview.Interviewers.Add(iv);
                        }
                    }

                    if (SelectedRecruiterId.HasValue)
                    {
                        _context.InterviewParticipants.Add(new InterviewParticipant
                        {
                            InterviewId = interview.InterviewId,
                            RecruiterId = SelectedRecruiterId.Value,
                            Role = "recruiter"
                        });
                    }

                    _context.SaveChanges();

                    if (interview.Status == "Cancelled")
                    {
                        await SendNotificationsAndMailsAsync(existingInterview, "Cancellation: Interview Cancelled", isCancel: true);
                        Console.WriteLine("[LOG] Interview Cancelled");
                    }
                    else
                    {
                        string subject = isRescheduled ? "Reschedule: Interview Rescheduled" : "Update: Interview Details Updated";
                        await SendNotificationsAndMailsAsync(existingInterview, subject);
                        if (isRescheduled)
                        {
                            Console.WriteLine("[LOG] Interview Rescheduled");
                        }
                        else
                        {
                            Console.WriteLine("[LOG] Interview Updated");
                        }
                    }

                    transaction.Commit();
                    TempData["Success"] = "Interview schedule updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "An error occurred while updating the interview: " + ex.Message);
                }
            }

            // Reload select lists if validation fails
            var screenedCandidates = GetEligibleCandidatesForInterview();

            ViewBag.Candidates = new SelectList(screenedCandidates, "CandidateId", "Name", interview.CandidateId);

            var recruiters = _context.Recruiters
                .Where(u => u.Role == "Recruiter" && u.Status == "Approved" && u.IsApproved)
                .OrderBy(u => u.Name)
                .ToList();

            var interviewers = _context.Interviewers
                .OrderBy(u => u.Name)
                .ToList();

            ViewBag.Recruiters = new SelectList(recruiters, "RecruiterId", "Name", SelectedRecruiterId);
            ViewBag.Interviewers = new MultiSelectList(interviewers, "InterviewerId", "Name", SelectedInterviewers);

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
                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    _context.Interviews.Remove(interview);
                    _context.SaveChanges();
                    transaction.Commit();
                    Console.WriteLine("[LOG] Interview Cancelled");
                    TempData["Info"] = "Interview schedule deleted.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Failed to delete interview: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        // Direct Cancellation helper
        public async Task<IActionResult> Cancel(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin") return RedirectToAction("Login", "Account");

            var interview = _context.Interviews
                .Include(i => i.Participants)
                .FirstOrDefault(i => i.InterviewId == id);

            if (interview != null)
            {
                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    interview.Status = "Cancelled";
                    _context.SaveChanges();

                    await SendNotificationsAndMailsAsync(interview, "Cancellation: Interview Cancelled", isCancel: true);
                    transaction.Commit();
                    Console.WriteLine("[LOG] Interview Cancelled");

                    TempData["Success"] = "Interview cancelled successfully.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Failed to cancel interview: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        private void ValidateInterview(Interview interview, int? SelectedRecruiterId, int[] SelectedInterviewers, bool isEdit)
        {
            // Candidate required
            if (interview.CandidateId <= 0)
            {
                ModelState.AddModelError("CandidateId", "Candidate is required.");
            }
            else
            {
                var app = _context.CandidateApplications
                    .Include(ca => ca.Job)
                        .ThenInclude(j => j!.JobSkills)
                    .Where(ca => ca.CandidateId == interview.CandidateId)
                    .OrderByDescending(ca => ca.AppliedDate)
                    .FirstOrDefault();

                if (app == null)
                {
                    ModelState.AddModelError("CandidateId", "Candidate application not found.");
                    return;
                }

                var screening = _context.Screenings
                    .Where(s => s.ApplicationId == app.ApplicationId && s.Status == "Completed")
                    .OrderByDescending(s => s.ScreeningDate)
                    .FirstOrDefault();

                if (screening == null)
                {
                    ModelState.AddModelError("CandidateId", "Interview scheduling is not allowed until Screening is completed.");
                }
                else
                {
                    // Check skills match
                    bool skillsMatch = true;
                    if (app.Job != null && app.Job.JobSkills != null)
                    {
                        var evaluations = _context.ScreeningSkillEvaluations
                            .Where(sse => sse.ScreeningId == screening.ScreeningId)
                            .ToList();

                        foreach (var requiredSkill in app.Job.JobSkills)
                        {
                            var matchingEval = evaluations.FirstOrDefault(e => e.SkillName.Trim().ToLower() == requiredSkill.SkillName.Trim().ToLower());
                            if (matchingEval == null || !matchingEval.HasSkill || matchingEval.ExperienceYears < requiredSkill.RequiredExperience)
                            {
                                skillsMatch = false;
                                break;
                            }
                        }
                    }

                    if (!skillsMatch)
                    {
                        ModelState.AddModelError("CandidateId", "Interview scheduling is not allowed because the candidate does not meet the required skills or experience in screening.");
                    }
                }
            }

            // Technical interviewer required
            if (SelectedInterviewers == null || SelectedInterviewers.Length == 0)
            {
                ModelState.AddModelError("", "At least one Technical Interviewer is required.");
            }

            // Duration required
            if (interview.Duration <= 0)
            {
                ModelState.AddModelError("Duration", "Duration must be greater than 0.");
            }

            // Date validation (cannot be in the past)
            var threshold = isEdit ? DateTime.Now.AddMinutes(-5) : DateTime.Now;
            if (interview.InterviewDate < threshold)
            {
                ModelState.AddModelError("InterviewDate", "Interview date and time cannot be in the past.");
            }

            // Prevent duplicate panel members
            if (SelectedRecruiterId.HasValue && SelectedInterviewers != null)
            {
                if (SelectedInterviewers.Contains(SelectedRecruiterId.Value))
                {
                    ModelState.AddModelError("", "Duplicate panel members are not allowed.");
                }
            }

            // Prevent invalid/inactive users
            if (SelectedInterviewers != null)
            {
                foreach (var id in SelectedInterviewers)
                {
                    var user = _context.Interviewers.Find(id);
                    if (user == null)
                    {
                        ModelState.AddModelError("", $"User with ID {id} is not an active Technical Interviewer.");
                    }
                }
            }

            if (SelectedRecruiterId.HasValue)
            {
                var user = _context.Recruiters.Find(SelectedRecruiterId.Value);
                if (user == null || user.Role != "Recruiter" || user.Status != "Approved" || !user.IsApproved)
                {
                    ModelState.AddModelError("", $"User with ID {SelectedRecruiterId.Value} is not an active, approved Recruiter.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Recruiter is required.");
            }

            // Scheduling Conflict Detection
            if (ModelState.IsValid)
            {
                if (CheckSchedulingConflicts(interview, SelectedRecruiterId, SelectedInterviewers ?? Array.Empty<int>(), out var conflictError))
                {
                    ModelState.AddModelError("", conflictError);
                }
            }
        }

        private bool CheckSchedulingConflicts(Interview interview, int? selectedRecruiterId, int[] selectedInterviewers, out string errorMessage)
        {
            errorMessage = "";
            var newStart = interview.InterviewDate;
            var newEnd = interview.InterviewDate.AddMinutes(interview.Duration);

            // Fetch all non-cancelled interviews
            var existingInterviews = _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.Interviewers)
                .Include(i => i.Participants)
                .Where(i => i.Status != "Cancelled" && i.InterviewId != interview.InterviewId)
                .ToList();

            foreach (var exist in existingInterviews)
            {
                var existStart = exist.InterviewDate;
                var existEnd = existStart.AddMinutes(exist.Duration);

                // Overlap check
                if (newStart < existEnd && existStart < newEnd)
                {
                    // Candidate conflict
                    if (exist.CandidateId == interview.CandidateId)
                    {
                        errorMessage = $"Candidate already has another interview scheduled at {existStart:dd MMM yyyy, hh:mm tt}.";
                        return true;
                    }

                    // Participant conflicts (Interviewers from junction table)
                    var existInterviewerIds = exist.Interviewers.Select(iv => iv.InterviewerId).ToList();
                    
                    foreach (var interId in selectedInterviewers)
                    {
                        if (existInterviewerIds.Contains(interId))
                        {
                            var user = _context.Interviewers.Find(interId);
                            errorMessage = $"Technical Interviewer '{user?.Name}' already has another interview scheduled at {existStart:dd MMM yyyy, hh:mm tt}.";
                            return true;
                        }
                    }

                    var existUserIds = exist.Participants.Select(p => p.RecruiterId).ToList();
                    if (selectedRecruiterId.HasValue && existUserIds.Contains(selectedRecruiterId.Value))
                    {
                        var user = _context.Recruiters.Find(selectedRecruiterId.Value);
                        errorMessage = $"Recruiter '{user?.Name}' already has another interview scheduled at {existStart:dd MMM yyyy, hh:mm tt}.";
                        return true;
                    }
                }
            }

            return false;
        }

        private async Task SendNotificationsAndMailsAsync(Interview interview, string subject, bool isCancel = false)
        {
            try
            {
                var candidate = interview.Candidate ?? _context.Candidates.Find(interview.CandidateId);
                var job = interview.Job ?? (interview.JobId.HasValue ? _context.Jobs.Find(interview.JobId.Value) : null);
                
                // Recruiters
                var participants = _context.InterviewParticipants
                    .Include(p => p.Recruiter)
                    .Where(p => p.InterviewId == interview.InterviewId && p.Role == "recruiter")
                    .ToList();

                // Technical Interviewers (from many-to-many relation)
                var interviewers = interview.Interviewers;
                if (interviewers == null || !interviewers.Any())
                {
                    interviewers = _context.Interviews
                        .Include(i => i.Interviewers)
                        .FirstOrDefault(i => i.InterviewId == interview.InterviewId)?.Interviewers ?? new List<Interviewer>();
                }

                var companyName = HttpContext.Session.GetString("Company") ?? "TalentTrack";
                var panelDetails = new List<string>();
                foreach (var p in participants)
                {
                    if (p.Recruiter != null)
                    {
                        panelDetails.Add($"{p.Recruiter.Name} (Recruiter)");
                    }
                }
                foreach (var iv in interviewers)
                {
                    panelDetails.Add($"{iv.Name} (Technical Interviewer)");
                }
                var panelString = string.Join(", ", panelDetails);

                var emailBody = $"Subject: {subject}\n" +
                                $"Dear Participant,\n\n" +
                                $"Here are the details for the interview:\n" +
                                $"- Candidate Name: {candidate?.Name ?? "N/A"}\n" +
                                $"- Position: {job?.JobTitle ?? "N/A"}\n" +
                                $"- Date: {interview.InterviewDate:dd MMM yyyy}\n" +
                                $"- Time: {interview.InterviewDate:hh:mm tt}\n" +
                                $"- Duration: {interview.Duration} minutes\n" +
                                $"- Meeting Link: {interview.MeetingLink ?? "N/A"}\n" +
                                $"- Panel Members: {panelString}\n" +
                                $"- Company Name: {companyName}\n" +
                                $"- Notes: {interview.Notes ?? "None"}\n\n" +
                                $"Best regards,\n" +
                                $"{companyName} Team";

                var sentEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Send to Candidate
                if (candidate != null && !string.IsNullOrEmpty(candidate.Email))
                {
                    SendSimulatedEmail(candidate.Email, subject, emailBody);
                    sentEmails.Add(candidate.Email);
                }

                // Send to recruiters
                foreach (var p in participants)
                {
                    if (p.Recruiter != null && !string.IsNullOrEmpty(p.Recruiter.Email))
                    {
                        if (!sentEmails.Contains(p.Recruiter.Email))
                        {
                            SendSimulatedEmail(p.Recruiter.Email, subject, emailBody);
                            sentEmails.Add(p.Recruiter.Email);
                        }

                        // System notification
                        _context.Notifications.Add(new Notification
                        {
                            TargetUserEmail = p.Recruiter.Email,
                            TargetRole = p.Recruiter.Role,
                            Title = subject,
                            Message = isCancel 
                                ? $"Interview with candidate '{candidate?.Name}' for position '{job?.JobTitle}' has been cancelled."
                                : $"Interview with candidate '{candidate?.Name}' for position '{job?.JobTitle}' is scheduled/updated for {interview.InterviewDate:dd MMM yyyy, hh:mm tt}.",
                            CreatedAt = DateTime.Now,
                            TargetUrl = "/Interview/Index"
                        });
                    }
                }

                // Send to technical interviewers (Technical Interviewers table)
                foreach (var iv in interviewers)
                {
                    if (!string.IsNullOrEmpty(iv.Email))
                    {
                        if (!sentEmails.Contains(iv.Email))
                        {
                            SendSimulatedEmail(iv.Email, subject, emailBody);
                            sentEmails.Add(iv.Email);
                        }

                        // System notification
                        _context.Notifications.Add(new Notification
                        {
                            TargetUserEmail = iv.Email,
                            TargetRole = "Interviewer",
                            Title = subject,
                            Message = isCancel 
                                ? $"Interview with candidate '{candidate?.Name}' for position '{job?.JobTitle}' has been cancelled."
                                : $"Interview with candidate '{candidate?.Name}' for position '{job?.JobTitle}' is scheduled/updated for {interview.InterviewDate:dd MMM yyyy, hh:mm tt}.",
                            CreatedAt = DateTime.Now,
                            TargetUrl = "/Interviewer/Dashboard"
                        });
                    }
                }

                _context.SaveChanges();

                // Trigger real email sending asynchronously
                try
                {
                    // Collect all unique recipient email addresses and map to their names
                    var recipientEmails = new List<string>();
                    var recipientNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    if (candidate != null && !string.IsNullOrEmpty(candidate.Email))
                    {
                        recipientEmails.Add(candidate.Email);
                        recipientNames[candidate.Email] = candidate.Name ?? "Candidate";
                    }

                    foreach (var p in participants)
                    {
                        if (p.Recruiter != null && !string.IsNullOrEmpty(p.Recruiter.Email))
                        {
                            recipientEmails.Add(p.Recruiter.Email);
                            recipientNames[p.Recruiter.Email] = p.Recruiter.Name ?? "Recruiter";
                        }
                    }

                    foreach (var iv in interviewers)
                    {
                        if (!string.IsNullOrEmpty(iv.Email))
                        {
                            recipientEmails.Add(iv.Email);
                            recipientNames[iv.Email] = iv.Name ?? "Interviewer";
                        }
                    }

                    var uniqueRecipients = recipientEmails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    // Determine styling and text based on scenario
                    string title = subject;
                    string introMessage = "You have been invited to an interview. Here are the details:";
                    string statusText = "Scheduled";
                    string badgeBg = "#e0e7ff"; // Indigo light
                    string badgeColor = "#4338ca"; // Indigo deep

                    if (isCancel)
                    {
                        statusText = "Cancelled";
                        badgeBg = "#ffe4e6"; // Rose light
                        badgeColor = "#e11d48"; // Rose deep
                        introMessage = "Please note that the following interview has been cancelled. If you have any questions, please contact the recruiter.";
                    }
                    else if (subject.Contains("Reschedule") || subject.Contains("Rescheduled"))
                    {
                        statusText = "Rescheduled";
                        badgeBg = "#fef3c7"; // Amber light
                        badgeColor = "#d97706"; // Amber deep
                        introMessage = "Please note that your interview schedule has been updated. Here are the rescheduled details:";
                    }
                    else if (subject.Contains("Update") || subject.Contains("Updated"))
                    {
                        statusText = "Updated";
                        badgeBg = "#e0f2fe"; // Sky light
                        badgeColor = "#0284c7"; // Sky deep
                        introMessage = "The details for your upcoming interview have been updated. Please review the changes below:";
                    }

                    foreach (var email in uniqueRecipients)
                    {
                        try
                        {
                            string recipientName = recipientNames.TryGetValue(email, out var rName) ? rName : "Participant";

                            string htmlBody = GetHtmlEmailBody(
                                title,
                                introMessage,
                                statusText,
                                badgeBg,
                                badgeColor,
                                recipientName,
                                candidate?.Name ?? "N/A",
                                job?.JobTitle,
                                interview.InterviewDate,
                                interview.Duration,
                                interview.Mode ?? "Offline",
                                interview.MeetingLink,
                                panelString,
                                interview.Notes,
                                companyName
                            );

                            await _emailService.SendEmailAsync(email, subject, htmlBody);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[LOG] Actual Email Failed to {email}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LOG] Actual Email Process failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG] Email Processing Failed: {ex.Message}");
            }
        }

        private string GetHtmlEmailBody(
            string title, 
            string introMessage, 
            string statusText, 
            string badgeBg, 
            string badgeColor, 
            string recipientName,
            string candidateName, 
            string? position, 
            DateTime interviewDate, 
            int duration, 
            string mode, 
            string? meetingLink, 
            string panelString, 
            string? notes, 
            string companyName)
        {
            var badgeHtml = $@"<span style=""display: inline-block; padding: 6px 12px; font-size: 12px; font-weight: 600; border-radius: 9999px; background-color: {badgeBg}; color: {badgeColor}; text-transform: uppercase; letter-spacing: 0.05em;"">{statusText}</span>";
            
            var positionRowHtml = "";
            if (!string.IsNullOrEmpty(position))
            {
                positionRowHtml = $@"
                <tr>
                    <td width=""32"" valign=""top"" style=""padding-bottom: 16px;"">
                        <span style=""font-size: 18px;"">💼</span>
                    </td>
                    <td style=""padding-bottom: 16px;"">
                        <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: #64748b; margin-bottom: 2px;"">Position</div>
                        <div style=""font-size: 15px; font-weight: 600; color: #1e293b;"">{position}</div>
                    </td>
                </tr>";
            }

            var panelRowHtml = "";
            if (!string.IsNullOrEmpty(panelString))
            {
                panelRowHtml = $@"
                <tr>
                    <td width=""32"" valign=""top"" style=""padding-bottom: 16px;"">
                        <span style=""font-size: 18px;"">👥</span>
                    </td>
                    <td style=""padding-bottom: 16px;"">
                        <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: #64748b; margin-bottom: 2px;"">Interview Panel</div>
                        <div style=""font-size: 15px; font-weight: 600; color: #1e293b;"">{panelString}</div>
                    </td>
                </tr>";
            }

            var notesHtml = "";
            if (!string.IsNullOrEmpty(notes) && notes != "None")
            {
                notesHtml = $@"
                <div style=""margin-top: 24px; padding: 16px; background-color: #fffbeb; border-left: 4px solid #f59e0b; border-radius: 8px; margin-bottom: 28px;"">
                    <div style=""font-size: 13px; font-weight: 700; color: #b45309; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 4px;"">Notes & Instructions</div>
                    <div style=""font-size: 14px; line-height: 1.5; color: #78350f;"">{notes}</div>
                </div>";
            }

            var ctaHtml = "";
            if (mode == "Online" && !string.IsNullOrEmpty(meetingLink) && meetingLink != "N/A")
            {
                ctaHtml = $@"
                <div style=""text-align: center; margin-top: 32px; margin-bottom: 12px;"">
                    <a href=""{meetingLink}"" target=""_blank"" style=""display: inline-block; padding: 14px 32px; font-size: 15px; font-weight: 700; color: #ffffff; background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%); border-radius: 10px; text-decoration: none; box-shadow: 0 4px 6px -1px rgba(79, 70, 229, 0.2), 0 2px 4px -2px rgba(79, 70, 229, 0.2);"">Join Video Interview</a>
                </div>";
            }

            var formattedDate = interviewDate.ToString("dd MMM yyyy, dddd");
            var formattedTime = interviewDate.ToString("hh:mm tt");

            var currentYear = DateTime.Now.Year;

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Interview Notification</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; -webkit-font-smoothing: antialiased; color: #1e293b;"">
    <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f8fafc; padding: 40px 0;"">
        <tr>
            <td align=""center"">
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0;"">
                    <tr>
                        <td height=""6"" style=""background: linear-gradient(90deg, #4f46e5 0%, #06b6d4 100%);""></td>
                    </tr>
                    <tr>
                        <td align=""center"" style=""padding: 32px 32px 24px 32px; border-bottom: 1px solid #f1f5f9;"">
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                                <tr>
                                    <td align=""left"" style=""font-size: 24px; font-weight: 800; letter-spacing: -0.025em; color: #0f172a;"">
                                        <span style=""color: #4f46e5;"">Talent</span>Track
                                    </td>
                                    <td align=""right"">
                                        {badgeHtml}
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 32px;"">
                            <div style=""margin-bottom: 20px; font-size: 15px; color: #334155; line-height: 1.5;"">
                                <strong>To,</strong><br>
                                <span style=""font-size: 16px; font-weight: 700; color: #4f46e5;"">{recipientName}</span>
                            </div>
                            <h1 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 700; color: #0f172a; line-height: 1.3;"">
                                {title}
                            </h1>
                            <p style=""margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: #475569;"">
                                {introMessage}
                            </p>
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f8fafc; border-radius: 12px; border: 1px solid #e2e8f0; padding: 24px; margin-bottom: 28px;"">
                                <tr>
                                    <td>
                                        <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                                            <tr>
                                                <td width=""32"" valign=""top"" style=""padding-bottom: 16px;"">
                                                    <span style=""font-size: 18px;"">👤</span>
                                                </td>
                                                <td style=""padding-bottom: 16px;"">
                                                    <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: #64748b; margin-bottom: 2px;"">Candidate</div>
                                                    <div style=""font-size: 15px; font-weight: 600; color: #1e293b;"">{candidateName}</div>
                                                </td>
                                            </tr>
                                            {positionRowHtml}
                                            <tr>
                                                <td width=""32"" valign=""top"" style=""padding-bottom: 16px;"">
                                                    <span style=""font-size: 18px;"">📅</span>
                                                </td>
                                                <td style=""padding-bottom: 16px;"">
                                                    <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: #64748b; margin-bottom: 2px;"">Schedule</div>
                                                    <div style=""font-size: 15px; font-weight: 600; color: #1e293b;"">{formattedDate} at {formattedTime} <span style=""font-size: 13px; font-weight: 400; color: #64748b;"">({duration} mins)</span></div>
                                                </td>
                                            </tr>
                                            {panelRowHtml}
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            {notesHtml}
                            {ctaHtml}
                            <p style=""margin: 28px 0 0 0; font-size: 15px; line-height: 1.5; color: #475569;"">
                                Regards,<br>
                                <strong style=""color: #0f172a;"">Talent Track Team</strong>
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 32px; background-color: #f8fafc; border-top: 1px solid #f1f5f9; text-align: center;"">
                            <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #475569;"">TalentTrack Recruitment Suite</p>
                            <p style=""margin: 0 0 16px 0; font-size: 12px; color: #94a3b8; line-height: 1.5;"">This is an automated notification regarding your scheduled interview processes. Please do not reply directly to this mail.</p>
                            <div style=""margin-top: 16px; font-size: 12px; color: #cbd5e1;"">&copy; {currentYear} {companyName}. All rights reserved.</div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private void SendSimulatedEmail(string toEmail, string subject, string body)
        {
            try
            {
                Console.WriteLine($"[LOG] Email Sent to {toEmail}: {subject}");
            }
            catch (Exception)
            {
                Console.WriteLine($"[LOG] Email Failed to {toEmail}");
                throw;
            }
        }

        private List<Candidate> GetEligibleCandidatesForInterview()
        {
            var applications = _context.CandidateApplications
                .Where(ca => ca.Status == "Screened" || ca.Status == "Pending")
                .Include(ca => ca.Candidate)
                .Include(ca => ca.Job)
                    .ThenInclude(j => j!.JobSkills)
                .ToList();

            var eligibleCandidates = new List<Candidate>();

            foreach (var app in applications)
            {
                if (app.Candidate == null) continue;

                var screening = _context.Screenings
                    .Where(s => s.ApplicationId == app.ApplicationId && s.Status == "Completed")
                    .OrderByDescending(s => s.ScreeningDate)
                    .FirstOrDefault();

                if (screening == null) continue;

                bool skillsMatch = true;
                if (app.Job != null && app.Job.JobSkills != null)
                {
                    var evaluations = _context.ScreeningSkillEvaluations
                        .Where(sse => sse.ScreeningId == screening.ScreeningId)
                        .ToList();

                    foreach (var requiredSkill in app.Job.JobSkills)
                    {
                        var matchingEval = evaluations.FirstOrDefault(e => e.SkillName.Trim().ToLower() == requiredSkill.SkillName.Trim().ToLower());
                        if (matchingEval == null || !matchingEval.HasSkill || matchingEval.ExperienceYears < requiredSkill.RequiredExperience)
                        {
                            skillsMatch = false;
                            break;
                        }
                    }
                }

                if (skillsMatch)
                {
                    eligibleCandidates.Add(app.Candidate);
                }
            }

            return eligibleCandidates.GroupBy(c => c.CandidateId).Select(g => g.First()).ToList();
        }
    }
}
