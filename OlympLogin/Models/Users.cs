using System;
using System.Collections.Generic;

namespace OlympLogin.Models
{
    public partial class Users
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string TerritoryCode { get; set; }
        public string StreetCode { get; set; }
        public int? Building { get; set; }
        public int? Flat { get; set; }

        public Street StreetCodeNavigation { get; set; }
        public Territory TerritoryCodeNavigation { get; set; }
    }
}
