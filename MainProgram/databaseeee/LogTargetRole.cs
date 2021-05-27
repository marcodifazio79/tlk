using System;
using System.Collections.Generic;

namespace tlk_core.databaseeee
{
    public partial class LogTargetRole
    {
        public int Id { get; set; }
        public string IdAspNetRoles { get; set; }
        public int IdLog { get; set; }

        public virtual AspNetRoles IdAspNetRolesNavigation { get; set; }
        public virtual Log IdLogNavigation { get; set; }
    }
}
