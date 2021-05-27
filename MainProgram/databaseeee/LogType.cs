using System;
using System.Collections.Generic;

namespace tlk_core.databaseeee
{
    public partial class LogType
    {
        public LogType()
        {
            Log = new HashSet<Log>();
        }

        public int Id { get; set; }
        public string Type { get; set; }

        public virtual ICollection<Log> Log { get; set; }
    }
}
