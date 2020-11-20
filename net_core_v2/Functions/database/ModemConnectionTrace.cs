using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class ModemConnectionTrace
    {
        public int Id { get; set; }
        public string IpAddress { get; set; }
        public string SendOrRecv { get; set; }
        public string TransferredData { get; set; }
    }
}
