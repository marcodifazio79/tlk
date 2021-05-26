using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class MachinesConnectionTrace
    {
        public MachinesConnectionTrace()
        {
            CashTransaction = new HashSet<CashTransaction>();
        }
        public int Id { get; set; }
        public string IpAddress { get; set; }
        public string SendOrRecv { get; set; }
        public string TransferredData { get; set; }
        public int? IdMacchina { get; set; }
        public DateTime? time_stamp { get; set; }
        public virtual Machines IdMacchinaNavigation { get; set; }
        public virtual ICollection<CashTransaction> CashTransaction { get; set; }
        public int telemetria_status { get; set; }
    }
}
