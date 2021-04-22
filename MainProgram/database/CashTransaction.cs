using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class CashTransaction
    {
        public int Id { get; set; }
        public string Odm { get; set; }
        public int IdMachines { get; set; }
        public int? IdMachinesConnectionTrace { get; set; }
        public string Status { get; set; }
        public int? TentativiAutomaticiEseguiti { get; set; }
        public DateTime DataCreazione { get; set; }
        public DateTime? DataInvioRichiesta { get; set; }
        public DateTime? DataPacchettoRicevuto { get; set; }
        public DateTime? DataSincronizzazione { get; set; }
        public virtual MachinesConnectionTrace IdMachinesConnectionTraceNavigation { get; set; }
        public virtual Machines IdMachinesNavigation { get; set; }
    }
}
