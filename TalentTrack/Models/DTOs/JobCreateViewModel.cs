using TalentTrack.Models;

namespace TalentTrack.Models.DTOs
{
    public class JobCreateViewModel
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = "";
        public string Description { get; set; } = "";
        public string Location { get; set; } = "";
        public string Experience { get; set; } = "";
        public string Status { get; set; } = "Active";

        // Legacy Skills field (backward compatible)
        public string Skills { get; set; } = "";

        // Phase 2: Per-skill experience
        public List<JobSkillInput> SkillEntries { get; set; } = new();

        // Available skills for the dropdown
        public static readonly string[] AvailableSkills = new[]
        {
            "Java", "MySQL", "Spring Boot", "Git", "Docker",
            "SQL", "AWS", "C#", ".NET", "React"
        };
    }

    public class JobSkillInput
    {
        public string SkillName { get; set; } = "";
        public int RequiredExperience { get; set; }
    }
}
