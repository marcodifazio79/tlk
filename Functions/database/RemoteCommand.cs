using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class RemoteCommand
    {
        public int Id { get; set; }
        public string Body { get; set; }
        public string Sender { get; set; }
        public int? IdMacchina { get; set; }
        public int? LifespanSeconds { get; set; }
        public string Status { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public DateTime? SendedAt { get; set; }
        public DateTime? AnsweredAt { get; set; }

        public virtual Machines IdMacchinaNavigation { get; set; }
    }
}
