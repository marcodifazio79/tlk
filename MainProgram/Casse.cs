using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Functions.database;
using Microsoft.Extensions.Configuration;
using MySql.Data;
using MySql.Data.MySqlClient;

//
// le casse le carico su un database, un servizio di 
// *qualcuno* si occuperà poi di caricarle su SAP
//

namespace Casse
{
    public class CasseFunctions
    {   
        public static IConfiguration Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();      
        public CasseFunctions()
        {
        }
        
        /// <summary>
        /// RegistrazioneCassa viene chiamato quando ricevo un pacchetto di cassa.
        /// Inizia qui l'elaborazione per caricarlo sul DB di Deborah
        /// </summary>
        public static void RegistrazioneCassa(int id_MachinesConnectionTrace)
        {
            listener_DBContext DB = new listener_DBContext();
            int id_machine = 0;
            try
            {
                // first thing first let's check if a cash transaction as been requested in the last few minute,
                // otherwise it's just someone sending cash request for "fun"
                MachinesConnectionTrace MCT = DB.MachinesConnectionTrace.Single(s=>s.Id == id_MachinesConnectionTrace);
                id_machine = Convert.ToInt32( MCT.IdMacchina);
                CashTransaction LastTransactionForModem =  DB.CashTransaction.Where(s=>s.IdMachines == id_machine)
                                                                            .OrderBy(h=>h.Id)
                                                                            .Last(s=>s.IdMachines == id_machine);
                
                if(LastTransactionForModem.Status == "CashRequestSentToModem")
                {
                    LastTransactionForModem.IdMachinesConnectionTrace = id_MachinesConnectionTrace;
                    LastTransactionForModem.Status = "CashPacketReceivedFromModem";
                    LastTransactionForModem.DataPacchettoRicevuto = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                    DB.SaveChanges();
                    new Thread(()=>
                        loadCashPacketToDeborahDB(LastTransactionForModem.Id)
                        ).Start();
                    
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " RegistrazioneCassa: "+e.Message);
            }
            finally
            {
                DB.SaveChanges();
                DB.DisposeAsync();
                try{
                    new Thread(()=>
                        Functions.SignalRSender.AskToReloadCashTransactionTable ( id_machine )
                        ).Start();
                    }
                catch(Exception e){
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " RegistrazioneCassa: "+e.Message);
                }
            }
        }
        
