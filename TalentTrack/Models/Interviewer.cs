using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class Interviewer
    {
        [Key]
        public int InterviewerId { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Email { get; set; } = "";

        public string Password { get; set; } = "interviewer123";

        public string? Phone { get; set; }

        public string? Department { get; set; }

        public string? Specialization { get; set; }

        public string? LinkedIn { get; set; }

        public string? Bio { get; set; }

        public int? YearsExperience { get; set; }

        public string? AvatarInitials { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<Interview> Interviews { get; set; } = new List<Interview>();
        public virtual ICollection<InterviewFeedback> Feedbacks { get; set; } = new List<InterviewFeedback>();
    }
}
