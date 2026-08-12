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
            httpContext.Session.SetString("UserRole", role);
            httpContext.Session.SetString("UserEmail", email);
            httpContext.Session.SetString("UserId", userId);
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

            // Logged in as Candidate 12 trying to download Candidate 55's doc
            var controller = GetController(db, "Candidate", "kajal@example.com", "12");

            // Act
            var result = await controller.Download(100);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
    }
}
