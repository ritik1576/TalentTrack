using System;
using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class Interview
    {
        [Key]
        public int InterviewId { get; set; }

        public int CandidateId { get; set; }

        public int JobId { get; set; }

        public int? InterviewerId { get; set; }

        public DateTime InterviewDate { get; set; }

        public string Status { get; set; } = "Scheduled";

        public string? Mode { get; set; }  // Online / Offline / Hybrid

        public string? Notes { get; set; }

        // Relationships
        public virtual Candidate? Candidate { get; set; }
        public virtual Job? Job { get; set; }
        public virtual Interviewer? Interviewer { get; set; }
        public virtual ICollection<InterviewFeedback> Feedbacks { get; set; } = new List<InterviewFeedback>();
    }
}