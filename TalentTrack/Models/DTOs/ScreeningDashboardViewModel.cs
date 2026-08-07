namespace TalentTrack.Models.DTOs
{
    public class ScreeningDashboardViewModel
    {
        public List<ScreeningDashboardItem> Applications { get; set; } = new();
    }

    public class ScreeningDashboardItem
    {
        public int ApplicationId { get; set; }
        public string CandidateName { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public DateTime AppliedDate { get; set; }
        public string ApplicationStatus { get; set; } = "";
        public string ScreeningStatus { get; set; } = "Not Started";
        public bool IsDuplicate { get; set; }
        public string DuplicateReason { get; set; } = "";
    }
}
