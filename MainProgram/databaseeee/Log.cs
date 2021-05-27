using System;
using System.Collections.Generic;

namespace tlk_core.databaseeee
{
    public partial class Log
    {
        public Log()
        {
            LogTargetRole = new HashSet<LogTargetRole>();
        }

        public int Id { get; set; }
        public string LogDescription { get; set; }
        public string LogSeggestedActions { get; set; }
        public string LinkToRelevantLocation { get; set; }
        public int IdLogType { get; set; }
        public int IdLogStatus { get; set; }
        public string IdUser { get; set; }
        public int? IdMachine { get; set; }

        public virtual LogStatus IdLogStatusNavigation { get; set; }
        public virtual LogType IdLogTypeNavigation { get; set; }
        public virtual Machines IdMachineNavigation { get; set; }
        public virtual AspNetUsers IdUserNavigation { get; set; }
        public virtual ICollection<LogTargetRole> LogTargetRole { get; set; }
    }
}
