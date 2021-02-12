using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class MachinesInMemory
    {
        public int Id { get; set; }
        public string IpAddress { get; set; }
        public int TcpLocalPort { get; set; }
        public string Mid { get; set; }
    }
}
