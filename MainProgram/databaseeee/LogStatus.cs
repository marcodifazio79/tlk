using System;
using System.Collections.Generic;

namespace tlk_core.databaseeee
{
    public partial class LogStatus
    {
        public LogStatus()
        {
            Log = new HashSet<Log>();
        }

        public int Id { get; set; }
        public string Status { get; set; }

        public virtual ICollection<Log> Log { get; set; }
    }
}
