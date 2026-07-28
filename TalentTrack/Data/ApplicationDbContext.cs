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

            // Relationships
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
        }
    }
}