﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OlympLogin.ViewModels
{
    public class UserRegisterViewModel
    {
        [Display(Name = "Login")] public string Login { get; set; }
        [Display(Name = "Password")] public string Password { get; set; }
        [Display(Name = "First name")] public string FirstName { get; set; }
        [Display(Name = "Last name")] public string LastName { get; set; }
        [Display(Name = "Middle name")] public string MiddleName { get; set; }

        [Display(Name = "Регион")] public string SelectedRegion { get; set; }
        public IEnumerable<SelectListItem> Regions { get; set; }

        [Display(Name = "Город")] public string SelectedCity { get; set; }
        public IEnumerable<SelectListItem> Cities { get; set; }
    }
}