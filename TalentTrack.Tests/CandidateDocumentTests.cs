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
using PdfSharp.Fonts;
using TalentTrack.Services;

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

        static CandidateDocumentTests()
        {
            try
            {
                GlobalFontSettings.FontResolver = new WindowsFontResolver();
            }
            catch (Exception)
            {
                // Ignore if already set
            }
        }

        private class WindowsFontResolver : IFontResolver
        {
            public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
            {
                if (familyName.Equals("Arial", StringComparison.OrdinalIgnoreCase))
                {
                    string faceName = "Arial";
                    if (isBold && isItalic) faceName = "ArialBoldItalic";
                    else if (isBold) faceName = "ArialBold";
                    else if (isItalic) faceName = "ArialItalic";
                    return new FontResolverInfo(faceName);
                }
                return new FontResolverInfo("Arial");
            }

            public byte[]? GetFont(string faceName)
            {
                string fontPath = faceName switch
                {
                    "ArialBold" => @"C:\Windows\Fonts\arialbd.ttf",
                    "ArialItalic" => @"C:\Windows\Fonts\ariali.ttf",
                    "ArialBoldItalic" => @"C:\Windows\Fonts\arialbi.ttf",
                    _ => @"C:\Windows\Fonts\arial.ttf"
                };

                if (!File.Exists(fontPath))
                {
                    fontPath = @"C:\Windows\Fonts\arial.ttf";
                }
                return File.ReadAllBytes(fontPath);
            }
        }

        private CandidateDocumentController GetControllerWithEmail(
            ApplicationDbContext db, 
            string role, 
            string email, 
            string userId, 
            IEmailService emailService, 
            string webRootPath = null)
        {
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns(webRootPath ?? Path.GetTempPath());

            var httpContext = GetMockHttpContext(role, email, userId);
            var controller = new CandidateDocumentController(db, mockEnv.Object, emailService)
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
        public async Task Recruiter_Can_View_Offer_Pipeline_With_Verified_Candidates()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate1 = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var candidate2 = new Candidate { CandidateId = 13, Name = "Ravi Sharma", Email = "ravi@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            
            // App1 is Verified
            var app1 = new CandidateApplication 
            { 
                ApplicationId = 101, 
                CandidateId = 12, 
                JobId = 1, 
                Status = "Screened", 
                BackgroundVerificationStatus = "Verified" 
            };
            // App2 is Pending
            var app2 = new CandidateApplication 
            { 
                ApplicationId = 102, 
                CandidateId = 13, 
                JobId = 1, 
                Status = "Screened", 
                BackgroundVerificationStatus = "Pending" 
            };

            db.Candidates.AddRange(candidate1, candidate2);
            db.Jobs.Add(job);
            db.CandidateApplications.AddRange(app1, app2);

            var offer = new OfferLetter
            {
                OfferLetterId = 1,
                CandidateId = 12,
                ApplicationId = 101,
                JoiningDate = DateTime.Today.AddDays(30),
                FileName = "offer.pdf",
                FilePath = "/uploads/offers/offer.pdf",
                Status = "Generated"
            };
            db.OfferLetters.Add(offer);
            await db.SaveChangesAsync();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2");

            // Act
            var result = await controller.Shortlisted();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<CandidateApplication>>(viewResult.Model);
            
            // Controller returns both Verified and Pending shortlisted applications
            var list = model.ToList();
            Assert.Contains(list, a => a.ApplicationId == 101);
            Assert.Contains(list, a => a.ApplicationId == 102);

            // Offer letters should be in ViewBag
            var offerLetters = Assert.IsType<List<OfferLetter>>(controller.ViewBag.OfferLetters);
            Assert.Single(offerLetters);
            Assert.Equal(1, offerLetters[0].OfferLetterId);
        }

        [Fact]
        public async Task GenerateOffer_Get_Returns_View_For_Verified_Candidate()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication 
            { 
                ApplicationId = 101, 
                CandidateId = 12, 
                JobId = 1, 
                Status = "Screened", 
                BackgroundVerificationStatus = "Verified" 
            };
            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.CandidateApplications.Add(app);
            await db.SaveChangesAsync();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2");

            // Act
            var result = await controller.GenerateOffer(101);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CandidateApplication>(viewResult.Model);
            Assert.Equal(101, model.ApplicationId);
        }

        [Fact]
        public async Task GenerateOffer_Get_Redirects_If_Candidate_Not_Verified()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication 
            { 
                ApplicationId = 101, 
                CandidateId = 12, 
                JobId = 1, 
                Status = "Screened", 
                BackgroundVerificationStatus = "Pending" // Not Verified
            };
            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.CandidateApplications.Add(app);
            await db.SaveChangesAsync();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2");

            // Act
            var result = await controller.GenerateOffer(101);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Shortlisted", redirectResult.ActionName);
            Assert.Equal("Candidate's background verification must be Verified to generate an offer.", controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task GenerateOffer_Post_Creates_Pdf_And_Sends_Email()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication 
            { 
                ApplicationId = 101, 
                CandidateId = 12, 
                JobId = 1, 
                Status = "Screened", 
                BackgroundVerificationStatus = "Verified" 
            };
            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.CandidateApplications.Add(app);
            await db.SaveChangesAsync();

            // Set up temp path for web root
            var tempWebRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempWebRoot);

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(e => e.SendEmailWithAttachmentAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = GetControllerWithEmail(db, "Recruiter", "recruiter@example.com", "2", mockEmail.Object, tempWebRoot);
            var joiningDate = DateTime.Today.AddDays(30);

            try
            {
                // Act
                var result = await controller.GenerateOffer(101, joiningDate);

                // Assert
                var redirectResult = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Shortlisted", redirectResult.ActionName);

                // Check DB
                var offer = await db.OfferLetters.FirstOrDefaultAsync(ol => ol.ApplicationId == 101);
                if (offer == null)
                {
                    Assert.Fail($"Offer is null. ErrorMessage: {controller.TempData["ErrorMessage"]}");
                }

                Assert.Equal(joiningDate, offer.JoiningDate);
                Assert.Equal("Sent", offer.Status); // Successfully sent
                Assert.True(offer.FileName.StartsWith("OfferLetter_12_"));

                // Check physical file exists in temp web root
                string physicalPath = Path.Combine(tempWebRoot, offer.FilePath.TrimStart('/'));
                Assert.True(File.Exists(physicalPath));

                // Verify email service called
                mockEmail.Verify(e => e.SendEmailWithAttachmentAsync(
                    "kajal@example.com",
                    "Offer Letter – TalentTrack",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    offer.FileName
                ), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempWebRoot))
                {
                    Directory.Delete(tempWebRoot, true);
                }
            }
        }

        [Fact]
        public async Task GenerateOffer_Post_Fails_When_JoiningDate_Is_Null()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication 
            { 
                ApplicationId = 101, 
                CandidateId = 12, 
                JobId = 1, 
                Status = "Screened", 
                BackgroundVerificationStatus = "Verified" 
            };
            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.CandidateApplications.Add(app);
            await db.SaveChangesAsync();

            var controller = GetController(db, "Recruiter", "recruiter@example.com", "2");

            // Act
            var result = await controller.GenerateOffer(101, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CandidateApplication>(viewResult.Model);
            Assert.Equal(101, model.ApplicationId);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("joiningDate"));
        }

        [Fact]
        public async Task GenerateOffer_Post_Keeps_Generated_Status_If_Email_Fails()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication 
            { 
                ApplicationId = 101, 
                CandidateId = 12, 
                JobId = 1, 
                Status = "Screened", 
                BackgroundVerificationStatus = "Verified" 
            };
            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.CandidateApplications.Add(app);
            await db.SaveChangesAsync();

            var tempWebRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempWebRoot);

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(e => e.SendEmailWithAttachmentAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP connection failure"));

            var controller = GetControllerWithEmail(db, "Recruiter", "recruiter@example.com", "2", mockEmail.Object, tempWebRoot);
            var joiningDate = DateTime.Today.AddDays(30);

            try
            {
                // Act
                var result = await controller.GenerateOffer(101, joiningDate);

                // Assert
                var redirectResult = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Shortlisted", redirectResult.ActionName);

                // Check DB: Record exists, but status remains "Generated"
                var offer = await db.OfferLetters.FirstOrDefaultAsync(ol => ol.ApplicationId == 101);
                if (offer == null)
                {
                    Assert.Fail($"Offer is null. ErrorMessage: {controller.TempData["ErrorMessage"]}");
                }

                Assert.Equal("Generated", offer.Status);

                // Warning error message set
                Assert.Contains("sending email failed", controller.TempData["ErrorMessage"]?.ToString());
            }
            finally
            {
                if (Directory.Exists(tempWebRoot))
                {
                    Directory.Delete(tempWebRoot, true);
                }
            }
        }

        [Fact]
        public async Task SendOffer_Post_Resends_Email_Successfully()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            var candidate = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var job = new Job { JobId = 1, JobTitle = "Software Developer", Description = "Desc", Skills = "C#", Experience = "3 yrs", Location = "Remote", Status = "Open" };
            var app = new CandidateApplication 
            { 
                ApplicationId = 101, 
                CandidateId = 12, 
                JobId = 1, 
                Status = "Screened", 
                BackgroundVerificationStatus = "Verified" 
            };
            db.Candidates.Add(candidate);
            db.Jobs.Add(job);
            db.CandidateApplications.Add(app);

            // Seed an existing Generated offer letter
            var tempWebRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempWebRoot);
            Directory.CreateDirectory(Path.Combine(tempWebRoot, "uploads", "offers"));

            string fileName = "OfferLetter_12_temp.pdf";
            string relativePath = "/uploads/offers/" + fileName;
            string physicalPath = Path.Combine(tempWebRoot, "uploads", "offers", fileName);
            File.WriteAllText(physicalPath, "Fake PDF Content");

            var offer = new OfferLetter
            {
                OfferLetterId = 5,
                CandidateId = 12,
                ApplicationId = 101,
                JoiningDate = DateTime.Today.AddDays(30),
                FileName = fileName,
                FilePath = relativePath,
                Status = "Generated"
            };
            db.OfferLetters.Add(offer);
            await db.SaveChangesAsync();

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(e => e.SendEmailWithAttachmentAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = GetControllerWithEmail(db, "Recruiter", "recruiter@example.com", "2", mockEmail.Object, tempWebRoot);

            try
            {
                // Act
                var result = await controller.SendOffer(5);

                // Assert
                var redirectResult = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Shortlisted", redirectResult.ActionName);

                // Verify status updated to "Sent"
                var updatedOffer = await db.OfferLetters.FindAsync(5);
                Assert.Equal("Sent", updatedOffer.Status);

                mockEmail.Verify(e => e.SendEmailWithAttachmentAsync(
                    "kajal@example.com",
                    "Offer Letter – TalentTrack",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    fileName
                ), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempWebRoot))
                {
                    Directory.Delete(tempWebRoot, true);
                }
            }
        }

        [Fact]
        public async Task DownloadOffer_Access_Control_Rules_Are_Enforced()
        {
            // Arrange
            using var db = GetInMemoryDbContext();

            // Seed candidate detail which is queried inside the controller (like ol.Candidate)
            var cand12 = new Candidate { CandidateId = 12, Name = "Kajal Patel", Email = "kajal@example.com" };
            var cand13 = new Candidate { CandidateId = 13, Name = "Ravi Sharma", Email = "ravi@example.com" };
            db.Candidates.AddRange(cand12, cand13);
            
            // Candidate 12 owns Offer 1 (Status: Sent)
            var offer1 = new OfferLetter
            {
                OfferLetterId = 1,
                CandidateId = 12,
                ApplicationId = 101,
                JoiningDate = DateTime.Today.AddDays(30),
                FileName = "offer1.pdf",
                FilePath = "/uploads/offers/offer1.pdf",
                Status = "Sent"
            };

            // Candidate 12 owns Offer 2 (Status: Generated - not yet Sent)
            var offer2 = new OfferLetter
            {
                OfferLetterId = 2,
                CandidateId = 12,
                ApplicationId = 102,
                JoiningDate = DateTime.Today.AddDays(30),
                FileName = "offer2.pdf",
                FilePath = "/uploads/offers/offer2.pdf",
                Status = "Generated"
            };

            // Candidate 13 owns Offer 3 (Status: Sent)
            var offer3 = new OfferLetter
            {
                OfferLetterId = 3,
                CandidateId = 13,
                ApplicationId = 103,
                JoiningDate = DateTime.Today.AddDays(30),
                FileName = "offer3.pdf",
                FilePath = "/uploads/offers/offer3.pdf",
                Status = "Sent"
            };

            db.OfferLetters.AddRange(offer1, offer2, offer3);
            
            // Seed shortlisted applications to allow candidates portal access
            var app1 = new CandidateApplication { ApplicationId = 101, CandidateId = 12, JobId = 1, Status = "Screened" };
            var app2 = new CandidateApplication { ApplicationId = 102, CandidateId = 12, JobId = 1, Status = "Screened" };
            var app3 = new CandidateApplication { ApplicationId = 103, CandidateId = 13, JobId = 1, Status = "Screened" };
            db.CandidateApplications.AddRange(app1, app2, app3);
            
            await db.SaveChangesAsync();

            var tempWebRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempWebRoot);
            Directory.CreateDirectory(Path.Combine(tempWebRoot, "uploads", "offers"));

            // Create actual files
            File.WriteAllText(Path.Combine(tempWebRoot, "uploads", "offers", "offer1.pdf"), "PDF content 1");
            File.WriteAllText(Path.Combine(tempWebRoot, "uploads", "offers", "offer2.pdf"), "PDF content 2");
            File.WriteAllText(Path.Combine(tempWebRoot, "uploads", "offers", "offer3.pdf"), "PDF content 3");

            try
            {
                // Test 1: Recruiter can download ANY offer
                var controllerRecruiter = GetController(db, "Recruiter", "recruiter@example.com", "2", tempWebRoot);
                var resultRecruiter1 = await controllerRecruiter.DownloadOffer(1);
                var resultRecruiter3 = await controllerRecruiter.DownloadOffer(3);
                Assert.IsType<PhysicalFileResult>(resultRecruiter1);
                Assert.IsType<PhysicalFileResult>(resultRecruiter3);

                // Test 2: Admin can download ANY offer
                var controllerAdmin = GetController(db, "Admin", "admin@example.com", "1", tempWebRoot);
                var resultAdmin = await controllerAdmin.DownloadOffer(3);
                Assert.IsType<PhysicalFileResult>(resultAdmin);

                // Test 3: Candidate can download their own SENT offer
                var controllerCandidate12 = GetController(db, "Candidate", "kajal@example.com", "12", tempWebRoot);
                var resultCandSelfSent = await controllerCandidate12.DownloadOffer(1);
                Assert.IsType<PhysicalFileResult>(resultCandSelfSent);

                // Test 4: Candidate CANNOT download their own offer if status is only "Generated" (not Sent)
                var resultCandSelfGen = await controllerCandidate12.DownloadOffer(2);
                Assert.IsType<ForbidResult>(resultCandSelfGen);

                // Test 5: Candidate CANNOT download other candidates' offer
                var resultCandOther = await controllerCandidate12.DownloadOffer(3);
                Assert.IsType<ForbidResult>(resultCandOther);

                // Test 6: Interviewer is denied download
                var controllerInterviewer = GetController(db, "Interviewer", "int@example.com", "5", tempWebRoot);
                var resultInterviewer = await controllerInterviewer.DownloadOffer(1);
                Assert.IsType<ForbidResult>(resultInterviewer);
            }
            finally
            {
                if (Directory.Exists(tempWebRoot))
                {
                    Directory.Delete(tempWebRoot, true);
                }
            }
        }
    }
}