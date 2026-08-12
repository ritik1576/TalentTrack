using System;
using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class CandidateDocument
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        public int CandidateId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; } = ""; // "ID Proof", "Address Proof", "Education Certificate", "Experience Letter"

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = "";

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = "";

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Uploaded";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual Candidate? Candidate { get; set; }
    }
}
