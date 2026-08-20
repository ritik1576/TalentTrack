using Microsoft.AspNetCore.Mvc;
using TalentTrack.Data;
using TalentTrack.Models;
using TalentTrack.Services;

namespace TalentTrack.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService? _emailService;

        public AccountController(ApplicationDbContext context, IEmailService? emailService = null)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Admin") return RedirectToAction("Index", "Admin");
            if (role == "Recruiter") return RedirectToAction("Index", "Home");
            if (role == "Interviewer") return RedirectToAction("Dashboard", "Interviewer");

            var demoCandidates = _context.Candidates.OrderBy(c => c.CandidateId).ToList();
            ViewBag.DemoCandidates = demoCandidates;
            if (demoCandidates.Any())
            {
                ViewBag.DemoCandidateEmail = demoCandidates.First().Email;
                ViewBag.DemoCandidatePassword = demoCandidates.First().Phone;
                ViewBag.DemoCandidateName = demoCandidates.First().Name;
            }

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

            // Check Recruiters table
            var userAccount = _context.Recruiters
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

            // Check Candidates table
            var candidate = _context.Candidates
                .FirstOrDefault(c => c.Email.ToLower() == email);
            if (candidate != null)
            {
                var phoneDigits = new string((candidate.Phone ?? "").Where(char.IsDigit).ToArray());
                var passwordDigits = new string(password.Where(char.IsDigit).ToArray());
                if ((!string.IsNullOrEmpty(phoneDigits) && phoneDigits == passwordDigits) || candidate.Phone == password)
                {
                    HttpContext.Session.SetString("UserRole", "Candidate");
                    HttpContext.Session.SetString("UserName", candidate.Name ?? "Candidate");
                    HttpContext.Session.SetString("UserEmail", candidate.Email ?? "");
                    HttpContext.Session.SetString("UserId", candidate.CandidateId.ToString());
                    HttpContext.Session.SetString("UserInitials", candidate.Name?.Length >= 2 ? candidate.Name.Substring(0, 2).ToUpper() : "C");
                    return RedirectToAction("Portal", "CandidateDocument");
                }
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
        public IActionResult Register(Recruiter model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Error = "Please fill in all required fields.";
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                ViewBag.Error = firstError ?? "Invalid inputs. Please verify your details.";
                return View(model);
            }

            if (model.Role == "Candidate")
            {
                if (string.IsNullOrWhiteSpace(model.Phone))
                {
                    ViewBag.Error = "Phone number is required for Candidate registration (it will be used as your Password).";
                    return View(model);
                }

                var digitsOnly = new string(model.Phone.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length != 10)
                {
                    ViewBag.Error = "Phone number must be exactly 10 digits.";
                    return View(model);
                }
                model.Phone = digitsOnly;

                model.Email = model.Email.Trim().ToLower();

                // Check if email already registered in Candidates or Recruiters
                if (_context.Candidates.Any(c => c.Email.ToLower() == model.Email) || _context.Recruiters.Any(u => u.Email.ToLower() == model.Email))
                {
                    ViewBag.Error = "An account with this email address already exists. Please Sign In or use another email.";
                    return View(model);
                }

                var candidate = new Candidate
                {
                    Name = model.Name,
                    Email = model.Email,
                    Phone = model.Phone,
                    Skills = "N/A",
                    Experience = 0,
                    Resume = "Not Uploaded"
                };

                var parts = model.Name.Split(' ', 2);
                candidate.FirstName = parts[0];
                candidate.LastName = parts.Length > 1 ? parts[1] : "";

                _context.Candidates.Add(candidate);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! You can now log in using your Email and Phone Number (Password).";
                return RedirectToAction("Login");
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
            if (_context.Recruiters.Any(u => u.Email.ToLower() == model.Email))
            {
                ViewBag.Error = "An account with this email address already exists. Please Sign In or use another email.";
                return View(model);
            }

            model.Status = "Pending";
            model.IsApproved = false;
            model.CreatedAt = DateTime.Now;
            model.CreatedBy = "Self";

            _context.Recruiters.Add(model);

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
            Recruiter? dbUser = null;
            if (!string.IsNullOrEmpty(currentEmail))
            {
                dbUser = _context.Recruiters.FirstOrDefault(u => u.Email.ToLower() == currentEmail.ToLower());
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

            // Validation for Name: must contain only letters and spaces
            if (string.IsNullOrWhiteSpace(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z\s]+$"))
            {
                TempData["Error"] = "Name must contain only alphabets and spaces.";
                return RedirectToAction("Profile");
            }

            // Validation for Email: must be clean
            if (string.IsNullOrWhiteSpace(email) || !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("Profile");
            }

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
                var dbUser = _context.Recruiters.FirstOrDefault(u => u.Email.ToLower() == currentEmail.ToLower());
                if (dbUser != null)
                {
                    dbUser.Name = name ?? "";
                    dbUser.Email = email ?? "";
                    dbUser.Phone = phone ?? "";
                    dbUser.UpdatedAt = DateTime.Now;
                    dbUser.UpdatedBy = name ?? dbUser.Name;
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your Email address.";
                return View();
            }

            email = email.Trim().ToLower();

            // Check Recruiters
            var recruiter = _context.Recruiters.FirstOrDefault(u => u.Email.ToLower() == email);
            var candidate = _context.Candidates.FirstOrDefault(c => c.Email.ToLower() == email);

            if (recruiter == null && candidate == null)
            {
                ViewBag.Error = "We could not find an account with this Email address.";
                return View();
            }

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.Now.AddMinutes(10);

            if (recruiter != null)
            {
                recruiter.ResetOTP = otp;
                recruiter.ResetOTPExpiry = expiry;
                _context.Recruiters.Update(recruiter);
            }
            if (candidate != null)
            {
                candidate.ResetOTP = otp;
                candidate.ResetOTPExpiry = expiry;
                _context.Candidates.Update(candidate);
            }
            _context.SaveChanges();

            // Send actual email using IEmailService
            string emailSubject = "TalentTrack Password Reset OTP";
            string emailBodyHtml = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; background-color: #ffffff;'>
                    <div style='text-align: center; border-bottom: 2px solid #007bff; padding-bottom: 15px; margin-bottom: 20px;'>
                        <h2 style='color: #007bff; margin: 0;'>TalentTrack Security</h2>
                    </div>
                    <p>Hello,</p>
                    <p>We received a request to reset the password for your TalentTrack account.</p>
                    <p>Please use the following 6-digit One-Time Password (OTP) to proceed with resetting your password:</p>
                    <div style='background-color: #f8f9fa; border: 1px dashed #007bff; padding: 15px; text-align: center; font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #007bff; border-radius: 4px; margin: 20px 0;'>
                        {otp}
                    </div>
                    <p>This OTP is valid for <strong>10 minutes</strong>. If you did not request a password reset, you can safely ignore this email.</p>
                    <p style='color: #dc3545; font-size: 13px; font-weight: bold;'>Security Notice: Never share this OTP with anyone.</p>
                    <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #6c757d; line-height: 1.5;'>
                        Best regards,<br/>
                        <strong>TalentTrack Support Team</strong><br/>
                        This is an automated message, please do not reply directly to this email.
                    </p>
                </div>";

            if (_emailService != null)
            {
                // Run email delivery in a background thread so the HTTP response is sent instantly
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendEmailAsync(email, emailSubject, emailBodyHtml);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOG] Background Email Failed to {email}: {ex.Message}");
                    }
                });
            }

            // Simulate sending email
            var emailLogPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "simulated_emails.txt");
            var emailDirectory = System.IO.Path.GetDirectoryName(emailLogPath);
            if (!string.IsNullOrEmpty(emailDirectory) && !System.IO.Directory.Exists(emailDirectory))
            {
                System.IO.Directory.CreateDirectory(emailDirectory);
            }
            string emailBody = $@"
