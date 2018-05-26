using System;
using System.Collections.Generic;

namespace OlympLogin.Models
{
    public partial class Territory
    {
        public Territory()
        {
            Users = new HashSet<Users>();
        }

        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string Code { get; set; }
        public string Index { get; set; }
        public string Status { get; set; }

        public ICollection<Users> Users { get; set; }
    }
}
