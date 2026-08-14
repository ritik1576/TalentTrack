using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalentTrack.Controllers;
using TalentTrack.Data;
using TalentTrack.Models;

namespace TalentTrack.Tests
{
    public class CandidateDocumentTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options);
        }

        private DefaultHttpContext GetMockHttpContext(string role, string email, string userId)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new MockSession();
            if (role != null) httpContext.Session.SetString("UserRole", role);
            if (email != null) httpContext.Session.SetString("UserEmail", email);
            if (userId != null) httpContext.Session.SetString("UserId", userId);
            return httpContext;
        }

        private CandidateDocumentController GetController(ApplicationDbContext db, string role, string email, string userId, string webRootPath = null)
        {
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns(webRootPath ?? Path.GetTempPath());

            var httpContext = GetMockHttpContext(role, email, userId);
            var controller = new CandidateDocumentController(db, mockEnv.Object)
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

        private FeedbackController GetFeedbackController(ApplicationDbContext db, string role, string email, string userId)
        {
            var httpContext = GetMockHttpContext(role, email, userId);
            var controller = new FeedbackController(db)
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

        private IFormFile CreateMockFile(string fileName, long length, string content = "test")
        {
            var fileMock = new Mock<IFormFile>();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(length);
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns((Stream stream, System.Threading.CancellationToken token) =>
                {
                    ms.CopyTo(stream);
                    return Task.CompletedTask;
                });
            return fileMock.Object;
        }

        private void SeedShortlistedApplication(ApplicationDbContext db, int candidateId)
        {
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication { ApplicationId = 1000 + candidateId, CandidateId = candidateId, JobId = 1, Status = "Screened" };
            if (!db.Jobs.Any(j => j.JobId == 1))
            {
                db.Jobs.Add(job);
            }
            db.CandidateApplications.Add(app);
            db.SaveChanges();
        }

        [Fact]
        public async Task Candidate_Login_With_Valid_Email_And_Phone_Succeeds()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com", Phone = "9876543210" };
            db.Candidates.Add(candidate);
            db.SaveChanges();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new MockSession();
            var controller = new AccountController(db)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            // Act
            var result = controller.Login("kajal@example.com", "9876543210");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Portal", redirectResult.ActionName);
            Assert.Equal("CandidateDocument", redirectResult.ControllerName);
            Assert.Equal("Candidate", httpContext.Session.GetString("UserRole"));
            Assert.Equal("12", httpContext.Session.GetString("UserId"));
        }

        [Fact]
        public async Task Candidate_Can_Upload_Valid_Document()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com", Phone = "9876543210" };
            db.Candidates.Add(candidate);
            db.SaveChanges();
            SeedShortlistedApplication(db, 12);

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var controller = GetController(db, "Candidate", "kajal@example.com", "12", tempDir);
            var mockFile = CreateMockFile("id_proof.pdf", 1024);

            // Act
            var result = await controller.Upload("ID Proof", mockFile);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Portal", redirectResult.ActionName);

            var savedDoc = db.CandidateDocuments.FirstOrDefault(d => d.CandidateId == 12 && d.DocumentType == "ID Proof");
            Assert.NotNull(savedDoc);
            Assert.Equal("id_proof.pdf", savedDoc.FileName);
            Assert.Equal("Uploaded", savedDoc.Status);
            
            // Clean up
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task Invalid_File_Extension_Is_Rejected()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com", Phone = "9876543210" };
            db.Candidates.Add(candidate);
            db.SaveChanges();
            SeedShortlistedApplication(db, 12);

            var controller = GetController(db, "Candidate", "kajal@example.com", "12");
            var mockFile = CreateMockFile("virus.exe", 1024);

            // Act
            var result = await controller.Upload("ID Proof", mockFile);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var savedDoc = db.CandidateDocuments.FirstOrDefault(d => d.CandidateId == 12 && d.DocumentType == "ID Proof");
            Assert.Null(savedDoc);
            Assert.Equal("Only PDF, JPG, JPEG, and PNG files are allowed.", controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Oversized_File_Is_Rejected()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com", Phone = "9876543210" };
            db.Candidates.Add(candidate);
            db.SaveChanges();
            SeedShortlistedApplication(db, 12);

            var controller = GetController(db, "Candidate", "kajal@example.com", "12");
            // File size: 6MB (exceeds 5MB limit)
            var mockFile = CreateMockFile("large.pdf", 6 * 1024 * 1024);

            // Act
            var result = await controller.Upload("ID Proof", mockFile);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var savedDoc = db.CandidateDocuments.FirstOrDefault(d => d.CandidateId == 12 && d.DocumentType == "ID Proof");
            Assert.Null(savedDoc);
            Assert.Equal("File size exceeds 5MB limit.", controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Candidate_Can_Replace_Existing_Document()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com", Phone = "9876543210" };
            var existingDoc = new CandidateDocument
            {
                CandidateId = 12,
                DocumentType = "ID Proof",
                FileName = "old_id.jpg",
                FilePath = "/uploads/documents/old.jpg",
                Status = "Uploaded",
                CreatedAt = DateTime.Now.AddDays(-1)
            };
            db.Candidates.Add(candidate);
            db.CandidateDocuments.Add(existingDoc);
            db.SaveChanges();
            SeedShortlistedApplication(db, 12);

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var controller = GetController(db, "Candidate", "kajal@example.com", "12", tempDir);
            var mockFile = CreateMockFile("new_id.pdf", 2048);

            // Act
            var result = await controller.Upload("ID Proof", mockFile);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            
            var docsList = db.CandidateDocuments.Where(d => d.CandidateId == 12 && d.DocumentType == "ID Proof").ToList();
            Assert.Single(docsList); // Ensure no duplicate records created
            
            var savedDoc = docsList.First();
            Assert.Equal("new_id.pdf", savedDoc.FileName);
            Assert.NotNull(savedDoc.UpdatedAt);

            // Clean up
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task Candidate_Cannot_Download_Other_Candidates_Document()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var doc = new CandidateDocument
            {
                DocumentId = 100,
                CandidateId = 55, // belongs to candidate 55
                DocumentType = "ID Proof",
                FileName = "secret.pdf",
                FilePath = "/uploads/documents/secret.pdf",
                Status = "Uploaded"
            };
            db.CandidateDocuments.Add(doc);
            db.SaveChanges();
            SeedShortlistedApplication(db, 12);

            // Logged in as Candidate 12 trying to download Candidate 55's doc
            var controller = GetController(db, "Candidate", "kajal@example.com", "12");

            // Act
            var result = await controller.Download(100);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Recruiter_Can_View_Shortlisted_Candidates()
        {
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication { ApplicationId = 101, CandidateId = 12, JobId = 1, Status = "Screened", BackgroundVerificationStatus = "Pending" };
            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.CandidateApplications.Add(app);
            await db.SaveChangesAsync();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2");

            var result = await controller.Shortlisted();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<CandidateApplication>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal(101, model.First().ApplicationId);
        }

        [Fact]
        public async Task Recruiter_Can_View_Candidate_Verification_Page()
        {
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication { ApplicationId = 101, CandidateId = 12, JobId = 1, Status = "Screened" };
            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.CandidateApplications.Add(app);
            await db.SaveChangesAsync();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2");

            var result = await controller.Verify(101);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(controller.ViewBag.Application);
            Assert.NotNull(controller.ViewBag.Candidate);
        }

        [Fact]
        public async Task Recruiter_Can_Download_Candidate_Document_Securely()
        {
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var fileName = "id.pdf";
            var filePath = Path.Combine(tempDir, "uploads", "documents");
            Directory.CreateDirectory(filePath);
            var fullPath = Path.Combine(filePath, "test_id.pdf");
            await File.WriteAllTextAsync(fullPath, "pdf-content");

            var doc = new CandidateDocument
            {
                DocumentId = 100,
                CandidateId = 12,
                DocumentType = "ID Proof",
                FileName = fileName,
                FilePath = "/uploads/documents/test_id.pdf",
                Status = "Uploaded"
            };
            db.Candidates.Add(candidate);
            db.CandidateDocuments.Add(doc);
            await db.SaveChangesAsync();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2", tempDir);

            var result = await controller.VerifyDownload(100, 12);

            var fileResult = Assert.IsType<PhysicalFileResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("id.pdf", fileResult.FileDownloadName);

            // Clean up
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Theory]
        [InlineData("In Review")]
        [InlineData("Verified")]
        [InlineData("Rejected")]
        public async Task Recruiter_Can_Update_Background_Verification_Status(string targetStatus)
        {
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel" };
            var app = new CandidateApplication { ApplicationId = 101, CandidateId = 12, JobId = 1, Status = "Screened", BackgroundVerificationStatus = "Pending" };
            db.Candidates.Add(candidate);
            db.CandidateApplications.Add(app);
            await db.SaveChangesAsync();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2");

            var result = await controller.UpdateVerificationStatus(101, targetStatus);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Verify", redirectResult.ActionName);
            Assert.Equal(101, redirectResult.RouteValues["id"]);

            var updatedApp = await db.CandidateApplications.FindAsync(101);
            Assert.Equal(targetStatus, updatedApp.BackgroundVerificationStatus);
        }

        [Fact]
        public async Task Interviewer_Is_Denied_Access_To_Verification_Page()
        {
            using var db = GetInMemoryDbContext();
            var controller = GetController(db, "Interviewer", "interviewer@example.com", "3");

            var resultShortlisted = await controller.Shortlisted();
            var redirectResult1 = Assert.IsType<RedirectToActionResult>(resultShortlisted);
            Assert.Equal("Login", redirectResult1.ActionName);

            var resultVerify = await controller.Verify(101);
            var redirectResult2 = Assert.IsType<RedirectToActionResult>(resultVerify);
            Assert.Equal("Login", redirectResult2.ActionName);

            var resultStatus = await controller.UpdateVerificationStatus(101, "Verified");
            Assert.IsType<ForbidResult>(resultStatus);

            var resultDownload = await controller.VerifyDownload(100, 12);
            Assert.IsType<ForbidResult>(resultDownload);
        }

        [Fact]
        public async Task Candidate_Is_Denied_Access_To_Verification_Page()
        {
            using var db = GetInMemoryDbContext();
            var controller = GetController(db, "Candidate", "candidate@example.com", "12");

            var resultShortlisted = await controller.Shortlisted();
            var redirectResult1 = Assert.IsType<RedirectToActionResult>(resultShortlisted);
            Assert.Equal("Login", redirectResult1.ActionName);

            var resultVerify = await controller.Verify(101);
            var redirectResult2 = Assert.IsType<RedirectToActionResult>(resultVerify);
            Assert.Equal("Login", redirectResult2.ActionName);

            var resultStatus = await controller.UpdateVerificationStatus(101, "Verified");
            Assert.IsType<ForbidResult>(resultStatus);

            var resultDownload = await controller.VerifyDownload(100, 12);
            Assert.IsType<ForbidResult>(resultDownload);
        }

        [Fact]
        public async Task Unauthenticated_User_Is_Denied()
        {
            using var db = GetInMemoryDbContext();
            // Get controller with empty role/email/userId
            var controller = GetController(db, null, null, null);

            var resultShortlisted = await controller.Shortlisted();
            var redirectResult1 = Assert.IsType<RedirectToActionResult>(resultShortlisted);
            Assert.Equal("Login", redirectResult1.ActionName);

            var resultVerify = await controller.Verify(101);
            var redirectResult2 = Assert.IsType<RedirectToActionResult>(resultVerify);
            Assert.Equal("Login", redirectResult2.ActionName);

            var resultStatus = await controller.UpdateVerificationStatus(101, "Verified");
            Assert.IsType<ForbidResult>(resultStatus);

            var resultDownload = await controller.VerifyDownload(100, 12);
            Assert.IsType<ForbidResult>(resultDownload);
        }

        [Fact]
        public async Task Invalid_Status_Is_Rejected()
        {
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel" };
            var app = new CandidateApplication { ApplicationId = 101, CandidateId = 12, JobId = 1, Status = "Screened", BackgroundVerificationStatus = "Pending" };
            db.Candidates.Add(candidate);
            db.CandidateApplications.Add(app);
            await db.SaveChangesAsync();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2");

            var result = await controller.UpdateVerificationStatus(101, "ABC");
            Assert.IsType<BadRequestObjectResult>(result);

            var result2 = await controller.UpdateVerificationStatus(101, "Approved");
            Assert.IsType<BadRequestObjectResult>(result2);
        }

        [Fact]
        public async Task Unauthorized_Document_Access_Is_Rejected()
        {
            using var db = GetInMemoryDbContext();
            var doc = new CandidateDocument
            {
                DocumentId = 100,
                CandidateId = 55, // Belongs to Candidate 55
                DocumentType = "ID Proof",
                FileName = "doc.pdf",
                FilePath = "/uploads/documents/doc.pdf",
                Status = "Uploaded"
            };
            db.CandidateDocuments.Add(doc);
            await db.SaveChangesAsync();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2");

            // Recruiter tries to download document 100 but passes candidateId = 12 (mismatched)
            var result = await controller.VerifyDownload(100, 12);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Candidate_Not_Shortlisted_Is_Denied_Portal_Access()
        {
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com", Phone = "9876543210" };
            db.Candidates.Add(candidate);
            db.SaveChanges();

            var controller = GetController(db, "Candidate", "kajal@example.com", "12");

            var result = await controller.Portal();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ViewBag.IsShortlisted);
        }

        [Fact]
        public async Task Candidate_Not_Shortlisted_Is_Denied_Upload()
        {
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com", Phone = "9876543210" };
            db.Candidates.Add(candidate);
            db.SaveChanges();

            var controller = GetController(db, "Candidate", "kajal@example.com", "12");
            var mockFile = CreateMockFile("id_proof.pdf", 1024);

            var result = await controller.Upload("ID Proof", mockFile);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Recruiter_Can_Shortlist_Candidate_For_Verification()
        {
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication { ApplicationId = 101, CandidateId = 12, JobId = 1, Status = "Screened" };
            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.CandidateApplications.Add(app);
            db.SaveChanges();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2");

            var result = await controller.ShortlistCandidate(101);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Shortlisted", redirectResult.ActionName);

            var updatedApp = db.CandidateApplications.Find(101);
            Assert.Equal("Pending", updatedApp.BackgroundVerificationStatus);
        }

        [Fact]
        public async Task Interviewer_Cannot_Shortlist_Candidate_For_Verification()
        {
            using var db = GetInMemoryDbContext();
            var app = new CandidateApplication { ApplicationId = 101, CandidateId = 12, JobId = 1, Status = "Screened" };
            db.CandidateApplications.Add(app);
            db.SaveChanges();

            var controller = GetController(db, "Interviewer", "interviewer@example.com", "3");

            var result = await controller.ShortlistCandidate(101);

            Assert.IsType<ChallengeResult>(result);
        }

        [Fact]
        public async Task Recruiter_Can_Accept_Feedback_And_Unlock_Document_Portal()
        {
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication { ApplicationId = 101, CandidateId = 12, JobId = 1, Status = "Screened" };
            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.CandidateApplications.Add(app);
            db.SaveChanges();

            var controller = GetFeedbackController(db, "Recruiter", "recruiter@example.com", "2");

            var result = await controller.ProcessApplication(101, "Accept");

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var updatedApp = db.CandidateApplications.Find(101);
            Assert.Equal("Pending", updatedApp.BackgroundVerificationStatus);

            var notification = db.Notifications.FirstOrDefault(n => n.TargetUserEmail == "kajal@example.com");
            Assert.NotNull(notification);
            Assert.Equal("Shortlisted for Onboarding!", notification.Title);
        }

        [Fact]
        public async Task Recruiter_Can_Reject_Candidate_From_Feedback()
        {
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var app = new CandidateApplication { ApplicationId = 101, CandidateId = 12, JobId = 1, Status = "Screened" };
            db.Candidates.Add(candidate);
            db.CandidateApplications.Add(app);
            db.SaveChanges();

            var controller = GetFeedbackController(db, "Recruiter", "recruiter@example.com", "2");

            var result = await controller.ProcessApplication(101, "Reject");

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var updatedApp = db.CandidateApplications.Find(101);
            Assert.Equal("Rejected", updatedApp.BackgroundVerificationStatus);
            Assert.Equal("Rejected", updatedApp.Status);
        }

        [Fact]
        public async Task Interviewer_Cannot_Process_Feedback_Decision()
        {
            using var db = GetInMemoryDbContext();
            var app = new CandidateApplication { ApplicationId = 101, CandidateId = 12, JobId = 1, Status = "Screened" };
            db.CandidateApplications.Add(app);
            db.SaveChanges();

            var controller = GetFeedbackController(db, "Interviewer", "interviewer@example.com", "3");

            var result = await controller.ProcessApplication(101, "Accept");

            Assert.IsType<ChallengeResult>(result);
        }
    }
}
