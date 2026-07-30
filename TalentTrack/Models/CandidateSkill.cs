using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class CandidateSkill
    {
        [Key]
        public int CandidateSkillId { get; set; }

        public int CandidateId { get; set; }

        public string SkillName { get; set; } = "";

        public int ExperienceYears { get; set; } // Years

        // Navigation
        public virtual Candidate? Candidate { get; set; }
    }
}
