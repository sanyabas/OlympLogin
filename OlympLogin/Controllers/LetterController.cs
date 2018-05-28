using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlympLogin.Models;
using OlympLogin.ViewModels;

namespace OlympLogin.Controllers
{
    [Authorize(Roles="admin")]
    public class LetterController : Controller
    {
        private readonly kladrContext _context;

        public LetterController(kladrContext context)
        {
            _context = context;
        }

        public IActionResult Index(string ids)
        {
            ViewData["ids"] = ids;
            return View();
        }

        [HttpPost]
        public IActionResult Send(LetterVIewModel model)
        {
            
        }
    }
}