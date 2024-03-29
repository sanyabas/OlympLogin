﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize]
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

        // GET: Users/Create
        public IActionResult Register()
        {
            var repo = new AddressRepository(_context);
            var regions = repo.GetRegions();
            var model = new UserRegisterViewModel
            {
                Regions = new SelectList(regions, "Value", "Text")
            };
            ViewData["Title"] = "Регистрация";
            ViewData["Action"] = "Register";
            return View("Register", model);
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
            Console.WriteLine(result.ToList()[0]);
            return Json(new SelectList(result, "Value", "Text"));
        }

        public IActionResult GetBuildings(string street)
        {
            var repo = new AddressRepository(_context);
            var result = repo.GetBuildings(street);
            Console.WriteLine(result.ToList()[0]);
            return Json(new SelectList(result, "Value", "Text"));
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
                    Console.WriteLine(model.SelectedBuilding);
                    var repo = new AddressRepository(_context, model.SelectedRegion);
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
                        Index = index,
                        Building = model.BuildingName,
                        BuildingCode = model.SelectedBuilding,
                        Flat = model.Flat
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
            return _context.Users.FirstOrDefault(user => user.Login == login);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit()
        {
            var user = GetCurrentUser();
            var regionCode = user.TerritoryCode.Substring(0, 2);
            var repo = new AddressRepository(_context, regionCode);
            var regions = repo.GetRegions();
            var cities = repo.GetLocalities();//.Where(city=>city.Value!=user.TerritoryCode);
            var streets = repo.GetStreets(user.TerritoryCode == "0" ? null : user.TerritoryCode);//.Where(str => str.Value != user.StreetCode);
            var buildings = repo.GetBuildings(user.StreetCode=="0"?null : user.StreetCode);//.Where(house => house.Value != user.BuildingCode);
            var model = new UserRegisterViewModel
            {
                Login = user.Login,
                LastName = user.LastName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                Regions = new SelectList(regions, "Value", "Text"),
                SelectedRegion = regionCode,
                Cities = new SelectList(cities, "Value", "Text"),
                SelectedCity = user.TerritoryCode,
                Streets = user.TerritoryCode == "0" ? new SelectList(new[] { new SelectListItem { Value = "0", Text = "Нет улиц" } }, "Value", "Text") : new SelectList(streets, "Value", "Text"),
                SelectedStreet = user.StreetCode,
                Buildings = user.StreetCode == "0" ? new SelectList(new[] { new SelectListItem { Value = "0", Text = "Нет домов" } }, "Value", "Text") : new SelectList(buildings, "Value", "Text"),
                SelectedBuilding = user.BuildingCode,
                BuildingName = user.Building,
                Flat = user.Flat
            };
            ViewData["Title"] = "Изменение";
            ViewData["Action"] = "Edit";
            return View("Edit", model);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = GetCurrentUser();
                    if (user != null)
                    {
                        _context.Users.Update(user);

                        user.Login = model.Login;
                        if (!string.IsNullOrEmpty(model.Password))
                        {
                            var hashed = HashPassword(model.Password);
                            user.Password = hashed;
                        }

                        user.Role = Role.User;
                        user.LastName = model.LastName;
                        user.FirstName = model.FirstName;
                        user.MiddleName = model.MiddleName;
                        //if (!string.IsNullOrEmpty(model.SelectedStreet) || !string.IsNullOrEmpty(model.SelectedCity))
                        //{
                        var repo = new AddressRepository(_context, model.SelectedRegion);
                        var (address, index) = await repo.MakeAddress(model);
                        user.Address = address;
                        user.Index = index;
                        user.Building = model.BuildingName;
                        user.BuildingCode = model.SelectedBuilding;
                        user.StreetCode = model.SelectedStreet;
                        user.TerritoryCode = model.SelectedCity;
                        //}

                        await _context.SaveChangesAsync();

                    }
                }
                catch (DbUpdateConcurrencyException)
                {

                }
                return RedirectToAction(nameof(Index));
            }
            return View("Edit", model);
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
    }
}
