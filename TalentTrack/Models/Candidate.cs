using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class Candidate
    {
        [Key]
        public int CandidateId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First name must contain only alphabets.")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last name must contain only alphabets.")]
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

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address.")]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Skills { get; set; }

        public int? Experience { get; set; }

        public string? Resume { get; set; }

        // Password Reset OTP
        public string? ResetOTP { get; set; }
        public DateTime? ResetOTPExpiry { get; set; }

        // Phase 2: Navigation for per-skill experience and job applications
        public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
        public virtual ICollection<CandidateApplication> Applications { get; set; } = new List<CandidateApplication>();
    }
}