using Microsoft.AspNetCore.Mvc;
using TalentTrack.Data;
using TalentTrack.Models;

namespace TalentTrack.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Admin") return RedirectToAction("Index", "Admin");
            if (role == "Recruiter") return RedirectToAction("Index", "Home");
            if (role == "Interviewer") return RedirectToAction("Dashboard", "Interviewer");

            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password, bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter both Email and Password.";
                return View();
            }

            email = email.Trim().ToLower();

            // Admin Master Credentials or UserAccounts check
            if (email == "admin@talenttrack.com" && password == "admin123")
            {
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", "System Admin");
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserInitials", "SA");
                return RedirectToAction("Index", "Admin");
            }

            // Check UserAccounts table
            var userAccount = _context.UserAccounts
                .FirstOrDefault(u => u.Email.ToLower() == email && u.Password == password);

            if (userAccount != null)
            {
                if (!userAccount.IsApproved || userAccount.Status != "Approved")
                {
                    ViewBag.Error = "Your account registration is pending Admin Approval. Please wait until the Admin approves your account.";
                    return View();
                }

                HttpContext.Session.SetString("UserRole", userAccount.Role);
                HttpContext.Session.SetString("UserName", userAccount.Name);
                HttpContext.Session.SetString("UserEmail", userAccount.Email);
                var initials = string.Concat(userAccount.Name.Split(' ').Take(2).Select(w => w.Length > 0 ? w[0].ToString().ToUpper() : ""));
                HttpContext.Session.SetString("UserInitials", string.IsNullOrEmpty(initials) ? "U" : initials);

                if (userAccount.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (userAccount.Role == "Interviewer")
                {
                    var interviewer = _context.Interviewers.FirstOrDefault(i => i.Email.ToLower() == email);
                    if (interviewer != null)
                    {
                        HttpContext.Session.SetString("UserId", interviewer.InterviewerId.ToString());
                    }
                    return RedirectToAction("Dashboard", "Interviewer");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            // Check fallback for seeded interviewers if not in UserAccounts yet
            var interviewerFallback = _context.Interviewers
                .FirstOrDefault(i => i.Email.ToLower() == email && i.Password == password);

            if (interviewerFallback != null)
            {
                HttpContext.Session.SetString("UserRole", "Interviewer");
                HttpContext.Session.SetString("UserName", interviewerFallback.Name);
                HttpContext.Session.SetString("UserEmail", interviewerFallback.Email);
                HttpContext.Session.SetString("UserId", interviewerFallback.InterviewerId.ToString());
                HttpContext.Session.SetString("UserInitials", interviewerFallback.AvatarInitials ?? "I");
                return RedirectToAction("Dashboard", "Interviewer");
            }

            ViewBag.Error = "Invalid credentials or account not found. If you are new, please Sign Up first for Admin Approval.";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(UserAccount model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Error = "Please fill in all required fields.";
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.Phone))
            {
                var digitsOnly = new string(model.Phone.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length != 10)
                {
                    ViewBag.Error = "Phone number must be exactly 10 digits.";
                    return View(model);
                }
                model.Phone = digitsOnly;
            }

            model.Email = model.Email.Trim().ToLower();

            // Check if email already registered
            if (_context.UserAccounts.Any(u => u.Email.ToLower() == model.Email))
            {
                ViewBag.Error = "An account with this email address already exists. Please Sign In or use another email.";
                return View(model);
            }

            model.Status = "Pending";
            model.IsApproved = false;
            model.CreatedAt = DateTime.Now;

            _context.UserAccounts.Add(model);

            // Also create Interviewer record if role is Interviewer
            if (model.Role == "Interviewer")
            {
                var initials = string.Concat(model.Name.Split(' ').Take(2).Select(w => w.Length > 0 ? w[0].ToString().ToUpper() : ""));
                _context.Interviewers.Add(new Interviewer
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = model.Password,
                    Phone = model.Phone,
                    Department = model.Department,
                    AvatarInitials = string.IsNullOrEmpty(initials) ? "I" : initials,
                    CreatedAt = DateTime.Now
                });
            }

            // Create notification for Admin
            _context.Notifications.Add(new Notification
            {
                TargetRole = "Admin",
                Title = "New Registration Request",
                Message = $"User '{model.Name}' ({model.Email}) registered as {model.Role} and requires your approval.",
                CreatedAt = DateTime.Now,
                TargetUrl = "/Admin/Index"
            });

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Registration successful! Your request has been sent to the Admin. Once approved, you will be able to log in.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Interviewer")
            {
                return RedirectToAction("Profile", "Interviewer");
            }

            var currentEmail = HttpContext.Session.GetString("UserEmail");
            UserAccount? dbUser = null;
            if (!string.IsNullOrEmpty(currentEmail))
            {
                dbUser = _context.UserAccounts.FirstOrDefault(u => u.Email.ToLower() == currentEmail.ToLower());
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? dbUser?.Name ?? "Recruiter";
            ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail") ?? dbUser?.Email ?? "recruiter@talenttrack.com";
            ViewBag.UserPhone = HttpContext.Session.GetString("UserPhone") ?? dbUser?.Phone ?? "";
            ViewBag.Company = HttpContext.Session.GetString("Company") ?? "TalentTrack";
            ViewBag.Location = HttpContext.Session.GetString("Location") ?? "";
            ViewBag.Bio = HttpContext.Session.GetString("Bio") ?? "";
            ViewBag.LinkedIn = HttpContext.Session.GetString("LinkedIn") ?? "";

            return View();
        }

        [HttpPost]
        public IActionResult Profile(string name, string email, string phone, string company, string location, string bio, string linkedin)
        {
            var role = HttpContext.Session.GetString("UserRole");

            // Server-side validation for Phone: if provided, must be exactly 10 digits
            if (!string.IsNullOrEmpty(phone))
            {
                var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length != 10)
                {
                    TempData["Error"] = "Phone number must be exactly 10 digits.";
                    return RedirectToAction("Profile");
                }
                phone = digitsOnly;
            }

            // Update session values
            HttpContext.Session.SetString("UserName", name ?? "Recruiter");
            HttpContext.Session.SetString("UserEmail", email ?? "");
            var initials = string.IsNullOrEmpty(name) ? "R" : string.Concat(name.Split(' ').Take(2).Select(w => w[0].ToString().ToUpper()));
            HttpContext.Session.SetString("UserInitials", initials);
            HttpContext.Session.SetString("UserPhone", phone ?? "");
            HttpContext.Session.SetString("Location", location ?? "");
            HttpContext.Session.SetString("Bio", bio ?? "");
            HttpContext.Session.SetString("LinkedIn", linkedin ?? "");

            // ONLY Admin can change Company Name
            if (role == "Admin")
            {
                HttpContext.Session.SetString("Company", company ?? "TalentTrack");
            }

            // Sync with Database if user is registered in DB
            var currentEmail = HttpContext.Session.GetString("UserEmail");
            if (!string.IsNullOrEmpty(currentEmail))
            {
                var dbUser = _context.UserAccounts.FirstOrDefault(u => u.Email.ToLower() == currentEmail.ToLower());
                if (dbUser != null)
                {
                    dbUser.Name = name ?? "";
                    dbUser.Email = email ?? "";
                    dbUser.Phone = phone ?? "";
                    _context.SaveChanges();
                }
            }

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Landing", "Home");
        }
    }
}