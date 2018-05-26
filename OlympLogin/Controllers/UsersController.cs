using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
            var kladrContext = _context.Users.Include(u => u.StreetCodeNavigation).Include(u => u.TerritoryCodeNavigation);
            return View(await kladrContext.ToListAsync());
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
        public IActionResult Create()
        {
            var regions = _context.Regions.AsNoTracking()
                .OrderBy(region => region.Name)
                .Select(region =>
                    new SelectListItem
                    {
                        Value = region.Code,
                        Text = region.Name
                    }).ToList();
            var model = new UserRegisterViewModel
            {
                Regions = new SelectList(regions, "Value", "Text")
            };
            //ViewData["StreetCode"] = new SelectList(_context.Street, "Code", "Code");
            //ViewData["TerritoryCode"] = new SelectList(_context.Territory, "Code", "Code");
            return View("Register", model);
        }

        public IActionResult GetCities(string region)
        {
            if (string.IsNullOrEmpty(region))
                return null;
            var cities = _context.Territory.AsNoTracking()
                .Join(_context.Abbreviation, t=>t.Abbreviation, a=>a.ShortName, (terr, abbr)=>new
                {
                    Name=terr.Name,
                    Type=abbr.FullName,
                    Code=terr.Code,
                    Level=abbr.Level,
                    Status=terr.Status
                })
                .Where(ter => ter.Code.StartsWith(region) && ter.Level=="4")
                .OrderBy(ter=>ter.Name)
                .Select(ter=> new SelectListItem
                {
                    Value=ter.Code,
                    Text = $"{ter.Type} {ter.Name}"
                }).ToList();
            return Json(new SelectList(cities, "Value", "Text"));
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

        private bool UsersExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
