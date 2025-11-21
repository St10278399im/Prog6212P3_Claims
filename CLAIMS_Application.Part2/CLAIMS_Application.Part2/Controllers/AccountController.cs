using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using CLAIMS_Application.Part2.Models;
using System.Security.Claims;

namespace CLAIMS_Application.Part2.Controllers
{
    public class AccountController : Controller
    {
        private static List<User> _users = new List<User>();
        private static int _nextUserId = 1;

        // ---------------------------
        // STATIC CONSTRUCTOR ADDED ✔
        // ---------------------------
        static AccountController()
        {
            if (_users.Count == 0)
            {
                _users.AddRange(new List<User>
                {
                    new User
                    {
                        Id = 1,
                        FirstName = "John",
                        LastName = "Doe",
                        Email = "JDlecturer@work.com",
                        Password = "11",
                        Role = "Lecturer",
                        CreatedDate = DateTime.Now
                    },
                    new User
                    {
                        Id = 2,
                        FirstName = "Sarah",
                        LastName = "Kyle",
                        Email = "SKcoordinator@work.com",
                        Password = "22",
                        Role = "ProgrammeCoordinator",
                        CreatedDate = DateTime.Now
                    },
                    new User
                    {
                        Id = 3,
                        FirstName = "Adam",
                        LastName = "Sandler",
                        Email = "ASadmin@work.com",
                        Password = "33",
                        Role = "Administrator",
                        CreatedDate = DateTime.Now
                    },
                    new User
                    {
                        Id = 4,
                        FirstName = "Jenny",
                        LastName = "Mace",
                        Email = "JMhr@work.com",
                        Password = "44",
                        Role = "HR",
                        CreatedDate = DateTime.Now
                    }
                });

                _nextUserId = 5;
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "User with this email already exists.");
                return View(model);
            }

            var newUser = new User
            {
                Id = _nextUserId++,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password,
                Role = model.Role ?? "Lecturer",
                CreatedDate = DateTime.Now
            };

            _users.Add(newUser);

            TempData["SuccessMessage"] = "Registration successful. Please login.";
            return RedirectToAction("Login");
        }

        // Static helpers
        public static List<User> GetUsers()
        {
            return _users;
        }

        public static int GetNextUserId()
        {
            return _nextUserId++;
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
