using System;
using System.Collections.Generic;

namespace tlk_core.database
{
    public partial class ModemInMemory
    {
        public int Id { get; set; }
        public string IpAddress { get; set; }
        public int TcpLocalPort { get; set; }
        public string Mid { get; set; }
    }
}
