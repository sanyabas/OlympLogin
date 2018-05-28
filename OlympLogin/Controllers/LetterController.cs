using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlympLogin.Models;
using OlympLogin.ViewModels;
using SautinSoft.Document;

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
            var docx = new DocumentCore();
            foreach (var id in model.Ids.Split('.'))
            {
                var section = GetSectionForUser(docx, _context.Users.Single(x => x.Id == int.Parse(id)));
                var par = new Paragraph(docx);
                section.Blocks.Add(par);


                var separators = new List<char>();
                for (var i = 0; i < 32; i++)
                    separators.Add((char) i);

                for (var i = 0; i < 4; i++)
                {
                    var run = new SpecialCharacter(docx, SpecialCharacterType.LineBreak);
                    par.Inlines.Add(run);
                }

                var text = model.Text.Split(separators.ToArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (var str in text)
                {
                    var run = new Run(docx, str);
                    par.Inlines.Add(run);
                    var @break = new SpecialCharacter(docx, SpecialCharacterType.LineBreak);
                    par.Inlines.Add(@break);
                }

                par.ParagraphFormat.Alignment = HorizontalAlignment.Center;
                docx.Sections.Add(section);
            }

            var filename = Path.GetTempFileName();
            docx.Save(filename, SaveOptions.DocxDefault);
            return File(System.IO.File.ReadAllBytes(filename), "application/msword", "letter.docx");
        }

        private static Section GetSectionForUser(DocumentCore docx, Users user)
        {
            var section = new Section(docx) { PageSetup = { PaperType = PaperType.A4 } };

            var par = new Paragraph(docx);
            section.Blocks.Add(par);

            foreach (var str in new[]
                {user.Index, user.Address, $"{user.LastName} {user.FirstName} {user.MiddleName}"})
            {
                if (string.IsNullOrEmpty(str)) continue;
                var run = new Run(docx, str);
                par.Inlines.Add(run);
                par.Inlines.Add(new SpecialCharacter(docx, SpecialCharacterType.LineBreak));
            }

            par.ParagraphFormat.Alignment = HorizontalAlignment.Right;

            return section;
        }
    }
}