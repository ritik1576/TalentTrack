using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentTrack.Data;
using TalentTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TalentTrack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FeedbackApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /api/feedbackapi/jobs/{jobId}/feedback-criteria
        [HttpGet("jobs/{jobId}/feedback-criteria")]
        [Route("/jobs/{jobId}/feedback-criteria")] // Also map to root /jobs/{jobId}/feedback-criteria as requested
        public IActionResult GetFeedbackCriteria(int jobId)
        {
            var job = _context.Jobs.Include(j => j.JobSkills).FirstOrDefault(j => j.JobId == jobId);
            if (job == null) return NotFound(new { message = "Job not found." });

            var response = new
            {
                jobId = job.JobId,
                skills = job.JobSkills.Select(js => new
                {
                    id = js.JobSkillId,
                    name = js.SkillName,
                    source = js.Source
                }).ToList()
            };

            return Ok(response);
        }

        // GET: /api/feedbackapi/feedback/{feedbackId}
        [HttpGet("feedback/{feedbackId:int}")]
        [Route("/feedback/{feedbackId:int}")] // Also map to root /feedback/{feedbackId} as requested
        public IActionResult GetFeedbackDetails(int feedbackId)
        {
            var fb = _context.InterviewFeedbacks
                .Include(f => f.SkillRatings)
                .Include(f => f.Candidate)
                .Include(f => f.Interviewer)
                .FirstOrDefault(f => f.FeedbackId == feedbackId);

            if (fb == null) return NotFound(new { message = "Feedback not found." });

            var response = new
            {
                feedbackId = fb.FeedbackId,
                candidateId = fb.CandidateId,
                jobId = fb.Interview?.JobId ?? 0,
                interviewerId = fb.InterviewerId,
                overallComments = fb.Comments,
                finalRecommendation = fb.Recommendation,
                createdAt = fb.CreatedAt,
                updatedAt = fb.CreatedAt,
                skills = fb.SkillRatings.Select(sr => new
                {
                    id = sr.FeedbackSkillRatingId,
                    jobSkillId = sr.JobSkillId,
                    skill = sr.SkillName,
                    rating = sr.Rating,
                    comment = sr.Comment
                }).ToList()
            };

            return Ok(response);
        }

        // POST: /feedback
        [HttpPost]
        [Route("/feedback")] // Map to root /feedback as requested
        public IActionResult SubmitFeedback([FromBody] PostFeedbackDto dto)
        {
            // 1. Authentication check
            var role = HttpContext.Session.GetString("UserRole");
            var idStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(role) || role != "Interviewer" || !int.TryParse(idStr, out var interviewerId))
            {
                return Unauthorized(new { message = "Unauthorized. Only interviewers can submit feedback." });
            }

            // 2. Validation
            if (dto == null) return BadRequest(new { message = "Invalid payload." });
            if (dto.CandidateId <= 0 || dto.JobId <= 0) return BadRequest(new { message = "CandidateId and JobId are required." });
            if (string.IsNullOrWhiteSpace(dto.OverallComments)) return BadRequest(new { message = "Overall comments are required." });

            // Validate ratings (1-5)
            foreach (var s in dto.Skills)
            {
                if (s.Rating < 1 || s.Rating > 5)
                {
                    return BadRequest(new { message = $"Rating for skill '{s.Skill}' must be between 1 and 5." });
                }
                if (string.IsNullOrWhiteSpace(s.Skill))
                {
                    return BadRequest(new { message = "Skill name is required." });
                }
            }

            // Prevent duplicate skills in payload
            var uniqueSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in dto.Skills)
            {
                if (!uniqueSkills.Add(s.Skill))
                {
                    return BadRequest(new { message = $"Duplicate skill '{s.Skill}' detected in payload." });
                }
            }

            // Validate candidate-job relationship (check if candidate has applied or is scheduled for this job)
            var interview = _context.Interviews
                .Include(i => i.Interviewers)
                .FirstOrDefault(i => i.CandidateId == dto.CandidateId && i.JobId == dto.JobId && i.Interviewers.Any(iv => iv.InterviewerId == interviewerId));

            if (interview == null)
            {
                return BadRequest(new { message = "No assigned interview found for this interviewer, candidate, and job." });
            }

            // Check if feedback already submitted
            var existing = _context.InterviewFeedbacks
                .FirstOrDefault(f => f.InterviewId == interview.InterviewId && f.InterviewerId == interviewerId);
            if (existing != null)
            {
                return BadRequest(new { message = "Feedback already submitted for this interview." });
            }

            // Calculate overall rating from skills average, or default to 3
            int overallRating = dto.Skills.Any() ? (int)Math.Round(dto.Skills.Average(s => s.Rating)) : 3;

            // Technical, communication, problem solving averages
            int tech = dto.Skills.Where(s => s.Skill.Contains("Java") || s.Skill.Contains("SQL") || s.Skill.Contains("AWS") || s.Skill.Contains("Spring") || s.Skill.Contains("Docker")).Select(s => s.Rating * 2).DefaultIfEmpty(6).Max();
            int comm = 8;
            int prob = dto.Skills.Select(s => s.Rating * 2).DefaultIfEmpty(6).Max();

            // Create main feedback entry
            var feedback = new InterviewFeedback
            {
                InterviewId = interview.InterviewId,
                CandidateId = dto.CandidateId,
                InterviewerId = interviewerId,
                OverallRating = overallRating,
                TechnicalScore = Math.Min(tech, 10),
                CommunicationScore = comm,
                ProblemSolvingScore = Math.Min(prob, 10),
                Comments = dto.OverallComments,
                Recommendation = dto.FinalRecommendation,
                CreatedAt = DateTime.Now
            };

            _context.InterviewFeedbacks.Add(feedback);
            _context.SaveChanges();

            // Save skill ratings
            foreach (var s in dto.Skills)
            {
                // Verify jobSkillId exists and belongs to the job if provided
                int? validJobSkillId = null;
                if (s.JobSkillId.HasValue && s.JobSkillId.Value > 0)
                {
                    var js = _context.JobSkills.FirstOrDefault(x => x.JobSkillId == s.JobSkillId.Value && x.JobId == dto.JobId);
                    if (js != null)
                    {
                        validJobSkillId = js.JobSkillId;
                    }
                }

                // If not found by ID but name matches, check existing job skill by name
                if (validJobSkillId == null)
                {
                    var js = _context.JobSkills.FirstOrDefault(x => x.SkillName.ToLower() == s.Skill.ToLower() && x.JobId == dto.JobId);
                    if (js != null)
                    {
                        validJobSkillId = js.JobSkillId;
                    }
                    else
                    {
                        // If it's a manual skill not currently in job_skills, add it to JobSkills with source = 'manual'
                        var manualSkill = new JobSkill
                        {
                            JobId = dto.JobId,
                            SkillName = s.Skill,
                            RequiredExperience = 1,
                            Source = "manual",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        _context.JobSkills.Add(manualSkill);
                        _context.SaveChanges();
                        validJobSkillId = manualSkill.JobSkillId;
                    }
                }

                _context.FeedbackSkillRatings.Add(new FeedbackSkillRating
                {
                    FeedbackId = feedback.FeedbackId,
                    JobSkillId = validJobSkillId,
                    SkillName = s.Skill,
                    Rating = s.Rating,
                    Comment = s.Comment,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            // Update interview status to Completed
            interview.Status = "Completed";
            _context.Interviews.Update(interview);

            // Add notification
            var candidate = _context.Candidates.Find(dto.CandidateId);
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

            return Created($"/feedback/{feedback.FeedbackId}", new { message = "Feedback submitted successfully.", feedbackId = feedback.FeedbackId });
        }
    }

    public class PostFeedbackDto
    {
        public int CandidateId { get; set; }
        public int JobId { get; set; }
        public string OverallComments { get; set; } = "";
        public string FinalRecommendation { get; set; } = "Recommended";
        public List<FeedbackSkillDto> Skills { get; set; } = new List<FeedbackSkillDto>();
    }

    public class FeedbackSkillDto
    {
        public int? JobSkillId { get; set; }
        public string Skill { get; set; } = "";
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
