using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class InterviewFeedback
    {
        [Key]
        public int FeedbackId { get; set; }

        public int InterviewId { get; set; }

        public int CandidateId { get; set; }

        public int InterviewerId { get; set; }

        [Range(1, 5)]
        public int OverallRating { get; set; }  // 1-5 stars

        [Range(1, 10)]
        public int TechnicalScore { get; set; }  // 1-10

        [Range(1, 10)]
        public int CommunicationScore { get; set; }  // 1-10

        [Range(1, 10)]
        public int ProblemSolvingScore { get; set; }  // 1-10

        public string? Strengths { get; set; }

        public string? Weaknesses { get; set; }

        public string? Comments { get; set; }

        // Hire / Reject / Hold
        public string Recommendation { get; set; } = "Hold";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Interview? Interview { get; set; }
        public virtual Candidate? Candidate { get; set; }
        public virtual Interviewer? Interviewer { get; set; }
    }
}
