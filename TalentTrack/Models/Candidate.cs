using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class Candidate
    {
        [Key]
        public int CandidateId { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? Name
        {
            get => $"{FirstName} {LastName}".Trim();
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                var parts = value.Split(' ', 2);
                FirstName = parts[0];
                LastName = parts.Length > 1 ? parts[1] : "";
            }
        }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Skills { get; set; }

        public int? Experience { get; set; }

        public string? Resume { get; set; }

        // Phase 2: Navigation for per-skill experience and job applications
        public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
        public virtual ICollection<CandidateApplication> Applications { get; set; } = new List<CandidateApplication>();
    }
}