using System;
using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class OfferLetter
    {
        [Key]
        public int OfferLetterId { get; set; }

        [Required]
        public int CandidateId { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public DateTime JoiningDate { get; set; }

        [Required]
        public DateTime OfferDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = "";

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = ""; // StorageReference

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Generated"; // "Generated", "Sent"

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual Candidate? Candidate { get; set; }
        public virtual CandidateApplication? Application { get; set; }
    }
}
