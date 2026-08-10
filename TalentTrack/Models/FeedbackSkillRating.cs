using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentTrack.Models
{
    public class FeedbackSkillRating
    {
        [Key]
        public int FeedbackSkillRatingId { get; set; }

        public int FeedbackId { get; set; }

        public int? JobSkillId { get; set; }

        [Required]
        public string SkillName { get; set; } = "";

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("FeedbackId")]
        public virtual InterviewFeedback? Feedback { get; set; }

        [ForeignKey("JobSkillId")]
        public virtual JobSkill? JobSkill { get; set; }
    }
}
