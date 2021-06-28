using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class Log
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.DisplayFormat(DataFormatString = "{0:dd/MM/yyyy - HH:mm:ss}")]
        public DateTime DataCreazione { get; set; }
        [System.ComponentModel.DataAnnotations.DisplayFormat(DataFormatString = "{0:dd/MM/yyyy - HH:mm:ss}")]
        public DateTime DataRisoluzione { get; set; }
        public string LogDescription { get; set; }
        public string LogSeggestedActions { get; set; }
        public string LinkToRelevantLocation { get; set; }
        public int IdLogType { get; set; }
        public int IdLogStatus { get; set; }
        public string IdUser { get; set; }
        public int? IdMachine { get; set; }
        public virtual Machines IdMachineNavigation { get; set; }
        public virtual AspNetUsers IdUserNavigation { get; set; }
    }
}
