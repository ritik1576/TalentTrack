using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TalentTrack.Data;
using TalentTrack.Models;

namespace TalentTrack.Controllers
{
    public class CandidateDocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CandidateDocumentController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /CandidateDocument/Portal
        [HttpGet]
        public async Task<IActionResult> Portal()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Candidate")
            {
                return RedirectToAction("Login", "Account");
            }

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdStr, out var candidateId))
            {
                return RedirectToAction("Login", "Account");
            }

            var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.CandidateId == candidateId);
            if (candidate == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            // Retrieve all documents for this candidate
            var dbDocs = await _context.CandidateDocuments
                .Where(d => d.CandidateId == candidateId)
                .ToListAsync();

            // Prepare list of required document types
            var requiredTypes = new List<string> { "ID Proof", "Address Proof", "Education Certificate", "Experience Letter" };
            
            ViewBag.Candidate = candidate;
            ViewBag.UploadedDocuments = dbDocs;
            ViewBag.RequiredTypes = requiredTypes;
            ViewBag.IsShortlisted = await IsCandidateShortlisted(candidateId);

            return View();
        }

        // POST: /CandidateDocument/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string documentType, IFormFile documentFile)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Candidate")
            {
                return Challenge();
            }

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdStr, out var candidateId))
            {
                return Challenge();
            }

            var candidate = await _context.Candidates.FindAsync(candidateId);
            if (candidate == null)
            {
                return NotFound("Candidate not found.");
            }

            // Check if candidate is shortlisted/selected
            if (!await IsCandidateShortlisted(candidateId))
            {
                return Forbid();
            }

            var requiredTypes = new List<string> { "ID Proof", "Address Proof", "Education Certificate", "Experience Letter" };
            if (string.IsNullOrEmpty(documentType) || !requiredTypes.Contains(documentType))
            {
                TempData["ErrorMessage"] = "Invalid document type.";
                return RedirectToAction(nameof(Portal));
            }

            if (documentFile == null || documentFile.Length == 0)
            {
                TempData["ErrorMessage"] = "File cannot be empty.";
                return RedirectToAction(nameof(Portal));
            }

            // File extension validation
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(documentFile.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "Only PDF, JPG, JPEG, and PNG files are allowed.";
                return RedirectToAction(nameof(Portal));
            }

            // File size validation (Max 5MB)
            long maxFileLength = 5 * 1024 * 1024;
            if (documentFile.Length > maxFileLength)
            {
                TempData["ErrorMessage"] = "File size exceeds 5MB limit.";
                return RedirectToAction(nameof(Portal));
            }

            try
            {
                // Create documents directory if not exists
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Clean and structure the stored filename
                var uniqueFileName = $"cand_{candidateId}_{documentType.Replace(" ", "_").ToLower()}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                var fileUrl = $"/uploads/documents/{uniqueFileName}";

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await documentFile.CopyToAsync(stream);
                }

                // Check if document of this type already exists for replacement
                var existingDoc = await _context.CandidateDocuments
                    .FirstOrDefaultAsync(d => d.CandidateId == candidateId && d.DocumentType == documentType);

                if (existingDoc != null)
                {
                    // Optionally delete the old file from disk
                    var oldFilePath = Path.Combine(_environment.WebRootPath, existingDoc.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                    // Update existing record
                    existingDoc.FileName = documentFile.FileName;
                    existingDoc.FilePath = fileUrl;
                    existingDoc.Status = "Uploaded";
                    existingDoc.UpdatedAt = DateTime.Now;
                    _context.CandidateDocuments.Update(existingDoc);
                }
                else
                {
                    // Create new record
                    var newDoc = new CandidateDocument
                    {
                        CandidateId = candidateId,
                        DocumentType = documentType,
                        FileName = documentFile.FileName,
                        FilePath = fileUrl,
                        Status = "Uploaded",
                        CreatedAt = DateTime.Now
                    };
                    _context.CandidateDocuments.Add(newDoc);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{documentType} uploaded successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred during file upload. Please try again.";
            }

            return RedirectToAction(nameof(Portal));
        }

        // GET: /CandidateDocument/Download/5
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(userIdStr))
            {
                return Challenge();
            }

            var doc = await _context.CandidateDocuments.FindAsync(id);
            if (doc == null)
            {
                return NotFound("Document not found.");
            }

            // Security check: Candidate can only download their own document. Recruiter/Admin/Interviewer can download any candidate's document.
            if (role == "Candidate")
            {
                if (!int.TryParse(userIdStr, out var candidateId) || doc.CandidateId != candidateId)
                {
                    return Forbid();
                }

                if (!await IsCandidateShortlisted(candidateId))
                {
                    return Forbid();
                }
            }

            var filePath = Path.Combine(_environment.WebRootPath, doc.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Physical file not found on server.");
            }

            var contentType = "application/octet-stream";
            var ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".pdf") contentType = "application/pdf";
            else if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
            else if (ext == ".png") contentType = "image/png";

            return PhysicalFile(filePath, contentType, doc.FileName);
        }

        // GET: /CandidateDocument/Shortlisted
        [HttpGet]
        public async Task<IActionResult> Shortlisted()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var shortlistedApplications = await _context.CandidateApplications
                .Include(ca => ca.Candidate)
                .Include(ca => ca.Job)
                .Where(ca => !string.IsNullOrEmpty(ca.BackgroundVerificationStatus) && ca.BackgroundVerificationStatus != "Rejected")
                .ToListAsync();

            return View(shortlistedApplications);
        }

        // POST: /CandidateDocument/ShortlistCandidate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShortlistCandidate(int applicationId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin")
            {
                return Challenge();
            }

            var application = await _context.CandidateApplications.FindAsync(applicationId);
            if (application == null)
            {
                return NotFound("Application not found.");
            }

            application.BackgroundVerificationStatus = "Pending";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Candidate successfully shortlisted for document verification.";
            return RedirectToAction(nameof(Shortlisted));
        }

        // GET: /CandidateDocument/Verify/5
        [HttpGet]
        public async Task<IActionResult> Verify(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var application = await _context.CandidateApplications
                .Include(ca => ca.Candidate)
                .Include(ca => ca.Job)
                .FirstOrDefaultAsync(ca => ca.ApplicationId == id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            var candidate = application.Candidate;
            if (candidate == null)
            {
                return NotFound("Candidate not found.");
            }

            // Retrieve all documents for this candidate
            var dbDocs = await _context.CandidateDocuments
                .Where(d => d.CandidateId == candidate.CandidateId)
                .ToListAsync();

            var requiredTypes = new List<string> { "ID Proof", "Address Proof", "Education Certificate", "Experience Letter" };

            if (string.IsNullOrEmpty(application.BackgroundVerificationStatus))
            {
                application.BackgroundVerificationStatus = "Pending";
            }

            ViewBag.Application = application;
            ViewBag.Candidate = candidate;
            ViewBag.UploadedDocuments = dbDocs;
            ViewBag.RequiredTypes = requiredTypes;

            return View();
        }

        // POST: /CandidateDocument/UpdateVerificationStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVerificationStatus(int applicationId, string status)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin")
            {
                return Forbid();
            }

            var allowedStatuses = new List<string> { "Pending", "In Review", "Verified", "Rejected" };
            if (!allowedStatuses.Contains(status))
            {
                return BadRequest("Invalid status value.");
            }

            var application = await _context.CandidateApplications.FindAsync(applicationId);
            if (application == null)
            {
                return NotFound("Application not found.");
            }

            application.BackgroundVerificationStatus = status;
            _context.CandidateApplications.Update(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Background Verification status updated successfully.";

            return RedirectToAction(nameof(Verify), new { id = applicationId });
        }

        // GET: /CandidateDocument/VerifyDownload
        [HttpGet]
        public async Task<IActionResult> VerifyDownload(int documentId, int candidateId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin")
            {
                return Forbid();
            }

            var doc = await _context.CandidateDocuments.FindAsync(documentId);
            if (doc == null)
            {
                return NotFound("Document not found.");
            }

            // Security check: Verify the document belongs to the requested candidate
            if (doc.CandidateId != candidateId)
            {
                return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, doc.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Physical file not found on server.");
            }

            var contentType = "application/octet-stream";
            var ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".pdf") contentType = "application/pdf";
            else if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
            else if (ext == ".png") contentType = "image/png";

            return PhysicalFile(filePath, contentType, doc.FileName);
        }

        private async Task<bool> IsCandidateShortlisted(int candidateId)
        {
            return await _context.CandidateApplications
                .AnyAsync(ca => ca.CandidateId == candidateId && (ca.Status == "Screened" || ca.Status == "Interview"));
        }
    }
}
