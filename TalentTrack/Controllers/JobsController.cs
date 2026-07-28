using Microsoft.AspNetCore.Mvc;
using TalentTrack.Data;
using TalentTrack.Models;
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
            var jobs = _context.Jobs.ToList();
            return View(jobs);
        }

        // Open Add Job Page
        public IActionResult Create()
        {
            return View();
        }

        // Save Job
        [HttpPost]
        public IActionResult Create(Job job)
        {
            if (ModelState.IsValid)
            {
                _context.Jobs.Add(job);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(job);
        }

        // Open Edit Job Page
        public IActionResult Edit(int id)
        {
            var job = _context.Jobs.Find(id);
            if (job == null)
            {
                return NotFound();
            }
            return View(job);
        }

        // Save Edited Job
        [HttpPost]
        public IActionResult Edit(Job job)
        {
            if (ModelState.IsValid)
            {
                _context.Jobs.Update(job);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(job);
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