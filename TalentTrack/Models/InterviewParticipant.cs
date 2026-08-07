using System;
using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class InterviewParticipant
    {
        [Key]
        public int InterviewParticipantId { get; set; }

        public int InterviewId { get; set; }
        public int RecruiterId { get; set; }

        [Required]
        public string Role { get; set; } = "technical_interviewer"; // "recruiter" or "technical_interviewer"

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Interview? Interview { get; set; }
        public virtual Recruiter? Recruiter { get; set; }
    }
}
