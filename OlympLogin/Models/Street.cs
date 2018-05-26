using System;
using System.Collections.Generic;

namespace OlympLogin.Models
{
    public partial class Street
    {
        public Street()
        {
            Users = new HashSet<Users>();
        }

        public string Name { get; set; }
        public string Abbr { get; set; }
        public string Code { get; set; }
        public string Index { get; set; }

        public ICollection<Users> Users { get; set; }
    }
}
