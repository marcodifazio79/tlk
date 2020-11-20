using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class RemoteCommand
    {
        public int Id { get; set; }
        public string Body { get; set; }
        public string Sender { get; set; }
        public int IdMacchina { get; set; }
        public int? LifespanSeconds { get; set; }
        public string Status { get; set; }
    }
}
