using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TalentTrack.Models
{
    public class Interview
    {
        [Key]
        public int InterviewId { get; set; }

        [Required]
        public int CandidateId { get; set; }

        public int? JobId { get; set; }

        public int Duration { get; set; } = 30; // in minutes
        public string? MeetingLink { get; set; }

        public ICollection<InterviewParticipant> Participants { get; set; } = new List<InterviewParticipant>();

        [Required]
        public DateTime InterviewDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled

        [StringLength(50)]
        public string? Mode { get; set; } // Online, Offline

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;



        public virtual Candidate? Candidate { get; set; }
        public virtual Job? Job { get; set; }
        public virtual ICollection<InterviewFeedback> Feedbacks { get; set; } = new List<InterviewFeedback>();
        public virtual ICollection<Interviewer> Interviewers { get; set; } = new List<Interviewer>();
        public IEnumerable<Recruiter> technicalInterviewers()
        {
            return Participants
                .Where(p => p.Role == "technical_interviewer" && p.Recruiter != null)
                .Select(p => p.Recruiter!);
        }

        public IEnumerable<Recruiter> recruiters()
        {
            return Participants
                .Where(p => p.Role == "recruiter" && p.Recruiter != null)
                .Select(p => p.Recruiter!);
        }

        public IEnumerable<InterviewParticipant> participants()
        {
            return Participants;
        }
    }
}