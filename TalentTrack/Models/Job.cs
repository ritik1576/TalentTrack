using System.ComponentModel.DataAnnotations;

namespace TalentTrack.Models
{
    public class Job
    {
        [Key]
        public int JobId { get; set; }

        public string JobTitle { get; set; }

        public string Description { get; set; }

        public string Skills { get; set; }

        public string Experience { get; set; }

        public string Location { get; set; }

        public string Status { get; set; }
    }
}