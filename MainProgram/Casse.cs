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
        /// RegistrazioneCassa viene chiamato quando ricevo, indovina, un pacchetto di cassa.
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
        
        // /// <summary>
        // /// cash_calculator lo uso per vedere se il modem ha ricevuto la richiesta di cassa (CAS-OK)
        // /// e successivamente per leggere la cassa vera e propria (<TPK=$M1,)
        // /// </summary>
        // public static void cash_calculator(int machine_id, int cashTransactionID)
        // {
         
        //     listener_DBContext DB = new listener_DBContext (); 
        //     try{
        //         DB.CashTransaction.Single(m=>m.Id == cashTransactionID).Status = "Syncing..";
        //         DB.SaveChanges();
        //         var t = new Task( () => {
        //             loadCashPacketToDeborahDB(cashTransactionID); 
        //         });
        //         t.Start();
                
        //         return;    
        //     }
        //     catch(Exception e)
        //     {
        //         Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " cash_checker: "+e.Message);
        //     }
        //     finally
        //     {
        //         DB.SaveChanges();
        //         DB.DisposeAsync();
        //     }
        
        // }

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
                string queryPerCaricarePacchettoSap = "";
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
                        queryPerCaricarePacchettoSap = buildPacket(transaction, previousTransaction);
                    }
                    else
                    {
                        queryPerCaricarePacchettoSap = buildPacket(transaction);
                    }
                    int insertResult = insertToDeborahDB(queryPerCaricarePacchettoSap);
                    if(insertResult != 0)
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
                DateTime.Now.ToString(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))  //DateB
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
                " '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '000', '0', '0','"+
                DateTime.Now.AddMinutes(30).ToString(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")) + //timestamp_next_try
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
                DateTime.Now.ToString(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))  //DateB
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
                " '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '000', '0', '0','"+ 
                DateTime.Now.AddMinutes(30).ToString(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")) + //timestamp_next_try
                "','0', CURRENT_TIMESTAMP, '1', '0', '0');";

            return queryPrimaParte + querySecondaParte;
        }
    }
}