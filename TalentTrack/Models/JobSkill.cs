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

        // Navigation
        public virtual Job? Job { get; set; }
    }
}
