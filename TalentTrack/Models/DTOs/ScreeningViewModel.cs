using TalentTrack.Models;

namespace TalentTrack.Models.DTOs
{
    public class ScreeningViewModel
    {
        public CandidateApplication Application { get; set; } = null!;
        public Candidate Candidate { get; set; } = null!;
        public Job Job { get; set; } = null!;
        public List<JobSkill> RequiredSkills { get; set; } = new();
        public List<CandidateSkill> CandidateSkills { get; set; } = new();

        // Skill evaluation inputs from the form
        public List<ScreeningSkillInput> SkillInputs { get; set; } = new();

        // Duplicate detection flag
        public bool HasPreviousScreening { get; set; }

        public string? Notes { get; set; }
    }

    public class ScreeningSkillInput
    {
        public string SkillName { get; set; } = "";
        public bool HasSkill { get; set; }
        public int ExperienceYears { get; set; }
    }
}
