using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class Machines
    {
        public Machines()
        {
            MachinesConnectionTrace = new HashSet<MachinesConnectionTrace>();
            RemoteCommand = new HashSet<RemoteCommand>();
        }

        public int Id { get; set; }
        public string IpAddress { get; set; }
        public long? Imei { get; set; }
        public string Mid { get; set; }
        public int? KalValue { get; set; }
        public int? LggValue { get; set; }
        public string Version { get; set; }

        public DateTime? last_communication { get; set; }
        public DateTime? time_creation { get; set; }


        public virtual ICollection<MachinesConnectionTrace> MachinesConnectionTrace { get; set; }
        public virtual ICollection<RemoteCommand> RemoteCommand { get; set; }
    }
}
