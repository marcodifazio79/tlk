using System;
using System.Net;  
using System.Net.Sockets;
using System.Collections.Generic;
using System.Xml;
using System.Threading;

using Functions.database;

using System.Linq;

using ServiceReference;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Functions
{
    public class sapInterface
    {
        public static void miao()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            Uri u = new Uri("http://disp.servicesdedem.it/sap/bc/srt/rfc/sap/zpm_rfc_consumi_telemetria_400/001/zpm_rfc_consumi_telemetria_400/zpm_rfc_consumi_telemetria_400");
            EndpointAddress endpointAddress = new EndpointAddress(u);
            var v = new ZpmRfcConsumiTelemetria400();
            v.Posizione = new ZgeTelCassaItem[1];
            v.Posizione.Append( 
                new ZgeTelCassaItem{
                    Product = "1",
                    Sales = 2,
                    Test = 3,
                    Refund = 4,
                    Prezzo = 5
                    }
                );
            v.Testata = new ZgeTelCassaHeader();
            v.Testata.MandtGw = "300";
            v.Testata.CodeMa = "00023015";
            v.Testata.OdmTaskPalmare = "YCANDELA1603292322748";
            v.Testata.IdTelemetria = "301922";
            v.Testata.DateB =  DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
            v.Testata.TipoDa = "1";
            v.Testata.CanaleGettone = "1";
            v.Testata.CanaleProve = "8";
            v.Testata.Qty1 = 1;
            v.Testata.Ch2 = 1;
            v.Testata.Qty2 = 1;
            v.Testata.Ch3 = 1;
            v.Testata.Qty3 = 1;
            v.Testata.Ch4 = 1;
            v.Testata.Qty4 = 1;
            v.Testata.Ch5 = 1;
            v.Testata.Qty5 = 1;
            v.Testata.Ch6 = 1;
            v.Testata.Qty6 = 1;
            v.Testata.Ch7 = 1;
            v.Testata.Qty7 = 1;
            v.Testata.Ch8 = 1;
            v.Testata.Qty8 = 1;
            v.Testata.Ch9 = 1;
            v.Testata.Qty9 = 1;
            v.Testata.MdbVal2 = 1;
            v.Testata.MdbInc2 = 1;
            v.Testata.MdbTub2 = 1;
            v.Testata.MdbVal3 = 1;
            v.Testata.MdbInc3 = 1;
            v.Testata.MdbTub3 = 1;
            v.Testata.MdbVal4 = 1;
            v.Testata.MdbInc4 = 1; 
            v.Testata.MdbTub4 = 1;
            v.Testata.MdbVal5 = 1;
            v.Testata.MdbInc5 = 1;
            v.Testata.MdbTub5 = 1;
            v.Testata.MdbVal6 = 1;
            v.Testata.MdbInc6 = 1;
            v.Testata.MdbTub6 = 1;
            v.Testata.Cashless = 1;
            v.Testata.Total = 1;
            v.Testata.Change = 1;
            v.Testata.Sales = 1;
            v.Testata.Consumabile = 1;
            v.Testata.HopperGettone = "1";
            v.Testata.Vend1Prc = 1;
            v.Testata.QtyV1 = 1;
            v.Testata.Vend2Prc = 1;
            v.Testata.QtyV2 = 1;
            v.Testata.Ticket = 1;
            v.Testata.Price = 1;
            v.Testata.Bns1 = 1;
            v.Testata.Bns2 = 1;
            v.Testata.BNS_1 = 1;
            v.Testata.BNS_2 = 1;
            v.Testata.Bns5 = 1;
            v.Testata.Bns10 = 1;
            v.Testata.Bns20 = 1;
            v.Testata.Token = 1;
            v.Testata.ContMonViso = 1;
            v.Testata.MechValue = 1;
            v.Testata.CashlessNayax = 1;
            v.Testata.CashlessApp = 1;

            var client = new ZPM_RFC_CONSUMI_TELEMETRIA_400Client(   binding  ,  endpointAddress );
            client.Endpoint.Binding.SendTimeout = new TimeSpan(0, 5, 30);
            client.ClientCredentials.UserName.UserName = "tel_webserv"; 
            client.ClientCredentials.UserName.Password = "cbr900rr_GWP";
            sendCasToSap(v, client);
        }

        public static async void sendCasToSap(ZpmRfcConsumiTelemetria400 v, ZPM_RFC_CONSUMI_TELEMETRIA_400Client client)
        {
            var response = await client.ZpmRfcConsumiTelemetria400Async(  v  );
            Console.WriteLine(response.ZpmRfcConsumiTelemetria400Response.ToString());
        }

    }
}