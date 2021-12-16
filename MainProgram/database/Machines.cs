using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class Machines
    {
        public Machines()
        {
            CashTransaction = new HashSet<CashTransaction>();
            MachinesAttributes = new HashSet<MachinesAttributes>();
            MachinesConnectionTrace = new HashSet<MachinesConnectionTrace>();
            RemoteCommand = new HashSet<RemoteCommand>();
        }

        public int Id { get; set; }
        public string IpAddress { get; set; }
        public long? Imei { get; set; }
        public string Mid { get; set; }
        public string Version { get; set; }
        public bool IsOnline { get; set; }
        public bool MarkedBroken { get; set; }
        public bool LogEnabled { get; set; }

        public long? Sim_Serial{ get; set; }
        public DateTime? last_communication { get; set; }
        public DateTime? time_creation { get; set; }

        public virtual ICollection<MachinesConnectionTrace> MachinesConnectionTrace { get; set; }
        public virtual ICollection<RemoteCommand> RemoteCommand { get; set; }
        public virtual ICollection<MachinesAttributes> MachinesAttributes { get; set; }
        public virtual ICollection<CashTransaction> CashTransaction { get; set; }

    }
}
