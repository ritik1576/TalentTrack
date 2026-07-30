using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class ScreeningSkillEvaluation
    {
        [Key]
        public int EvaluationId { get; set; }

        public int ScreeningId { get; set; }

        public string SkillName { get; set; } = "";

        public bool HasSkill { get; set; } = false;

        public int? ExperienceYears { get; set; }

        // Navigation
        public virtual Screening? Screening { get; set; }
    }
}
