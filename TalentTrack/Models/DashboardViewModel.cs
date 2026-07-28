namespace TalentTrack.Models
{
    public class DashboardViewModel
    {
        public int TotalJobs { get; set; }
        public int TotalCandidates { get; set; }
        public int TotalInterviews { get; set; }
        public int OpenPositions { get; set; }

        public List<Job> RecentJobs { get; set; } = new();
        public List<Candidate> RecentCandidates { get; set; } = new();
        public List<Interview> UpcomingInterviews { get; set; } = new();
        public List<Interview> TodayInterviews { get; set; } = new();
    }

    public class InterviewerDashboardViewModel
    {
        public Interviewer? CurrentInterviewer { get; set; }
        public List<Interview> TodayInterviews { get; set; } = new();
        public List<Interview> UpcomingInterviews { get; set; } = new();
        public int TotalInterviewsConducted { get; set; }
        public int FeedbacksGiven { get; set; }
    }
}
