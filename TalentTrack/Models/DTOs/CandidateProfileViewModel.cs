using TalentTrack.Models;

namespace TalentTrack.Models.DTOs
{
    public class CandidateProfileViewModel
    {
        public Candidate Candidate { get; set; } = null!;
        public List<CandidateSkill> Skills { get; set; } = new();
        public List<CandidateApplication> Applications { get; set; } = new();
    }
}
