using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OlympLogin.Data;
using OlympLogin.Models;
using OlympLogin.ViewModels;

namespace OlympLogin.Controllers
{
    public class UsersController : Controller
    {
        private readonly kladrContext _context;

        public UsersController(kladrContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var user = GetCurrentUser();
            ViewData["Login"] = user.Login;
            if (user.Role == Role.Admin)
            {
                var users = _context.Users.AsNoTracking().Where(u => u.Role == Role.User);
                
                //var kladrContext = _context.Users.Include(u => u.StreetCodeNavigation).Include(u => u.TerritoryCodeNavigation);
                return View(await users.ToListAsync());
            }

            return View("Details", user);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var users = await _context.Users
                .Include(u => u.StreetCodeNavigation)
                .Include(u => u.TerritoryCodeNavigation)
                .SingleOrDefaultAsync(m => m.Id == id);
            if (users == null)
            {
                return NotFound();
            }

            return View(users);
        }

        // GET: Users/Create
        public IActionResult Register()
        {
            var tip = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = null,
                    Text = "Выберите регион"
                }
            };
            var repo=new AddressRepository(_context);
            var regions = repo.GetRegions();
            tip.AddRange(regions);
            var model = new UserRegisterViewModel
            {
                Regions = new SelectList(tip, "Value", "Text")
            };
            return View("Register", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == model.Login);
                if (user == null)
                {
                    var repo=new AddressRepository(_context, model.SelectedRegion);
                    var hashed = HashPassword(model.Password);
                    var (address, index) = await repo.MakeAddress(model);
                    _context.Users.Add(new Users
                    {
                        Login = model.Login,
                        Password = hashed,
                        Role = Role.User,
                        LastName = model.LastName,
                        FirstName = model.FirstName,
                        MiddleName = model.MiddleName,
                        TerritoryCode = model.SelectedCity,
                        StreetCode = model.SelectedStreet,
                        Address = address,
                        Index=index
                    });
                    await _context.SaveChangesAsync();
                    await Authenticate(model.Login);
                    return RedirectToAction(nameof(Index));

                }

                Console.WriteLine(user);

                ModelState.AddModelError("", "Пользователь уже существует");

            }

            return View(model);
        }

        private async Task Authenticate(string login)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, login)
            };

            if ((await _context.Users.FirstAsync(u => u.Login == login)).Role == Role.Admin)
                claims.Add(new Claim(ClaimTypes.Role, "admin"));

            var id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private Users GetCurrentUser()
        {
            var login = HttpContext.User.Identity.Name;
            return _context.Users.First(user => user.Login == login);
        }

        private string MakeAddress(UserRegisterViewModel model)
        {
            var result =
                $"{model.SelectedRegion}, {model.SelectedCity}, {model.SelectedStreet}, д. {model.Building}, кв. {model.Flat}";
            return result;
        }

        public IEnumerable<SelectListItem> GetCities(string region)
        {
            if (string.IsNullOrEmpty(region))
                return null;
            var cityReg = new Regex($"{region}\\d{6}0{5}");
            var cities = _context.Territory.AsNoTracking()
                .Join(_context.Abbreviation, t => t.Abbreviation, a => a.ShortName, (terr, abbr) => new
                {
                    Name = terr.Name,
                    Type = abbr.FullName,
                    Code = terr.Code,
                    Level = abbr.Level,
                    Status = terr.Status
                })
                .Where(ter => cityReg.IsMatch(ter.Code) && ter.Level == "3")
                .OrderBy(ter => ter.Name)
                .Select(ter => new SelectListItem
                {
                    Value = ter.Code,
                    Text = $"{ter.Type} {ter.Name}"
                }).ToList();
            return cities;
            //return Json(new SelectList(cities, "Value", "Text"));
        }

        //public IEnumerable<SelectListItem> GetVillages(string region)
        //{
        //    if (string.IsNullOrEmpty(region))
        //        return null;

        //}

        public IActionResult GetRayons(string region)
        {
            if (string.IsNullOrEmpty(region))
                return null;
            var rayonReg = new Regex($"{region}\\d{6}0{5}");
            var rayons = _context.Territory.AsNoTracking()
                .Join(_context.Abbreviation, t => t.Abbreviation, a => a.ShortName, (terr, abbr) => new
                {
                    Name = terr.Name,
                    Type = abbr.FullName,
                    Code = terr.Code,
                    Level = abbr.Level,
                    Status = terr.Status
                })
                .Where(ter => rayonReg.IsMatch(ter.Code) && ter.Level == "2")
                .OrderBy(ter => ter.Name)
                .Select(ter => new SelectListItem
                {
                    Value = ter.Code,
                    Text = $"{ter.Name} {ter.Type}"
                }).ToList();
            return Json(new SelectList(rayons, "Value", "Text"));
        }

        public IActionResult GetLocalities(string region)
        {
            Console.WriteLine(region);
            var repo = new AddressRepository(_context, region);
            var result = repo.GetLocalities();
            return Json(new SelectList(result, "Value", "Text"));
        }

        public IActionResult GetStreets(string city)
        {
            Console.WriteLine(city);
            var repo = new AddressRepository(_context);
            var result = repo.GetStreets(city);
            return Json(new SelectList(result, "Value", "Text"));
        }

        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,LastName,FirstName,MiddleName,TerritoryCode,StreetCode,Building,Flat")] Users users)
        {
            if (ModelState.IsValid)
            {
                _context.Add(users);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["StreetCode"] = new SelectList(_context.Street, "Code", "Code", users.StreetCode);
            ViewData["TerritoryCode"] = new SelectList(_context.Territory, "Code", "Code", users.TerritoryCode);
            return View(users);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var users = await _context.Users.SingleOrDefaultAsync(m => m.Id == id);
            if (users == null)
            {
                return NotFound();
            }
            ViewData["StreetCode"] = new SelectList(_context.Street, "Code", "Code", users.StreetCode);
            ViewData["TerritoryCode"] = new SelectList(_context.Territory, "Code", "Code", users.TerritoryCode);
            return View(users);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LastName,FirstName,MiddleName,TerritoryCode,StreetCode,Building,Flat")] Users users)
        {
            if (id != users.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(users);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsersExists(users.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["StreetCode"] = new SelectList(_context.Street, "Code", "Code", users.StreetCode);
            ViewData["TerritoryCode"] = new SelectList(_context.Territory, "Code", "Code", users.TerritoryCode);
            return View(users);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var users = await _context.Users
                .Include(u => u.StreetCodeNavigation)
                .Include(u => u.TerritoryCodeNavigation)
                .SingleOrDefaultAsync(m => m.Id == id);
            if (users == null)
            {
                return NotFound();
            }

            return View(users);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var users = await _context.Users.SingleOrDefaultAsync(m => m.Id == id);
            _context.Users.Remove(users);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var hashed = HashPassword(model.Password);
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Login == model.Login && u.Password == hashed);
                if (user != null)
                {
                    await Authenticate(model.Login);

                    return RedirectToAction("Index");
                }
                ModelState.AddModelError("", "Неверные логин и/или пароль");
            }

            return View(model);
        }

        private string HashPassword(string password)
        {
            var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: new byte[] { 0, 5, 2, 14 },
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            return hashed;
        }

        private bool UsersExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
