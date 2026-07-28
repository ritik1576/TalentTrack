using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class UserAccount
    {
        [Key]
        public int UserAccountId { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
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

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
