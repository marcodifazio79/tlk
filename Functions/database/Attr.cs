using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class Attr
    {
        public Attr()
        {
            MachinesAttributes = new HashSet<MachinesAttributes>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }

        public virtual ICollection<MachinesAttributes> MachinesAttributes { get; set; }
    }
}
