using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class CandidateApplication
    {
        [Key]
        public int ApplicationId { get; set; }

        public int CandidateId { get; set; }

        public int JobId { get; set; }

        public DateTime AppliedDate { get; set; } = DateTime.Now;

        // Status: "Applied", "Screening", "Screened", "Interview", "Rejected"
        public string Status { get; set; } = "Applied";

        [Required]
        [MaxLength(50)]
        public string BackgroundVerificationStatus { get; set; } = "Pending";

        // Navigation
        public virtual Candidate? Candidate { get; set; }
        public virtual Job? Job { get; set; }
    }
}
