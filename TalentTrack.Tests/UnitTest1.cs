using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using TalentTrack.Controllers;
using TalentTrack.Data;
using TalentTrack.Models;
using TalentTrack.Services;

namespace TalentTrack.Tests
{
    public class PanelInterviewTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options);
        }

        private DefaultHttpContext GetMockHttpContext(string role, string email)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new MockSession();
            httpContext.Session.SetString("UserRole", role);
            httpContext.Session.SetString("UserEmail", email);
            httpContext.Session.SetString("UserId", "1");
            return httpContext;
        }

        private InterviewController GetController(ApplicationDbContext db, string role, string email)
        {
            var httpContext = GetMockHttpContext(role, email);
            var mockEmail = new Mock<IEmailService>();
            var controller = new InterviewController(db, mockEmail.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    httpContext,
                    new MockTempDataProvider())
            };
            return controller;
        }

        [Fact]
        public void Create_Interview_With_Multiple_Participants_Saves_Correctly()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            
            // Seed candidate, job, and participants
            var candidate = new Candidate { CandidateId = 1, Name = "Alice Smith", Email = "alice@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Engineer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            
            var recruiter = new Recruiter { RecruiterId = 10, Name = "Recruiter 1", Email = "rec1@company.com", Password = "123", Role = "Recruiter", Status = "Approved", IsApproved = true };
            var interviewer = new Recruiter { RecruiterId = 11, Name = "Interviewer 1", Email = "int1@company.com", Password = "123", Role = "Interviewer", Status = "Approved", IsApproved = true };

            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.Recruiters.AddRange(recruiter, interviewer);
            db.Interviewers.Add(new Interviewer { InterviewerId = 11, Name = "Interviewer 1", Email = "int1@company.com" });
            var app = new CandidateApplication { ApplicationId = 101, CandidateId = 1, JobId = 1, Status = "Screened" };
            var screening = new Screening { ScreeningId = 201, ApplicationId = 101, Status = "Completed", ScreeningDate = DateTime.Now };
            db.CandidateApplications.Add(app);
            db.Screenings.Add(screening);
            db.SaveChanges();

            var controller = GetController(db, "Recruiter", "rec1@company.com");

            var newInterview = new Interview
            {
                CandidateId = 1,
                JobId = null,
                InterviewDate = DateTime.Now.AddDays(2),
                Duration = 45,
                MeetingLink = null,
                Status = "Scheduled",
                Mode = "Offline",
                Notes = "Initial technical screen"
            };

            // Act
            var result = controller.Create(newInterview, recruiter.RecruiterId, new[] { interviewer.RecruiterId }).GetAwaiter().GetResult();

            // Assert
            var errors = string.Join(", ", controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            Assert.True(controller.ModelState.IsValid, $"ModelState is invalid: {errors}");

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var savedInterview = db.Interviews
                .Include(i => i.Participants)
                .Include(i => i.Interviewers)
                .FirstOrDefault();

            Assert.NotNull(savedInterview);
            Assert.Equal(45, savedInterview.Duration);
            Assert.Null(savedInterview.MeetingLink);
            Assert.Equal("Offline", savedInterview.Mode);
            Assert.Equal(1, savedInterview.JobId);
            Assert.Single(savedInterview.Participants);
            Assert.Contains(savedInterview.Participants, p => p.RecruiterId == recruiter.RecruiterId && p.Role == "recruiter");
            Assert.Single(savedInterview.Interviewers);
            Assert.Contains(savedInterview.Interviewers, iv => iv.InterviewerId == interviewer.RecruiterId);
        }

        [Fact]
        public void Create_Interview_Detects_Overlapping_Candidate_Conflict()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            
            var candidate = new Candidate { CandidateId = 1, Name = "Alice Smith", Email = "alice@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Engineer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var interviewer = new Recruiter { RecruiterId = 11, Name = "Interviewer 1", Email = "int1@company.com", Password = "123", Role = "Interviewer", Status = "Approved", IsApproved = true };
            var recruiter = new Recruiter { RecruiterId = 10, Name = "Recruiter 1", Email = "rec1@company.com", Password = "123", Role = "Recruiter", Status = "Approved", IsApproved = true };

            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.Recruiters.AddRange(interviewer, recruiter);
            db.Interviewers.Add(new Interviewer { InterviewerId = 11, Name = "Interviewer 1", Email = "int1@company.com" });
            var app = new CandidateApplication { ApplicationId = 102, CandidateId = 1, JobId = 1, Status = "Screened" };
            var screening = new Screening { ScreeningId = 202, ApplicationId = 102, Status = "Completed", ScreeningDate = DateTime.Now };
            db.CandidateApplications.Add(app);
            db.Screenings.Add(screening);
            
            // Existing interview: today at 2:00 PM, duration 60 mins (ends 3:00 PM)
            var existingDate = DateTime.Today.AddDays(1).AddHours(14);
            var existing = new Interview
            {
                CandidateId = 1,
                JobId = 1,
                InterviewDate = existingDate,
                Duration = 60,
                Status = "Scheduled"
            };
            db.Interviews.Add(existing);
            db.SaveChanges();

            var controller = GetController(db, "Recruiter", "rec1@company.com");

            // Overlapping interview request: today at 2:30 PM (starts during previous)
            var newInterview = new Interview
            {
                CandidateId = 1,
                JobId = 1,
                InterviewDate = existingDate.AddMinutes(30),
                Duration = 30,
                Status = "Scheduled"
            };

            // Act
            var result = controller.Create(newInterview, recruiter.RecruiterId, new[] { interviewer.RecruiterId }).GetAwaiter().GetResult();

            // Assert
            Assert.False(controller.ModelState.IsValid);
            var errors = controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            Assert.Contains(errors, err => err.Contains("Candidate already has another interview"));
        }

        [Fact]
        public void Create_Interview_Detects_Overlapping_Interviewer_Conflict()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            
            var candidate1 = new Candidate { CandidateId = 1, Name = "Alice Smith", Email = "alice@example.com" };
            var candidate2 = new Candidate { CandidateId = 2, Name = "Bob Jones", Email = "bob@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Engineer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var interviewer = new Recruiter { RecruiterId = 11, Name = "Interviewer 1", Email = "int1@company.com", Password = "123", Role = "Interviewer", Status = "Approved", IsApproved = true };
            var recruiter = new Recruiter { RecruiterId = 10, Name = "Recruiter 1", Email = "rec1@company.com", Password = "123", Role = "Recruiter", Status = "Approved", IsApproved = true };

            db.Candidates.AddRange(candidate1, candidate2);
            db.Jobs.Add(job);
            db.Recruiters.AddRange(interviewer, recruiter);
            db.Interviewers.Add(new Interviewer { InterviewerId = 11, Name = "Interviewer 1", Email = "int1@company.com" });
            
            var app1 = new CandidateApplication { ApplicationId = 103, CandidateId = 1, JobId = 1, Status = "Screened" };
            var screening1 = new Screening { ScreeningId = 203, ApplicationId = 103, Status = "Completed", ScreeningDate = DateTime.Now };
            var app2 = new CandidateApplication { ApplicationId = 104, CandidateId = 2, JobId = 1, Status = "Screened" };
            var screening2 = new Screening { ScreeningId = 204, ApplicationId = 104, Status = "Completed", ScreeningDate = DateTime.Now };
            db.CandidateApplications.AddRange(app1, app2);
            db.Screenings.AddRange(screening1, screening2);
 
            // Existing interview: Bob Jones at 2:00 PM, duration 60 mins. Interviewer 1 is assigned.
            var existingDate = DateTime.Today.AddDays(1).AddHours(14);
            var existing = new Interview
            {
                CandidateId = 2,
                JobId = 1,
                InterviewDate = existingDate,
                Duration = 60,
                Status = "Scheduled"
            };
            existing.Interviewers.Add(db.Interviewers.Find(11)!);
            db.Interviews.Add(existing);
            db.SaveChanges();

            var controller = GetController(db, "Recruiter", "rec1@company.com");

            // Overlapping interview request for Alice Smith: 2:45 PM. Interviewer 1 is requested.
            var newInterview = new Interview
            {
                CandidateId = 1,
                JobId = 1,
                InterviewDate = existingDate.AddMinutes(45),
                Duration = 30,
                Status = "Scheduled"
            };

            // Act
            var result = controller.Create(newInterview, recruiter.RecruiterId, new[] { interviewer.RecruiterId }).GetAwaiter().GetResult();

            // Assert
            Assert.False(controller.ModelState.IsValid);
            var errors = controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            Assert.Contains(errors, err => err.Contains("already has another interview"));
        }

        [Fact]
        public void Edit_Interview_Can_Reschedule_Without_Self_Conflict()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            
            var candidate = new Candidate { CandidateId = 1, Name = "Alice Smith", Email = "alice@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Engineer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var interviewer = new Recruiter { RecruiterId = 11, Name = "Interviewer 1", Email = "int1@company.com", Password = "123", Role = "Interviewer", Status = "Approved", IsApproved = true };
            var recruiter = new Recruiter { RecruiterId = 10, Name = "Recruiter 1", Email = "rec1@company.com", Password = "123", Role = "Recruiter", Status = "Approved", IsApproved = true };

            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.Recruiters.AddRange(interviewer, recruiter);
            db.Interviewers.Add(new Interviewer { InterviewerId = 11, Name = "Interviewer 1", Email = "int1@company.com" });
            var app = new CandidateApplication { ApplicationId = 105, CandidateId = 1, JobId = 1, Status = "Screened" };
            var screening = new Screening { ScreeningId = 205, ApplicationId = 105, Status = "Completed", ScreeningDate = DateTime.Now };
            db.CandidateApplications.Add(app);
            db.Screenings.Add(screening);
            
            var interviewDate = DateTime.Today.AddDays(1).AddHours(14);
            var interview = new Interview
            {
                InterviewId = 100,
                CandidateId = 1,
                JobId = 1,
                InterviewDate = interviewDate,
                Duration = 60,
                Status = "Scheduled"
            };
            db.Interviews.Add(interview);
            db.SaveChanges();

            db.InterviewParticipants.Add(new InterviewParticipant
            {
                InterviewId = 100,
                RecruiterId = interviewer.RecruiterId,
                Role = "technical_interviewer"
            });
            db.SaveChanges();

            var controller = GetController(db, "Recruiter", "rec1@company.com");

            // Edit request: Reschedule the SAME interview 1 hour later (15:00).
            var editRequest = new Interview
            {
                InterviewId = 100,
                CandidateId = 1,
                JobId = 1,
                InterviewDate = interviewDate.AddHours(1),
                Duration = 60,
                Status = "Scheduled"
            };

            // Act
            var result = controller.Edit(editRequest, recruiter.RecruiterId, new[] { interviewer.RecruiterId }).GetAwaiter().GetResult();

            // Assert
            var errors = string.Join(", ", controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            Assert.True(controller.ModelState.IsValid, $"ModelState is invalid: {errors}");
        }

        [Fact]
        public void Create_Interview_Checks_User_Approval_Status()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            
            var candidate = new Candidate { CandidateId = 1, Name = "Alice Smith", Email = "alice@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Engineer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            
            // Recruiter is NOT approved/active
            var recruiter = new Recruiter { RecruiterId = 10, Name = "Recruiter 1", Email = "rec1@company.com", Password = "123", Role = "Recruiter", Status = "Pending", IsApproved = false };
            var interviewer = new Recruiter { RecruiterId = 11, Name = "Interviewer 1", Email = "int1@company.com", Password = "123", Role = "Interviewer", Status = "Approved", IsApproved = true };

            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.Recruiters.AddRange(recruiter, interviewer);
            db.Interviewers.Add(new Interviewer { InterviewerId = 11, Name = "Interviewer 1", Email = "int1@company.com" });
            var app = new CandidateApplication { ApplicationId = 106, CandidateId = 1, JobId = 1, Status = "Screened" };
            var screening = new Screening { ScreeningId = 206, ApplicationId = 106, Status = "Completed", ScreeningDate = DateTime.Now };
            db.CandidateApplications.Add(app);
            db.Screenings.Add(screening);
            db.SaveChanges();

            var controller = GetController(db, "Recruiter", "rec1@company.com");

            var newInterview = new Interview
            {
                CandidateId = 1,
                JobId = 1,
                InterviewDate = DateTime.Now.AddDays(2),
                Duration = 45,
                Status = "Scheduled"
            };

            // Act
            var result = controller.Create(newInterview, recruiter.RecruiterId, new[] { interviewer.RecruiterId }).GetAwaiter().GetResult();

            // Assert
            Assert.False(controller.ModelState.IsValid);
            var errors = controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            Assert.Contains(errors, err => err.Contains("not an active, approved Recruiter"));
        }
    }

    // Mock ISession implementation
    public class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new(StringComparer.OrdinalIgnoreCase);

        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    // Mock ITempDataProvider
    public class MockTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
