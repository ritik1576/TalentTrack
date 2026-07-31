using Microsoft.EntityFrameworkCore;
using TalentTrack.Models;

namespace TalentTrack.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<Interviewer> Interviewers { get; set; }
        public DbSet<InterviewFeedback> InterviewFeedbacks { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Phase 2: Screening Workflow
        public DbSet<JobSkill> JobSkills { get; set; }
        public DbSet<CandidateSkill> CandidateSkills { get; set; }
        public DbSet<CandidateApplication> CandidateApplications { get; set; }
        public DbSet<Screening> Screenings { get; set; }
        public DbSet<ScreeningSkillEvaluation> ScreeningSkillEvaluations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed User Accounts
            modelBuilder.Entity<UserAccount>().HasData(
                new UserAccount
                {
                    UserAccountId = 1,
                    Name = "System Admin",
                    Email = "admin@talenttrack.com",
                    Password = "admin123",
                    Role = "Admin",
                    Phone = "+91 99999 88888",
                    Department = "Administration",
                    Status = "Approved",
                    IsApproved = true,
                    CreatedAt = new DateTime(2026, 1, 1)
                },
                new UserAccount
                {
                    UserAccountId = 2,
                    Name = "Mansi Verma",
                    Email = "recruiter@talenttrack.com",
                    Password = "recruiter123",
                    Role = "Recruiter",
                    Phone = "+91 98765 11111",
                    Department = "Human Resources",
                    Status = "Approved",
                    IsApproved = true,
                    CreatedAt = new DateTime(2026, 1, 1)
                },
                new UserAccount
                {
                    UserAccountId = 3,
                    Name = "Priya Sharma",
                    Email = "interviewer@talenttrack.com",
                    Password = "interviewer123",
                    Role = "Interviewer",
                    Phone = "+91 98765 43210",
                    Department = "Engineering",
                    Status = "Approved",
                    IsApproved = true,
                    CreatedAt = new DateTime(2026, 1, 1)
                }
            );

            // Seed Only One Interviewer
            modelBuilder.Entity<Interviewer>().HasData(
                new Interviewer
                {
                    InterviewerId = 1,
                    Name = "Priya Sharma",
                    Email = "interviewer@talenttrack.com",
                    Password = "interviewer123",
                    Phone = "+91 98765 43210",
                    Department = "Engineering",
                    Specialization = "Full Stack Development",
                    LinkedIn = "linkedin.com/in/priyasharma",
                    Bio = "Senior Software Engineer with 8+ years of experience.",
                    YearsExperience = 8,
                    AvatarInitials = "PS",
                    CreatedAt = new DateTime(2026, 1, 1)
                }
            );

            // ===== Phase 1 Relationships =====

            modelBuilder.Entity<Interview>()
                .HasOne(i => i.Candidate)
                .WithMany()
                .HasForeignKey(i => i.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Interview>()
                .HasOne(i => i.Job)
                .WithMany()
                .HasForeignKey(i => i.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Interview>()
                .HasOne(i => i.Interviewer)
                .WithMany(iv => iv.Interviews)
                .HasForeignKey(i => i.InterviewerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InterviewFeedback>()
                .HasOne(f => f.Interview)
                .WithMany(i => i.Feedbacks)
                .HasForeignKey(f => f.InterviewId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InterviewFeedback>()
                .HasOne(f => f.Candidate)
                .WithMany()
                .HasForeignKey(f => f.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InterviewFeedback>()
                .HasOne(f => f.Interviewer)
                .WithMany(iv => iv.Feedbacks)
                .HasForeignKey(f => f.InterviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Phase 2 Relationships =====

            // JobSkill → Job
            modelBuilder.Entity<JobSkill>()
                .HasOne(js => js.Job)
                .WithMany(j => j.JobSkills)
                .HasForeignKey(js => js.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // CandidateSkill → Candidate
            modelBuilder.Entity<CandidateSkill>()
                .HasOne(cs => cs.Candidate)
                .WithMany(c => c.CandidateSkills)
                .HasForeignKey(cs => cs.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            // CandidateApplication → Candidate & Job
            modelBuilder.Entity<CandidateApplication>()
                .HasOne(ca => ca.Candidate)
                .WithMany(c => c.Applications)
                .HasForeignKey(ca => ca.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CandidateApplication>()
                .HasOne(ca => ca.Job)
                .WithMany()
                .HasForeignKey(ca => ca.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: one application per candidate per job
            modelBuilder.Entity<CandidateApplication>()
                .HasIndex(ca => new { ca.CandidateId, ca.JobId })
                .IsUnique();

            // Screening → CandidateApplication
            modelBuilder.Entity<Screening>()
                .HasOne(s => s.Application)
                .WithMany()
                .HasForeignKey(s => s.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // ScreeningSkillEvaluation → Screening
            modelBuilder.Entity<ScreeningSkillEvaluation>()
                .HasOne(se => se.Screening)
                .WithMany(s => s.SkillEvaluations)
                .HasForeignKey(se => se.ScreeningId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