        /// <summary>
        /// qui scriverò la funzione per caricare la cassa sul db di deborah 
        ///(cassa che andrà calcolata prima sulla base di cashPacket)
        /// </summary>
        public static void loadCashPacketToDeborahDB(int cashTransactionID)
        {
            listener_DBContext DB = new listener_DBContext (); 
            int machine_id = 0;
            try{
                CashTransaction transaction = DB.CashTransaction
                                                .Include(j=>j.IdMachinesConnectionTraceNavigation)
                                                .ThenInclude(j=>j.IdMacchinaNavigation)
                                                .Single(s=>s.Id == cashTransactionID);
                machine_id = transaction.IdMachines;
                string[] splittedCashPacket = transaction.IdMachinesConnectionTraceNavigation.TransferredData.Split(',');
                int loading_result = 0;
                if(splittedCashPacket.Length == 40)
                {
                    //controllo se è il l`unico pacchetto di cassa mai ricevuto da quella macchina o meno
                    if(DB.CashTransaction.Select(s=>s.IdMachines == transaction.IdMachines).Count()>1)
                    {
                        CashTransaction previousTransaction = DB.CashTransaction.OrderByDescending(s=>s.Id)
                            .Where(s=>s.IdMachines == transaction.IdMachines)
                            .Where(g=>g.IdMachinesConnectionTrace != null)
                            .Include(j=>j.IdMachinesConnectionTraceNavigation)
                            .ThenInclude(j=>j.IdMacchinaNavigation)
                            .Take(2)
                            .Last();
                        loading_result = buildPacket_better(transaction, previousTransaction);
                    }
                    else
                    {
                        loading_result = buildPacket_better(transaction);
                    }
                    
                    if(loading_result == 0)
                        transaction.Status = "Inviata";
                    else
                        transaction.Status = "Errore =(";
                }
                else{
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " wrong cash packet length (!=40)");
                    transaction.Status = "bad cash packet";
                }
            }catch (Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " loadCashPacketToDeborahDB error " + e.Message);
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " loadCashPacketToDeborahDB error " + e.StackTrace);
            }
            finally
            {
                DB.SaveChanges();
                DB.DisposeAsync();
                try{
                    new Thread(()=>
                        Functions.SignalRSender.AskToReloadCashTransactionTable ( machine_id  )
                        ).Start();
                    }
                catch(Exception e){
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " AskToReloadCashTransactionTable: "+e.Message);
                }
            }
        }


        static int insertToDeborahDB(string queryString)
        {
            try{
                string connectionString = Configuration["ConnectionStrings:DB_Casse"].ToString();//"server=10.10.10.99;uid=tel_daemon;pwd=Mjnh_ftl_#99;database=tel_admin";
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    MySqlCommand command = new MySqlCommand(queryString, connection);
                    command.Connection.Open();
                    return command.ExecuteNonQuery();
                }
            }catch(Exception e){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertToDeborahDB error " + e.Message);
                return 0;
            }
            
        }
        
        /// <summary>
        /// Crea il pacchetto da caricare sul db di deborah, partendo dall`unica cassa disponibile
        /// </summary>
        /// <typeparam name="theOnlyCashTransaction"></typeparam>
        static string buildPacket(CashTransaction theOnlyCashTransaction)
        {
            string queryPrimaParte = @"INSERT INTO `sap_cash_daemon` (`id`, `CodeMa`, `OdmTaskPalmare`, `DateB`, `TipoDa`, `CanaleGettone`, `CanaleProve`, `Ch1`, `Qty1`, `Ch2`, `Qty2`, `Ch3`, `Qty3`, `Ch4`, `Qty4`, `Ch5`, `Qty5`, `Ch6`, `Qty6`, `Ch7`, `Qty7`, `Ch8`, `Qty8`, `Ch9`, `Qty9`, `MdbVal2`, `MdbInc2`, `MdbTub2`, `MdbVal3`, `MdbInc3`, `MdbTub3`, `MdbVal4`, `MdbInc4`, `MdbTub4`, `MdbVal5`, `MdbInc5`, `MdbTub5`, `MdbVal6`, `MdbInc6`, `MdbTub6`, `Cashless`, `Total`, `Change`, `Sales`, `Consumabile`, `HopperGettone`, `Vend1Prc`, `QtyV1`, `Vend2Prc`, `QtyV2`, `Ticket`, `Price`, `Bns1`, `Bns2`, `BNS_1`, `BNS_2`, `Bns5`, `Bns10`, `Bns20`, `Token`, `ContMonViso`, `MechValue`, `CashlessNayax`, `CashlessApp`, `status`, `sap_exit_code`, `timestamp_try`, `timestamp_next_try`, `counter`, `timestamp`, `visible`, `message`, `force_stop`)";
            string[] splittedCashPacket = theOnlyCashTransaction.IdMachinesConnectionTraceNavigation.TransferredData.Split(',');
            string querySecondaParte = " VALUES (NULL, '"+ //id assegnato dal DB
                theOnlyCashTransaction.IdMachinesNavigation.Mid+"', '"+   //CodeMa 
                theOnlyCashTransaction.Odm+"', '"+   //OdmTaskPalmare
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")  //DateB
                +"', '2',"   //TipoDa
                +"'1',"      //CanaleGettone
                +"'8','"+    //CanaleProve
                splittedCashPacket[4]+"','"+  //valore ch01
                splittedCashPacket[5]+"','"+  //contatore ch01
                splittedCashPacket[6]+"','"+  //valore ch02
                splittedCashPacket[7]+"','"+  //contatore ch02
                splittedCashPacket[8]+"','"+  //valore ch03
                splittedCashPacket[9]+"','"+  //contatore ch03
                splittedCashPacket[10]+"','"+ //valore ch04
                splittedCashPacket[11]+"','"+ //contatore ch04
                splittedCashPacket[12]+"','"+ //valore ch05
                splittedCashPacket[13]+"','"+ //contatore ch05
                splittedCashPacket[14]+"','"+ //valore ch06
                splittedCashPacket[15]+"','"+ //contatore ch06
                splittedCashPacket[16]+"','"+ //valore ch07
                splittedCashPacket[17]+"','"+ //contatore ch07
                splittedCashPacket[18]+"','"+ //valore ch08
                splittedCashPacket[19]+"'," +  //contatore ch08
                " '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0','"+ //from Ch9 to Cashless
                splittedCashPacket[20]+"',"     //incasso
                +" '0','"+                     //change
                splittedCashPacket[21]+"',"     //sales 
                +" '0', '0', '0', '0', '0', '0','"+
                splittedCashPacket[22]+"','"+   //ticket
                splittedCashPacket[3]+"',"+     //price
                " '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '000', '0','"+
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + //timestamp_try
                "','"+
                DateTime.Now.AddMinutes(30).ToString("yyyy/MM/dd HH:mm:ss") + //timestamp_next_try
                "', '0', CURRENT_TIMESTAMP, '1', '0', '0');";
        
            return queryPrimaParte + querySecondaParte;
        }

        /// <summary>
        /// Crea il pacchetto da caricare sul db di deborah, confrontando il corrente e il precedente
        /// </summary>
        /// <typeparam name="currentTransaction"></typeparam>
        /// <typeparam name="previousTransaction"></typeparam>
        static string buildPacket(CashTransaction currentTransaction, CashTransaction previousTransaction)
        {
            string queryPrimaParte = @"INSERT INTO `sap_cash_daemon` (`id`, `CodeMa`, `OdmTaskPalmare`, `DateB`, `TipoDa`, `CanaleGettone`, `CanaleProve`, `Ch1`, `Qty1`, `Ch2`, `Qty2`, `Ch3`, `Qty3`, `Ch4`, `Qty4`, `Ch5`, `Qty5`, `Ch6`, `Qty6`, `Ch7`, `Qty7`, `Ch8`, `Qty8`, `Ch9`, `Qty9`, `MdbVal2`, `MdbInc2`, `MdbTub2`, `MdbVal3`, `MdbInc3`, `MdbTub3`, `MdbVal4`, `MdbInc4`, `MdbTub4`, `MdbVal5`, `MdbInc5`, `MdbTub5`, `MdbVal6`, `MdbInc6`, `MdbTub6`, `Cashless`, `Total`, `Change`, `Sales`, `Consumabile`, `HopperGettone`, `Vend1Prc`, `QtyV1`, `Vend2Prc`, `QtyV2`, `Ticket`, `Price`, `Bns1`, `Bns2`, `BNS_1`, `BNS_2`, `Bns5`, `Bns10`, `Bns20`, `Token`, `ContMonViso`, `MechValue`, `CashlessNayax`, `CashlessApp`, `status`, `sap_exit_code`, `timestamp_try`, `timestamp_next_try`, `counter`, `timestamp`, `visible`, `message`, `force_stop`)";
            string[] splittedCashPacket_current = currentTransaction.IdMachinesConnectionTraceNavigation.TransferredData.Split(',');
            string[] splittedCashPacket_previous = previousTransaction.IdMachinesConnectionTraceNavigation.TransferredData.Split(',');
            
            string querySecondaParte = " VALUES (NULL, '"+ //id assegnato dal DB
                currentTransaction.IdMachinesNavigation.Mid+"', '"+   //CodeMa 
                currentTransaction.Odm+"', '"+   //OdmTaskPalmare
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")  //DateB
                +"', '2',"   //TipoDa
                +"'1',"      //CanaleGettone
                +"'8','"+    //CanaleProve
                splittedCashPacket_current[4]+"','"+  //valore ch01
                (Convert.ToInt32(splittedCashPacket_current[5]) - Convert.ToInt32(splittedCashPacket_previous[5])).ToString() +"','"+  //contatore ch01
                splittedCashPacket_current[6]+"','"+  //valore ch02
                (Convert.ToInt32(splittedCashPacket_current[7]) - Convert.ToInt32(splittedCashPacket_previous[7])).ToString() +"','"+  //contatore ch02
                splittedCashPacket_current[8]+"','"+  //valore ch03
                (Convert.ToInt32(splittedCashPacket_current[9]) - Convert.ToInt32(splittedCashPacket_previous[9])).ToString() +"','"+  //contatore ch03
                splittedCashPacket_current[10]+"','"+ //valore ch04
                (Convert.ToInt32(splittedCashPacket_current[11]) - Convert.ToInt32(splittedCashPacket_previous[11])).ToString() +"','"+ //contatore ch04
                splittedCashPacket_current[12]+"','"+ //valore ch05
                (Convert.ToInt32(splittedCashPacket_current[13]) - Convert.ToInt32(splittedCashPacket_previous[13])).ToString() +"','"+ //contatore ch05
                splittedCashPacket_current[14]+"','"+ //valore ch06
                (Convert.ToInt32(splittedCashPacket_current[15]) - Convert.ToInt32(splittedCashPacket_previous[15])).ToString() +"','"+ //contatore ch06
                splittedCashPacket_current[16]+"','"+ //valore ch07
                (Convert.ToInt32(splittedCashPacket_current[17]) - Convert.ToInt32(splittedCashPacket_previous[17])).ToString() +"','"+ //contatore ch07
                splittedCashPacket_current[18]+"','"+ //valore ch08
                (Convert.ToInt32(splittedCashPacket_current[19]) - Convert.ToInt32(splittedCashPacket_previous[19])).ToString()  //contatore ch08
                +"', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0','"+ //from Ch9 to Cashless
                (Convert.ToInt32(splittedCashPacket_current[20]) - Convert.ToInt32(splittedCashPacket_previous[20])).ToString()+"',"     //incasso
                +" '0','"+                     //change
                (Convert.ToInt32(splittedCashPacket_current[21]) - Convert.ToInt32(splittedCashPacket_previous[21])).ToString()+"',"     //sales 
                +"'0', '0', '0', '0', '0', '0','"+
                (Convert.ToInt32(splittedCashPacket_current[22]) - Convert.ToInt32(splittedCashPacket_previous[22])).ToString()+"','"+   //ticket
                splittedCashPacket_current[3]+"',"+     //price
                " '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '000', '0', '"+
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + //timestamp_try
                "','"+ 
                DateTime.Now.AddMinutes(30).ToString("yyyy/MM/dd HH:mm:ss") + //timestamp_next_try
                "','0', CURRENT_TIMESTAMP, '1', '0', '0');";

            return queryPrimaParte + querySecondaParte;
        }

        static int buildPacket_better(CashTransaction theOnlyCashTransaction)
        {
            try{
                tel_adminContext tel_adminDB = new tel_adminContext();
                string[] splittedCashPacket = theOnlyCashTransaction.IdMachinesConnectionTraceNavigation.TransferredData.Split(',');
                SapCashDaemon SapCashDaemon_toLoad   = new SapCashDaemon{
                    CodeMa = splittedCashPacket[1],
                    OdmTaskPalmare = theOnlyCashTransaction.Odm,
                    TipoDa = 1,
                    CanaleGettone = 1 ,
                    CanaleProve = 8,
                    Ch1 = (float)Convert.ToInt32( splittedCashPacket[4]),
                    Qty1 = Convert.ToInt32(splittedCashPacket[5]),
                    Ch2 = (float)Convert.ToInt32( splittedCashPacket[6]),
                    Qty2 = Convert.ToInt32(splittedCashPacket[7]),
                    Ch3 = (float)Convert.ToInt32( splittedCashPacket[8]),
                    Qty3 = Convert.ToInt32(splittedCashPacket[9]),
                    Ch4 = (float)Convert.ToInt32( splittedCashPacket[10]),
                    Qty4 = Convert.ToInt32(splittedCashPacket[11]),
                    Ch5 = (float)Convert.ToInt32( splittedCashPacket[12]),
                    Qty5 = Convert.ToInt32(splittedCashPacket[13]),
                    Ch6 = (float)Convert.ToInt32( splittedCashPacket[14]),
                    Qty6 = Convert.ToInt32(splittedCashPacket[15]),
                    Ch7 = (float)Convert.ToInt32( splittedCashPacket[16]),
                    Qty7 = Convert.ToInt32(splittedCashPacket[17]),
                    Ch8 = (float)Convert.ToInt32( splittedCashPacket[18]),
                    Qty8 = Convert.ToInt32(splittedCashPacket[19]),
                    Ch9 = 0,
                    Qty9 = 0,
                    MdbVal2 = 0,
                    MdbInc2 = 0,
                    MdbTub2 = 0,
                    MdbVal3 = 0,
                    MdbInc3 = 0,
                    MdbTub3 = 0,
                    MdbVal4 = 0,
                    MdbInc4 = 0,
                    MdbTub4 = 0,
                    MdbVal5 = 0,
                    MdbInc5 = 0,
                    MdbTub5 = 0,
                    MdbVal6 = 0,
                    MdbInc6 = 0,
                    MdbTub6 = 0,
                    Cashless = (float)Convert.ToInt32( splittedCashPacket[24]),
                    Total = Convert.ToInt32( splittedCashPacket[20]),
                    Change  = 0,
                    Sales = Convert.ToInt32( splittedCashPacket[21]),
                    Consumabile = 0,
                    HopperGettone = 0,
                    Vend1Prc = 0,
                    QtyV1 = 0,
                    Vend2Prc = 0,
                    QtyV2 = 0,
                    Ticket = Convert.ToInt32( splittedCashPacket[22]),
                    Price = Convert.ToInt32( splittedCashPacket[3]),
                    Bns1 = 0,
                    Bns2 = 0,
                    Bns11 = 0,
                    Bns21 = 0,
                    Bns5 = 0,
                    Bns10 = 0,
                    Bns20 = 0,
                    Token = 0,
                    ContMonViso = 0, 
                    MechValue = 0,
                    CashlessNayax = 0,
                    CashlessApp = 0,
                    Status = "0",
                    SapExitCode = "0",
                    TimestampNextTry = DateTime.Now.AddMinutes(30).ToString("yyyy-MM-dd HH:mm:ss"),
                    Counter = 0,
                    Visible = true,
                    Message = "0",
                    ForceStop = 0,
                    DateB = DateTime.Now,
                    timestamp_try = DateTime.Now,
                    timestamp = DateTime.Now
                };
                tel_adminDB.SapCashDaemon.Add(SapCashDaemon_toLoad);
                tel_adminDB.SapCashProducts.Add(new SapCashProducts{
                    CashId = SapCashDaemon_toLoad.Id,
                    Product = "0",
                    Sales = 0,
                    Test = 0,
                    Prezzo = 0,
                    Refund = 0,
                    Status = 0,
                    timestamp = DateTime.Now
                });
                return 0;
            }
            catch(Exception e){
                Console.WriteLine("Exception loading SapCashDaemon or SapCashProducts: " + e.StackTrace);
                return 1;
            }
        }
        static int buildPacket_better(CashTransaction theOnlyCashTransaction, CashTransaction previousTransaction)
        {
            try{
                tel_adminContext tel_adminDB = new tel_adminContext();
                string[] splittedCashPacket = theOnlyCashTransaction.IdMachinesConnectionTraceNavigation.TransferredData.Split(',');
                string[] splittedCashPacket_previous = previousTransaction.IdMachinesConnectionTraceNavigation.TransferredData.Split(',');
                if(splittedCashPacket[24] == "")
                    splittedCashPacket[24] = "0";
                if(splittedCashPacket_previous[24] == "")
                    splittedCashPacket_previous[24] = "0";
                
                SapCashDaemon SapCashDaemon_toLoad   = new SapCashDaemon{
                    CodeMa = splittedCashPacket[1],
                    OdmTaskPalmare = theOnlyCashTransaction.Odm,
                    TipoDa = 1,
                    CanaleGettone = 1,
                    CanaleProve = 8,
                    Ch1 =  0,//(float)Convert.ToInt32( splittedCashPacket[4]),
                    Qty1 = 0,//Convert.ToInt32(splittedCashPacket[5]) - Convert.ToInt32(splittedCashPacket_previous[5]),
                    Ch2 =0,// (float)Convert.ToInt32( splittedCashPacket[6]),
                    Qty2 = 0,//Convert.ToInt32(splittedCashPacket[7])- Convert.ToInt32(splittedCashPacket_previous[7]),
                    Ch3 = 0,//(float)Convert.ToInt32( splittedCashPacket[8]),
                    Qty3 = 0,//Convert.ToInt32(splittedCashPacket[9])- Convert.ToInt32(splittedCashPacket_previous[9]),
                    Ch4 = 0,//(float)Convert.ToInt32( splittedCashPacket[10]),
                    Qty4 =0,// Convert.ToInt32(splittedCashPacket[11])- Convert.ToInt32(splittedCashPacket_previous[11]),
                    Ch5 = 0,//(float)Convert.ToInt32( splittedCashPacket[12]),
                    Qty5 =0,// Convert.ToInt32(splittedCashPacket[13])- Convert.ToInt32(splittedCashPacket_previous[13]),
                    Ch6 = 0,//(float)Convert.ToInt32( splittedCashPacket[14]),
                    Qty6 =0,// Convert.ToInt32(splittedCashPacket[15])- Convert.ToInt32(splittedCashPacket_previous[15]),
                    Ch7 =0,// (float)Convert.ToInt32( splittedCashPacket[16]),
                    Qty7 =0,// Convert.ToInt32(splittedCashPacket[17])- Convert.ToInt32(splittedCashPacket_previous[17]),
                    Ch8 = 0,//(float)Convert.ToInt32( splittedCashPacket[18]),
                    Qty8 =0,// Convert.ToInt32(splittedCashPacket[19])- Convert.ToInt32(splittedCashPacket_previous[19]),
                    Ch9 = 0,
                    Qty9 = 0,
                    MdbVal2 = 0,
                    MdbInc2 = 0,
                    MdbTub2 = 0,
                    MdbVal3 = 0,
                    MdbInc3 = 0,
                    MdbTub3 = 0,
                    MdbVal4 = 0,
                    MdbInc4 = 0,
                    MdbTub4 = 0,
                    MdbVal5 = 0,
                    MdbInc5 = 0,
                    MdbTub5 = 0,
                    MdbVal6 = 0,
                    MdbInc6 = 0,
                    MdbTub6 = 0,
                    Cashless = 0,//(float)Convert.ToInt32( splittedCashPacket[24])- (float)Convert.ToInt32(splittedCashPacket_previous[24]),
                    Total = 0,//(float)Convert.ToInt32( splittedCashPacket[20]) - (float)Convert.ToInt32(splittedCashPacket_previous[20]),
                    Change  = 0,
                    Sales =0,// Convert.ToInt32( splittedCashPacket[21]) - Convert.ToInt32(splittedCashPacket_previous[21]),
                    Consumabile = 0,
                    HopperGettone = 0,
                    Vend1Prc = 0,
                    QtyV1 = 0,
                    Vend2Prc = 0,
                    QtyV2 = 0,
                    Ticket = 0,//Convert.ToInt32( splittedCashPacket[22]) - Convert.ToInt32(splittedCashPacket_previous[22]),
                    Price =0,// (float)Convert.ToInt32( splittedCashPacket[3]),
                    Bns1 = 0,
                    Bns2 = 0,
                    Bns11 = 0,
                    Bns21 = 0,
                    Bns5 = 0,
                    Bns10 = 0,
                    Bns20 = 0,
                    Token = 0,
                    ContMonViso = 0, 
                    MechValue = 0,
                    CashlessNayax = 0,
                    CashlessApp = 0,
                    Status = "0",
                    SapExitCode = "0",
                    TimestampNextTry = DateTime.Now.AddMinutes(30).ToString("yyyy-MM-dd HH:mm:ss"),
                    Counter = 0,
                    Visible = true,
                    Message = "0",
                    ForceStop = 0,
                    DateB = DateTime.Now,
                    timestamp_try = DateTime.Now,
                    timestamp = DateTime.Now
                };
                tel_adminDB.SapCashDaemon.Add(SapCashDaemon_toLoad);
                tel_adminDB.SapCashProducts.Add(new SapCashProducts{
                    CashId = SapCashDaemon_toLoad.Id,
                    Product = "0",
                    Sales = 0,
                    Test = 0,
                    Prezzo = 0,
                    Refund = 0,
                    Status = 0,
                    timestamp = DateTime.Now
                });
                return 0;
            }
            catch(Exception e){
                Console.WriteLine("[2] Exception loading SapCashDaemon or SapCashProducts: " + e.StackTrace);
                return 1;
            }
        }
    }
}