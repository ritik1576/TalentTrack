using Microsoft.AspNetCore.Mvc;
using TalentTrack.Data;
using TalentTrack.Models;

namespace TalentTrack.Controllers
{
    public class CandidatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CandidatesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Show all candidates
        public IActionResult Index()
        {
            var candidates = _context.Candidates.ToList();
            return View(candidates);
        }

        // Open Add Candidate Page
        public IActionResult Create()
        {
            return View();
        }

        // Save Candidate Data + Resume PDF File Upload
        [HttpPost]
        public async Task<IActionResult> Create(Candidate candidate, IFormFile? resumeFile)
        {
            if (!string.IsNullOrEmpty(candidate.Phone))
            {
                var digitsOnly = new string(candidate.Phone.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length != 10)
                {
                    ModelState.AddModelError("Phone", "Phone number must be exactly 10 digits.");
                }
                else
                {
                    candidate.Phone = digitsOnly;
                }
            }

            if (resumeFile != null && resumeFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(resumeFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await resumeFile.CopyToAsync(stream);
                }

                candidate.Resume = $"/uploads/resumes/{uniqueFileName}";
            }
            else if (string.IsNullOrEmpty(candidate.Resume))
            {
                candidate.Resume = "Not Uploaded";
            }

            if (ModelState.IsValid)
            {
                _context.Candidates.Add(candidate);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Candidate added successfully!";
                return RedirectToAction("Index");
            }

            return View(candidate);
        }

        // Open Edit Candidate Page
        public IActionResult Edit(int id)
        {
            var candidate = _context.Candidates.Find(id);
            if (candidate == null)
            {
                return NotFound();
            }
            return View(candidate);
        }

        // Save Edited Candidate Data + Resume PDF update
        [HttpPost]
        public async Task<IActionResult> Edit(Candidate candidate, IFormFile? resumeFile)
        {
            if (!string.IsNullOrEmpty(candidate.Phone))
            {
                var digitsOnly = new string(candidate.Phone.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length != 10)
                {
                    ModelState.AddModelError("Phone", "Phone number must be exactly 10 digits.");
                }
                else
                {
                    candidate.Phone = digitsOnly;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(candidate);
            }

            var existing = await _context.Candidates.FindAsync(candidate.CandidateId);
            if (existing == null) return NotFound();

            if (resumeFile != null && resumeFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(resumeFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await resumeFile.CopyToAsync(stream);
                }

                existing.Resume = $"/uploads/resumes/{uniqueFileName}";
            }

            existing.Name = candidate.Name;
            existing.Email = candidate.Email;
            existing.Phone = candidate.Phone;
            existing.Skills = candidate.Skills;
            existing.Experience = candidate.Experience;

            _context.Candidates.Update(existing);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Candidate profile updated successfully!";
            return RedirectToAction("Index");
        }

        // Delete Candidate
        public IActionResult Delete(int id)
        {
            var candidate = _context.Candidates.Find(id);
            if (candidate != null)
            {
                _context.Candidates.Remove(candidate);
                _context.SaveChanges();
                TempData["Info"] = "Candidate deleted.";
            }
            return RedirectToAction("Index");
        }
    }
}