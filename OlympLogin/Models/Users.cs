using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OlympLogin.Models
{
    public partial class Users
    {
        public int Id { get; set; }
        [Display(Name="Логин")]
        public string Login { get; set; }
        public string Password { get; set; }
        public Role Role { get; set; }
        [Display(Name="Фамилия")]
        public string LastName { get; set; }
        [Display(Name = "Имя")]
        public string FirstName { get; set; }
        [Display(Name = "Отчество")]
        public string MiddleName { get; set; }
        [Display(Name = "Адрес")]
        public string Address { get; set; }
        [Display(Name = "Индекс")]
        public string Index { get; set; }
        public string TerritoryCode { get; set; }
        public string StreetCode { get; set; }
        public int? Building { get; set; }
        public int? Flat { get; set; }

        public Street StreetCodeNavigation { get; set; }
        public Territory TerritoryCodeNavigation { get; set; }
    }

    public enum Role
    {
        User = 0,
        Admin = 1
    }
}