============================================================
SIMULATED EMAIL SENT
Date: {DateTime.Now:dd MMM yyyy, hh:mm tt}
To: {email}
Subject: TalentTrack Password Reset OTP
Body:
Hello,

You have requested to reset your password. 
Your 6-digit One Time Password (OTP) is: {otp}

This OTP is valid for 10 minutes.

Best regards,
TalentTrack System
============================================================
";
            System.IO.File.AppendAllText(emailLogPath, emailBody);

            return RedirectToAction("ResetPassword", new { email = email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("ForgotPassword");
            }
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string email, string otp, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Please fill in all fields.";
                ViewBag.Email = email;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                ViewBag.Email = email;
                return View();
            }

            email = email.Trim().ToLower();

            var recruiter = _context.Recruiters.FirstOrDefault(u => u.Email.ToLower() == email);
            var candidate = _context.Candidates.FirstOrDefault(c => c.Email.ToLower() == email);

            bool isValidOtp = false;

            if (recruiter != null)
            {
                if (recruiter.ResetOTP == otp && recruiter.ResetOTPExpiry.HasValue && recruiter.ResetOTPExpiry.Value > DateTime.Now)
                {
                    isValidOtp = true;
                    recruiter.Password = newPassword;
                    recruiter.ResetOTP = null;
                    recruiter.ResetOTPExpiry = null;
                    _context.Recruiters.Update(recruiter);

                    // If user is also an interviewer, update in Interviewers table to prevent desync
                    var interviewer = _context.Interviewers.FirstOrDefault(i => i.Email.ToLower() == email);
                    if (interviewer != null)
                    {
                        interviewer.Password = newPassword;
                        _context.Interviewers.Update(interviewer);
                    }
                }
            }
            else if (candidate != null)
            {
                if (candidate.ResetOTP == otp && candidate.ResetOTPExpiry.HasValue && candidate.ResetOTPExpiry.Value > DateTime.Now)
                {
                    isValidOtp = true;
                    candidate.Phone = newPassword; // Phone acts as password for Candidates
                    candidate.ResetOTP = null;
                    candidate.ResetOTPExpiry = null;
                    _context.Candidates.Update(candidate);
                }
            }

            if (!isValidOtp)
            {
                ViewBag.Error = "Invalid or expired OTP.";
                ViewBag.Email = email;
                return View();
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Password reset successfully! Please sign in with your new password.";
            return RedirectToAction("Login");
        }
    }
}