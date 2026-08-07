using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var screenedCandidates = _context.CandidateApplications
                .Where(ca => ca.Status == "Screened" || ca.Status == "Pending")
                .Include(ca => ca.Candidate)
                .Select(ca => ca.Candidate)
                .Distinct()
                .ToList();

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
        public IActionResult Create(Interview interview, int? SelectedRecruiterId, int[] SelectedInterviewers)
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

                    SendNotificationsAndMails(interview, "Invitation: New Panel Interview Scheduled");
                    
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
            var screenedCandidates = _context.CandidateApplications
                .Where(ca => ca.Status == "Screened" || ca.Status == "Pending")
                .Include(ca => ca.Candidate)
                .Select(ca => ca.Candidate)
                .Distinct()
                .ToList();

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

            var screenedCandidates = _context.CandidateApplications
                .Where(ca => ca.Status == "Screened" || ca.Status == "Pending")
                .Include(ca => ca.Candidate)
                .Select(ca => ca.Candidate)
                .Distinct()
                .ToList();

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
        public IActionResult Edit(Interview interview, int? SelectedRecruiterId, int[] SelectedInterviewers)
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
                        SendNotificationsAndMails(existingInterview, "Cancellation: Interview Cancelled", isCancel: true);
                        Console.WriteLine("[LOG] Interview Cancelled");
                    }
                    else
                    {
                        string subject = isRescheduled ? "Reschedule: Interview Rescheduled" : "Update: Interview Details Updated";
                        SendNotificationsAndMails(existingInterview, subject);
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
            var screenedCandidates = _context.CandidateApplications
                .Where(ca => ca.Status == "Screened")
                .Include(ca => ca.Candidate)
                .Select(ca => ca.Candidate)
                .Distinct()
                .ToList();

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
        public IActionResult Cancel(int id)
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

                    SendNotificationsAndMails(interview, "Cancellation: Interview Cancelled", isCancel: true);
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
                var hasScreening = _context.Screenings
                    .Include(s => s.Application)
                    .Any(s => s.Application != null && s.Application.CandidateId == interview.CandidateId && s.Status == "Completed");
                if (!hasScreening)
                {
                    ModelState.AddModelError("CandidateId", "Interview scheduling is not allowed until Screening is completed.");
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

        private void SendNotificationsAndMails(Interview interview, string subject, bool isCancel = false)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG] Email Failed: {ex.Message}");
            }
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
    }
}
