using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class JobSkill
    {
        [Key]
        public int JobSkillId { get; set; }

        public int JobId { get; set; }

        public string SkillName { get; set; } = "";

        public int RequiredExperience { get; set; } // Years

        public string Source { get; set; } = "jd_extracted"; // jd_extracted, manual

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Job? Job { get; set; }
    }
}
