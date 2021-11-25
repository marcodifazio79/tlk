using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class ModemPreConfig
    {
       public string IpAddressToChange { get; set; }

        public int Id { get; set; }
        public string IpAddress { get; set; }
        public long? Imei { get; set; }
        public string Mid { get; set; }
        public DateTime? last_communication { get; set; }
        public DateTime? time_creation { get; set; }

       

    }
}
