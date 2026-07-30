using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class Screening
    {
        [Key]
        public int ScreeningId { get; set; }

        public int ApplicationId { get; set; }

        public int ScreenedByUserId { get; set; } // UserAccount who performed screening

        public DateTime ScreeningDate { get; set; } = DateTime.Now;

        // Status: "Pending", "In Progress", "Completed"
        public string Status { get; set; } = "Pending";

        public string? Notes { get; set; }

        // Navigation
        public virtual CandidateApplication? Application { get; set; }
        public virtual ICollection<ScreeningSkillEvaluation> SkillEvaluations { get; set; } = new List<ScreeningSkillEvaluation>();
    }
}
