using System;
using System.Collections.Generic;

namespace tlk_core.database
{
    public partial class SapCashDaemon
    {
        public int Id { get; set; }
        public string CodeMa { get; set; }
        public string OdmTaskPalmare { get; set; }
        public int? TipoDa { get; set; }
        public int? CanaleGettone { get; set; }
        public int? CanaleProve { get; set; }
        public float? Ch1 { get; set; }
        public int? Qty1 { get; set; }
        public float? Ch2 { get; set; }
        public int? Qty2 { get; set; }
        public float? Ch3 { get; set; }
        public int? Qty3 { get; set; }
        public float? Ch4 { get; set; }
        public int? Qty4 { get; set; }
        public float? Ch5 { get; set; }
        public int? Qty5 { get; set; }
        public float? Ch6 { get; set; }
        public int? Qty6 { get; set; }
        public float? Ch7 { get; set; }
        public int? Qty7 { get; set; }
        public float? Ch8 { get; set; }
        public int? Qty8 { get; set; }
        public float? Ch9 { get; set; }
        public int? Qty9 { get; set; }
        public float? MdbVal2 { get; set; }
        public int? MdbInc2 { get; set; }
        public int? MdbTub2 { get; set; }
        public float? MdbVal3 { get; set; }
        public int? MdbInc3 { get; set; }
        public int? MdbTub3 { get; set; }
        public float? MdbVal4 { get; set; }
        public int? MdbInc4 { get; set; }
        public int? MdbTub4 { get; set; }
        public float? MdbVal5 { get; set; }
        public int? MdbInc5 { get; set; }
        public int? MdbTub5 { get; set; }
        public float? MdbVal6 { get; set; }
        public int? MdbInc6 { get; set; }
        public int? MdbTub6 { get; set; }
        public float? Cashless { get; set; }
        public float? Total { get; set; }
        public float? Change { get; set; }
        public int? Sales { get; set; }
        public int? Consumabile { get; set; }
        public int? HopperGettone { get; set; }
        public float? Vend1Prc { get; set; }
        public int? QtyV1 { get; set; }
        public float? Vend2Prc { get; set; }
        public int? QtyV2 { get; set; }
        public int? Ticket { get; set; }
        public float? Price { get; set; }
        public int? Bns1 { get; set; }
        public int? Bns2 { get; set; }
        public int? Bns11 { get; set; }
        public int? Bns21 { get; set; }
        public int? Bns5 { get; set; }
        public int? Bns10 { get; set; }
        public int? Bns20 { get; set; }
        public int? Token { get; set; }
        public float? ContMonViso { get; set; }
        public float? MechValue { get; set; }
        public float? CashlessNayax { get; set; }
        public float? CashlessApp { get; set; }
        public string Status { get; set; }
        public string SapExitCode { get; set; }
        public string TimestampNextTry { get; set; }
        public int? Counter { get; set; }
        public bool? Visible { get; set; }
        public string Message { get; set; }
        public int ForceStop { get; set; }
        public DateTime? DateB { get; set; }
        public DateTime? timestamp_try { get; set; }
        public DateTime? timestamp { get; set; }
    }
}
