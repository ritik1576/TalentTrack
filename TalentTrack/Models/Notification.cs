using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public string TargetRole { get; set; } = ""; // "Admin", "Recruiter", "Interviewer", or "All"

        public string? TargetUserEmail { get; set; }

        public string Title { get; set; } = "";

        public string Message { get; set; } = "";

        public bool IsRead { get; set; } = false;

        public string? TargetUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
