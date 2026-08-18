using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class Recruiter
    {
        [Key]
        public int RecruiterId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name must contain only alphabets.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        // Roles: "Admin", "Recruiter", "Interviewer"
        [Required]
        public string Role { get; set; } = "Recruiter";

        public string Phone { get; set; } = "";

        public string Department { get; set; } = "";

        // Status: "Pending", "Approved", "Rejected"
        public string Status { get; set; } = "Pending";

        public bool IsApproved { get; set; } = false;

        // Metadata / Audit fields
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Relationships
        public virtual ICollection<InterviewParticipant> InterviewParticipants { get; set; } = new List<InterviewParticipant>();

        // Helper method
        public IEnumerable<Interview> interviews()
        {
            return InterviewParticipants
                .Where(ip => ip.Interview != null)
                .Select(ip => ip.Interview!);
        }
    }
}
