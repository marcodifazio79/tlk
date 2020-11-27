using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class MachinesAttributes
    {
        public int Id { get; set; }
        public int IdMacchina { get; set; }
        public int IdAttribute { get; set; }
        public string Value { get; set; }

        public virtual Attr IdAttributeNavigation { get; set; }
        public virtual Machines IdMacchinaNavigation { get; set; }
    }
}
