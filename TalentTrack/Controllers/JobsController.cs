using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentTrack.Data;
using TalentTrack.Models;
using TalentTrack.Models.DTOs;
using System.Linq;

namespace TalentTrack.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show Jobs
        public async Task<IActionResult> Index(string? search, string? filterStatus, int page = 1, int pageSize = 10)
        {
            var query = _context.Jobs.Include(j => j.JobSkills).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower().Trim();
                query = query.Where(j => j.JobTitle != null && j.JobTitle.ToLower().Contains(lowerSearch) || 
                                         j.Location != null && j.Location.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrEmpty(filterStatus))
            {
                var lowerStatus = filterStatus.ToLower().Trim();
                query = query.Where(j => j.Status != null && j.Status.ToLower() == lowerStatus);
            }

            var totalItems = await query.CountAsync();

            // Ensure bounds for page
            if (page < 1) page = 1;

            if (pageSize == -1)
            {
                pageSize = totalItems > 0 ? totalItems : 10;
            }
            else if (pageSize < 1)
            {
                pageSize = 10;
            }

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page > totalPages && totalPages > 0) page = totalPages;

            var items = await query
                .OrderBy(j => j.JobId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedList = new PagedList<Job>(items, totalItems, page, pageSize);

            ViewBag.Search = search;
            ViewBag.FilterStatus = filterStatus;

            return View(pagedList);
        }

        // Job Details Page
        public IActionResult Details(int id)
        {
            var job = _context.Jobs.Include(j => j.JobSkills).FirstOrDefault(j => j.JobId == id);
            if (job == null) return NotFound();
            return View(job);
        }

        // Open Add Job Page
        public IActionResult Create()
        {
            var vm = new JobCreateViewModel();
            return View(vm);
        }

        // Save Job
        [HttpPost]
        public IActionResult Create(JobCreateViewModel vm)
        {
            // Build the legacy Skills field from selected skills
            var skillNames = vm.SkillEntries?.Where(s => !string.IsNullOrEmpty(s.SkillName)).Select(s => s.SkillName).ToList() ?? new List<string>();
            vm.Skills = string.Join(", ", skillNames);

            var job = new Job
            {
                JobTitle = vm.JobTitle,
                Description = vm.Description,
                Skills = vm.Skills,
                Experience = vm.Experience,
                Location = vm.Location,
                Status = vm.Status
            };

            _context.Jobs.Add(job);
            _context.SaveChanges();

            // Sync job skills from JD and manual inputs
            SyncJobSkills(job.JobId, job.Description, vm.SkillEntries);

            TempData["Success"] = "Job posted successfully!";
            return RedirectToAction("Index");
        }

        // Open Edit Job Page
        public IActionResult Edit(int id)
        {
            var job = _context.Jobs.Include(j => j.JobSkills).FirstOrDefault(j => j.JobId == id);
            if (job == null) return NotFound();

            var vm = new JobCreateViewModel
            {
                JobId = job.JobId,
                JobTitle = job.JobTitle,
                Description = job.Description,
                Skills = job.Skills,
                Experience = job.Experience,
                Location = job.Location,
                Status = job.Status,
                SkillEntries = job.JobSkills.Select(js => new JobSkillInput
                {
                    SkillName = js.SkillName,
                    RequiredExperience = js.RequiredExperience
                }).ToList()
            };

            return View(vm);
        }

        // Save Edited Job
        [HttpPost]
        public IActionResult Edit(JobCreateViewModel vm)
        {
            var job = _context.Jobs.Include(j => j.JobSkills).FirstOrDefault(j => j.JobId == vm.JobId);
            if (job == null) return NotFound();

            var skillNames = vm.SkillEntries?.Where(s => !string.IsNullOrEmpty(s.SkillName)).Select(s => s.SkillName).ToList() ?? new List<string>();

            job.JobTitle = vm.JobTitle;
            job.Description = vm.Description;
            job.Skills = string.Join(", ", skillNames);
            job.Experience = vm.Experience;
            job.Location = vm.Location;
            job.Status = vm.Status;

            _context.Jobs.Update(job);
            _context.SaveChanges();

            // Sync job skills from JD and manual inputs
            SyncJobSkills(job.JobId, job.Description, vm.SkillEntries);

            TempData["Success"] = "Job updated successfully!";
            return RedirectToAction("Index");
        }

        private List<string> ExtractSkillsFromDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return new List<string>();

            var commonSkills = new List<string>
            {
                "Java", "Spring Boot", "Spring", "AWS", "SQL", "Docker", "Kubernetes", "Python", 
                "Django", "Flask", "React", "Angular", "Vue", "JavaScript", "TypeScript", "Node.js", 
                "Express", "C#", ".NET", "ASP.NET", "Azure", "GCP", "HTML", "CSS", "Git", "GitHub", 
                "CI/CD", "Jenkins", "PostgreSQL", "MySQL", "MongoDB", "Redis", "C++", "Go", "Rust", 
                "PHP", "Laravel"
            };

            var extracted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var skill in commonSkills)
            {
                string pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(skill)}\b";
                if (System.Text.RegularExpressions.Regex.IsMatch(description, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    extracted.Add(skill);
                }
            }

            return extracted.ToList();
        }

        private void SyncJobSkills(int jobId, string description, List<JobSkillInput> manualSkills)
        {
            var existingSkills = _context.JobSkills.Where(js => js.JobId == jobId).ToList();
            var extractedSkillNames = ExtractSkillsFromDescription(description);

            var newSkills = new List<JobSkill>();
            var manualSkillNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (manualSkills != null)
            {
                foreach (var entry in manualSkills.Where(s => !string.IsNullOrEmpty(s.SkillName)))
                {
                    if (manualSkillNames.Add(entry.SkillName))
                    {
                        var existing = existingSkills.FirstOrDefault(es => es.SkillName.Equals(entry.SkillName, StringComparison.OrdinalIgnoreCase));
                        if (existing != null)
                        {
                            existing.RequiredExperience = entry.RequiredExperience;
                            existing.Source = "manual";
                            existing.UpdatedAt = DateTime.Now;
                            _context.JobSkills.Update(existing);
                        }
                        else
                        {
                            newSkills.Add(new JobSkill
                            {
                                JobId = jobId,
                                SkillName = entry.SkillName,
                                RequiredExperience = entry.RequiredExperience,
                                Source = "manual",
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            });
                        }
                    }
                }
            }

            foreach (var skillName in extractedSkillNames)
            {
                if (!manualSkillNames.Contains(skillName))
                {
                    var existing = existingSkills.FirstOrDefault(es => es.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.Source = "jd_extracted";
                        existing.UpdatedAt = DateTime.Now;
                        _context.JobSkills.Update(existing);
                    }
                    else
                    {
                        newSkills.Add(new JobSkill
                        {
                            JobId = jobId,
                            SkillName = skillName,
                            RequiredExperience = 1,
                            Source = "jd_extracted",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }
                }
            }

            if (newSkills.Any())
            {
                _context.JobSkills.AddRange(newSkills);
            }

            var allValidNames = new HashSet<string>(extractedSkillNames, StringComparer.OrdinalIgnoreCase);
            if (manualSkills != null)
            {
                foreach (var s in manualSkills.Where(x => !string.IsNullOrEmpty(x.SkillName)))
                {
                    allValidNames.Add(s.SkillName);
                }
            }

            var toRemove = existingSkills.Where(es => !allValidNames.Contains(es.SkillName)).ToList();
            if (toRemove.Any())
            {
                _context.JobSkills.RemoveRange(toRemove);
            }

            _context.SaveChanges();
        }

        // Delete Job
        public IActionResult Delete(int id)
        {
            var job = _context.Jobs.Find(id);
            if (job != null)
            {
                _context.Jobs.Remove(job);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}