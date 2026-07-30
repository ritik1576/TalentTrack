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
        public IActionResult Index()
        {
            var jobs = _context.Jobs.Include(j => j.JobSkills).ToList();
            return View(jobs);
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

            // Save per-skill experience requirements
            if (vm.SkillEntries != null)
            {
                foreach (var entry in vm.SkillEntries.Where(s => !string.IsNullOrEmpty(s.SkillName)))
                {
                    _context.JobSkills.Add(new JobSkill
                    {
                        JobId = job.JobId,
                        SkillName = entry.SkillName,
                        RequiredExperience = entry.RequiredExperience
                    });
                }
                _context.SaveChanges();
            }

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

            // Build legacy Skills string
            var skillNames = vm.SkillEntries?.Where(s => !string.IsNullOrEmpty(s.SkillName)).Select(s => s.SkillName).ToList() ?? new List<string>();

            job.JobTitle = vm.JobTitle;
            job.Description = vm.Description;
            job.Skills = string.Join(", ", skillNames);
            job.Experience = vm.Experience;
            job.Location = vm.Location;
            job.Status = vm.Status;

            // Remove old skills and add updated ones
            _context.JobSkills.RemoveRange(job.JobSkills);

            if (vm.SkillEntries != null)
            {
                foreach (var entry in vm.SkillEntries.Where(s => !string.IsNullOrEmpty(s.SkillName)))
                {
                    _context.JobSkills.Add(new JobSkill
                    {
                        JobId = job.JobId,
                        SkillName = entry.SkillName,
                        RequiredExperience = entry.RequiredExperience
                    });
                }
            }

            _context.SaveChanges();
            TempData["Success"] = "Job updated successfully!";
            return RedirectToAction("Index");
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