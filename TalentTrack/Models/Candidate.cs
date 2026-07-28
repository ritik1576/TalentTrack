using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class Candidate
    {
        [Key]
        public int CandidateId { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Skills { get; set; }

        public int? Experience { get; set; }

        public string? Resume { get; set; }
    }
}