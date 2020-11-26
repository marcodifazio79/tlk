using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class CommandsMatch
    {
        public int Id { get; set; }
        public string ModemCommand { get; set; }
        public string WebCommand { get; set; }
    }
}
