using System;
using System.Collections.Generic;

namespace tlk_core.database
{
    public partial class SapCashProducts
    {
        public int Id { get; set; }
        public int CashId { get; set; }
        public string Product { get; set; }
        public int? Sales { get; set; }
        public int? Test { get; set; }
        public int? Prezzo { get; set; }
        public int? Refund { get; set; }
        public int? Status { get; set; }
        public DateTime? timestamp { get; set; }
    }
}
