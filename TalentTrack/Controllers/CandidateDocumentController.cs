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
using TalentTrack.Services;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace TalentTrack.Controllers
{
    public class CandidateDocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService? _emailService;

        public CandidateDocumentController(ApplicationDbContext context, IWebHostEnvironment environment, IEmailService? emailService = null)
        {
            _context = context;
            _environment = environment;
            _emailService = emailService;
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

            var offer = await _context.OfferLetters
                .Include(ol => ol.Application)
                    .ThenInclude(app => app.Job)
                .FirstOrDefaultAsync(ol => ol.CandidateId == candidateId && ol.Status == "Sent");

            ViewBag.Offer = offer;

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

            var offerLetters = await _context.OfferLetters
                .Include(ol => ol.Candidate)
                .Include(ol => ol.Application)
                    .ThenInclude(a => a.Job)
                .ToListAsync();

            ViewBag.OfferLetters = offerLetters;

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

        // GET: /CandidateDocument/GenerateOffer
        [HttpGet]
        public async Task<IActionResult> GenerateOffer(int applicationId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var application = await _context.CandidateApplications
                .Include(ca => ca.Candidate)
                .Include(ca => ca.Job)
                .FirstOrDefaultAsync(ca => ca.ApplicationId == applicationId);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Backend validation: shortlisted + verified
            if (application.BackgroundVerificationStatus != "Verified")
            {
                TempData["ErrorMessage"] = "Candidate's background verification must be Verified to generate an offer.";
                return RedirectToAction(nameof(Shortlisted));
            }

            if (!await IsCandidateShortlisted(application.CandidateId))
            {
                TempData["ErrorMessage"] = "Candidate must be shortlisted through the screening workflow.";
                return RedirectToAction(nameof(Shortlisted));
            }

            return View(application);
        }

        // POST: /CandidateDocument/GenerateOffer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateOffer(int applicationId, DateTime? joiningDate)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin")
            {
                return Forbid();
            }

            var application = await _context.CandidateApplications
                .Include(ca => ca.Candidate)
                .Include(ca => ca.Job)
                .FirstOrDefaultAsync(ca => ca.ApplicationId == applicationId);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Backend validation: shortlisted + verified
            if (application.BackgroundVerificationStatus != "Verified")
            {
                return BadRequest("Candidate background verification status is not Verified.");
            }

            if (!await IsCandidateShortlisted(application.CandidateId))
            {
                return BadRequest("Candidate is not shortlisted.");
            }

            if (joiningDate == null)
            {
                ModelState.AddModelError("joiningDate", "Joining Date is required.");
                return View(application);
            }

            var candidate = application.Candidate;
            var job = application.Job;
            if (candidate == null || job == null)
            {
                return NotFound("Candidate or Job details missing.");
            }

            try
            {
                // Generate PDF
                string fileName = $"OfferLetter_{candidate.CandidateId}_{DateTime.Now.Ticks}.pdf";
                string relativePath = Path.Combine("uploads", "offers", fileName);
                string physicalPath = Path.Combine(_environment.WebRootPath, relativePath);

                // Ensure directory exists
                string dirPath = Path.GetDirectoryName(physicalPath)!;
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                // Render PDF using PdfSharp
                using (PdfDocument document = new PdfDocument())
                {
                    document.Info.Title = "Offer Letter - " + candidate.Name;
                    PdfPage page = document.AddPage();
                    page.Size = PdfSharp.PageSize.A4;
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    XPen pen = new XPen(XColors.Navy, 1.5);
                    gfx.DrawLine(pen, 40.0, 100.0, page.Width.Point - 40.0, 100.0);

                    XFont fontTitle = new XFont("Arial", 22, XFontStyleEx.Bold);
                    XFont fontHeader = new XFont("Arial", 12, XFontStyleEx.Bold);
                    XFont fontBody = new XFont("Arial", 11, XFontStyleEx.Regular);
                    XFont fontBodyBold = new XFont("Arial", 11, XFontStyleEx.Bold);
                    XFont fontFooter = new XFont("Arial", 9, XFontStyleEx.Italic);

                    gfx.DrawString("TALENTTRACK", fontHeader, XBrushes.Navy, new XRect(40.0, 50.0, page.Width.Point - 80.0, 30.0), XStringFormats.TopLeft);
                    gfx.DrawString("OFFER LETTER", fontTitle, XBrushes.DarkSlateGray, new XRect(0.0, 130.0, page.Width.Point, 40.0), XStringFormats.TopCenter);

                    gfx.DrawString($"Date: {DateTime.Now:dd MMMM yyyy}", fontBodyBold, XBrushes.Black, new XRect(40.0, 190.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString($"Dear {candidate.Name},", fontBodyBold, XBrushes.Black, new XRect(40.0, 230.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);

                    string p1 = $"We are pleased to offer you the position of {job.JobTitle} at TalentTrack.";
                    string p2 = "We were thoroughly impressed by your qualifications and the discussions during the interview process.";
                    string p3 = $"Your joining date is scheduled to be {joiningDate.Value.ToString("dd MMMM yyyy")}.";
                    string p4 = "Please find below key details regarding your offer:";

                    gfx.DrawString(p1, fontBody, XBrushes.Black, new XRect(40.0, 260.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString(p2, fontBody, XBrushes.Black, new XRect(40.0, 280.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString(p3, fontBody, XBrushes.Black, new XRect(40.0, 310.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString(p4, fontBody, XBrushes.Black, new XRect(40.0, 330.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);

                    XRect rectBox = new XRect(40.0, 360.0, page.Width.Point - 80.0, 120.0);
                    gfx.DrawRectangle(new XPen(XColors.LightGray, 1), XBrushes.GhostWhite, rectBox);

                    double startY = 380.0;
                    gfx.DrawString("Position:", fontBodyBold, XBrushes.Black, new XRect(60.0, startY, 150.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString(job.JobTitle, fontBody, XBrushes.Black, new XRect(220.0, startY, 300.0, 20.0), XStringFormats.TopLeft);

                    gfx.DrawString("Candidate Email:", fontBodyBold, XBrushes.Black, new XRect(60.0, startY + 25.0, 150.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString(candidate.Email ?? "N/A", fontBody, XBrushes.Black, new XRect(220.0, startY + 25.0, 300.0, 20.0), XStringFormats.TopLeft);

                    gfx.DrawString("Joining Date:", fontBodyBold, XBrushes.Black, new XRect(60.0, startY + 50.0, 150.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString(joiningDate.Value.ToString("dd MMMM yyyy"), fontBody, XBrushes.Black, new XRect(220.0, startY + 50.0, 300.0, 20.0), XStringFormats.TopLeft);

                    gfx.DrawString("Company Name:", fontBodyBold, XBrushes.Black, new XRect(60.0, startY + 75.0, 150.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString("TalentTrack", fontBody, XBrushes.Black, new XRect(220.0, startY + 75.0, 300.0, 20.0), XStringFormats.TopLeft);

                    string p5 = "To accept this offer, please sign and return the duplicate copy of this letter on or before your joining date.";
                    string p6 = "We look forward to welcoming you to the TalentTrack team.";

                    gfx.DrawString(p5, fontBody, XBrushes.Black, new XRect(40.0, 500.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString(p6, fontBody, XBrushes.Black, new XRect(40.0, 520.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);

                    gfx.DrawString("Regards,", fontBody, XBrushes.Black, new XRect(40.0, 560.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString("Recruitment Team", fontBodyBold, XBrushes.Navy, new XRect(40.0, 580.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);
                    gfx.DrawString("TalentTrack", fontBodyBold, XBrushes.Navy, new XRect(40.0, 595.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopLeft);

                    gfx.DrawLine(pen, 40.0, page.Height.Point - 60.0, page.Width.Point - 40.0, page.Height.Point - 60.0);
                    gfx.DrawString("This is a system-generated document and does not require a physical signature.", fontFooter, XBrushes.Gray, new XRect(40.0, page.Height.Point - 50.0, page.Width.Point - 80.0, 20.0), XStringFormats.TopCenter);

                    document.Save(physicalPath);
                }

                // Database check for duplicate / repeated generation
                var existingOffer = await _context.OfferLetters
                    .FirstOrDefaultAsync(ol => ol.ApplicationId == applicationId);

                string oldFilePath = "";
                if (existingOffer != null)
                {
                    oldFilePath = existingOffer.FilePath;
                    existingOffer.FileName = fileName;
                    existingOffer.FilePath = "/" + relativePath.Replace('\\', '/');
                    existingOffer.JoiningDate = joiningDate.Value;
                    existingOffer.OfferDate = DateTime.Now;
                    existingOffer.Status = "Generated";
                    existingOffer.UpdatedAt = DateTime.Now;
                    _context.OfferLetters.Update(existingOffer);
                }
                else
                {
                    existingOffer = new OfferLetter
                    {
                        CandidateId = candidate.CandidateId,
                        ApplicationId = applicationId,
                        JoiningDate = joiningDate.Value,
                        OfferDate = DateTime.Now,
                        FileName = fileName,
                        FilePath = "/" + relativePath.Replace('\\', '/'),
                        Status = "Generated",
                        CreatedAt = DateTime.Now
                    };
                    await _context.OfferLetters.AddAsync(existingOffer);
                }

                await _context.SaveChangesAsync();

                // Delete old PDF file if regenerating to avoid file clutter
                if (!string.IsNullOrEmpty(oldFilePath))
                {
                    string oldPhysicalPath = Path.Combine(_environment.WebRootPath, oldFilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldPhysicalPath);
                        }
                        catch (Exception)
                        {
                            // Log and ignore to prevent blocking
                        }
                    }
                }

                // Send email
                bool emailSent = false;
                try
                {
                    // Simulated Email Log
                    string emailBodySim = $@"
============================================================
SIMULATED EMAIL SENT (WITH ATTACHMENT: {fileName})
Date: {DateTime.Now:dd MMM yyyy, hh:mm tt}
To: {candidate.Email}
Subject: Offer Letter – TalentTrack
Body:
Dear {candidate.Name},

Please find attached your Offer Letter for the position of {job.JobTitle}.

Your joining date is {joiningDate.Value.ToString("dd MMMM yyyy")}.

Regards,
Recruitment Team
TalentTrack
============================================================
";
                    var emailLogPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "simulated_emails.txt");
                    var emailDirectory = Path.GetDirectoryName(emailLogPath);
                    if (!string.IsNullOrEmpty(emailDirectory) && !Directory.Exists(emailDirectory))
                    {
                        Directory.CreateDirectory(emailDirectory);
                    }
                    await System.IO.File.AppendAllTextAsync(emailLogPath, emailBodySim);

                    if (_emailService != null)
                    {
                        var body = $@"Dear {candidate.Name},<br/><br/>
Please find attached your Offer Letter for the position of {job.JobTitle}.<br/><br/>
Your joining date is <strong>{joiningDate.Value.ToString("dd MMMM yyyy")}</strong>.<br/><br/>
Regards,<br/>
Recruitment Team<br/>
TalentTrack";
                        await _emailService.SendEmailWithAttachmentAsync(candidate.Email, $"Offer Letter – TalentTrack", body, physicalPath, fileName);
                    }
                    emailSent = true;
                }
                catch (Exception ex)
                {
                    // Email failed, but we keep the Generated offer letter
                    TempData["ErrorMessage"] = $"Offer Letter PDF generated successfully, but sending email failed: {ex.Message}. You can retry sending from the pipeline.";
                }

                if (emailSent)
                {
                    existingOffer.Status = "Sent";
                    _context.OfferLetters.Update(existingOffer);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Offer Letter generated and emailed to candidate successfully!";
                }

                return RedirectToAction(nameof(Shortlisted));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred during offer generation: {ex.Message}";
                return RedirectToAction(nameof(Shortlisted));
            }
        }

        // POST: /CandidateDocument/SendOffer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOffer(int offerLetterId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Recruiter" && role != "Admin")
            {
                return Forbid();
            }

            var offer = await _context.OfferLetters
                .Include(ol => ol.Candidate)
                .Include(ol => ol.Application)
                    .ThenInclude(app => app.Job)
                .FirstOrDefaultAsync(ol => ol.OfferLetterId == offerLetterId);

            if (offer == null)
            {
                return NotFound("Offer Letter not found.");
            }

            var candidate = offer.Candidate;
            var job = offer.Application?.Job;
            if (candidate == null || job == null)
            {
                return NotFound("Candidate or Job details missing.");
            }

            string physicalPath = Path.Combine(_environment.WebRootPath, offer.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound("Offer letter PDF file not found on server. Try regenerating the offer.");
            }

            try
            {
                // Simulated Email Log
                string emailBodySim = $@"
============================================================
SIMULATED EMAIL SENT (WITH ATTACHMENT: {offer.FileName})
Date: {DateTime.Now:dd MMM yyyy, hh:mm tt}
To: {candidate.Email}
Subject: Offer Letter – TalentTrack
Body:
Dear {candidate.Name},

Please find attached your Offer Letter for the position of {job.JobTitle}.

Your joining date is {offer.JoiningDate.ToString("dd MMMM yyyy")}.

Regards,
Recruitment Team
TalentTrack
============================================================
";
                var emailLogPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "simulated_emails.txt");
                await System.IO.File.AppendAllTextAsync(emailLogPath, emailBodySim);

                if (_emailService != null)
                {
                    var body = $@"Dear {candidate.Name},<br/><br/>
Please find attached your Offer Letter for the position of {job.JobTitle}.<br/><br/>
Your joining date is <strong>{offer.JoiningDate.ToString("dd MMMM yyyy")}</strong>.<br/><br/>
Regards,<br/>
Recruitment Team<br/>
TalentTrack";
                    await _emailService.SendEmailWithAttachmentAsync(candidate.Email, $"Offer Letter – TalentTrack", body, physicalPath, offer.FileName);
                }

                offer.Status = "Sent";
                _context.OfferLetters.Update(offer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Offer Letter sent to candidate registered email successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send email: {ex.Message}";
            }

            return RedirectToAction(nameof(Shortlisted));
        }

        // GET: /CandidateDocument/DownloadOffer
        [HttpGet]
        public async Task<IActionResult> DownloadOffer(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            var offer = await _context.OfferLetters
                .Include(ol => ol.Candidate)
                .FirstOrDefaultAsync(ol => ol.OfferLetterId == id);

            if (offer == null)
            {
                return NotFound("Offer Letter not found.");
            }

            // Security check: Candidates can only access their own offer and only if it is "Sent"
            if (role == "Candidate")
            {
                var candidateIdStr = HttpContext.Session.GetString("UserId");
                if (!int.TryParse(candidateIdStr, out var loggedInCandidateId) || offer.CandidateId != loggedInCandidateId || offer.Status != "Sent")
                {
                    return Forbid("Access denied to this offer letter.");
                }
            }
            else if (role != "Recruiter" && role != "Admin")
            {
                return Forbid("Access denied.");
            }

            string filePath = Path.Combine(_environment.WebRootPath, offer.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Physical file not found on server.");
            }

            return PhysicalFile(filePath, "application/pdf", offer.FileName);
        }
    }
}
