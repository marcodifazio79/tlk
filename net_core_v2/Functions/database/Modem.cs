using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class Modem
    {
        public int Id { get; set; }
        public string IpAddress { get; set; }
        public long? Imei { get; set; }
        public string Mid { get; set; }
        public int? KalValue { get; set; }
        public int? LggValue { get; set; }
        public string Version { get; set; }
    }
}
