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
        public static void RegistrazioneCassa(int id_MachinesConnectionTrace,string CasType)
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
                    // new Thread(()=>
                    //     loadCashPacketToDeborahDB(LastTransactionForModem.Id,CasType)
                    //     ).Start();
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
        public static void loadCashPacketToDeborahDB(int cashTransactionID,string CasType)
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
                        loading_result = buildPacket_better(CasType,transaction, previousTransaction);
                    }
                    else
                    {
                        loading_result = buildPacket_better(CasType,transaction);
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

        static int buildPacket_better(string CasType,CashTransaction theOnlyCashTransaction)
        {
            tel_adminContext tel_adminDB = new tel_adminContext();
            try{
                string[] splittedCashPacket = theOnlyCashTransaction.IdMachinesConnectionTraceNavigation.TransferredData.Split(',');
                if (CasType== "M1")
                {
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
                        Total = (float)Convert.ToInt32( splittedCashPacket[20]),
                        Change  = 0,
                        Sales = Convert.ToInt32( splittedCashPacket[21]),
                        Consumabile = 0,
                        HopperGettone = 0,
                        Vend1Prc = 0,
                        QtyV1 = 0,
                        Vend2Prc = 0,
                        QtyV2 = 0,
                        Ticket = Convert.ToInt32( splittedCashPacket[22]),
                        Price = (float)Convert.ToInt32( splittedCashPacket[3]),
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
                    // salvo per poter prendere l'id di SapCashDaemon_toLoad
                    tel_adminDB.SaveChanges();
                    
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
                }
                else if (CasType=="M3")
                {
                    SapCashDaemon SapCashDaemon_toLoad   = new SapCashDaemon{					
                        CodeMa = splittedCashPacket[1],					
                        OdmTaskPalmare = theOnlyCashTransaction.Odm,					
                        TipoDa = 205,					
                        CanaleGettone = 1 ,					
                        CanaleProve = 9,					
                        Ch1 = (float)Convert.ToInt32( splittedCashPacket[11]),					
                        Qty1 = Convert.ToInt32(splittedCashPacket[12]),					
                        Ch2 = (float)Convert.ToInt32( splittedCashPacket[13]),					
                        Qty2 = Convert.ToInt32(splittedCashPacket[14]),					
                        Ch3 = (float)Convert.ToInt32( splittedCashPacket[15]),					
                        Qty3 = Convert.ToInt32(splittedCashPacket[16]),					
                        Ch4 = (float)Convert.ToInt32( splittedCashPacket[17]),					
                        Qty4 = Convert.ToInt32(splittedCashPacket[18]),					
                        Ch5 = (float)Convert.ToInt32( splittedCashPacket[19]),					
                        Qty5 = Convert.ToInt32(splittedCashPacket[20]),					
                        Ch6 = (float)Convert.ToInt32( splittedCashPacket[21]),					
                        Qty6 = Convert.ToInt32(splittedCashPacket[22]),					
                        Ch7 = (float)Convert.ToInt32( splittedCashPacket[23]),					
                        Qty7 = Convert.ToInt32(splittedCashPacket[24]),					
                        Ch8 = (float)Convert.ToInt32( splittedCashPacket[25]),					
                        Qty8 = Convert.ToInt32(splittedCashPacket[26]),					
                        Ch9 = 0,					
                        Qty9 = 0,					
                        MdbVal2 = 0,					
                        MdbInc2 = 0,					
                        MdbTub2 = 0,					
                        MdbVal3 = 0,					
                        MdbInc3 = 0,					
                        MdbTub3 = 0,					
                        MdbVal4 = 0,					
                        MdbInc4 = 0,					
                        MdbTub4 = 0,					
                        MdbVal5 = 0,					
                        MdbInc5 = 0,					
                        MdbTub5 = 0,					
                        MdbVal6 = 0,					
                        MdbInc6 = 0,					
                        MdbTub6 = 0,					
                        Cashless = 0,//da verificare					
                        Total = (float)Convert.ToInt32( splittedCashPacket[28]),					
                        Change  = 0,					
                        Sales = 0,					
                        Consumabile = 0,					
                        HopperGettone = 0,					
                        Vend1Prc =(float)Convert.ToInt32( splittedCashPacket[4]),					
                        QtyV1 = Convert.ToInt32( splittedCashPacket[5]),					
                        Vend2Prc = (float)Convert.ToInt32( splittedCashPacket[6]),					
                        QtyV2 = Convert.ToInt32( splittedCashPacket[7]),					
                        Ticket = 0,//Convert.ToInt32( splittedCashPacket[22]),					
                        Price = 0,					
                        Bns1 = 0,					
                        Bns2 = 0,					
                        Bns11 = 0,					
                        Bns21 = 0,					
                        Bns5 = 0,					
                        Bns10 = 0,					
                        Bns20 = 0,					
                        Token = 0,					
                        ContMonViso = 0, 					
                        MechValue = 0,					
                        CashlessNayax = 0,					
                        CashlessApp = 0,					
                        Status = "0",					
                        SapExitCode = "0",					
                        TimestampNextTry = DateTime.Now.AddMinutes(30).ToString("yyyy-MM-dd HH:mm:ss"),					
                        Counter = 0,					
                        Visible = true,					
                        Message = "0",					
                        ForceStop = 0,					
                        DateB = DateTime.Now,					
                        timestamp_try = DateTime.Now,					
                        timestamp = DateTime.Now					

                    };
               
                    tel_adminDB.SapCashDaemon.Add(SapCashDaemon_toLoad);
                    // salvo per poter prendere l'id di SapCashDaemon_toLoad
                    tel_adminDB.SaveChanges();
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

                }
                else if (CasType=="M5")
                {
                    SapCashDaemon SapCashDaemon_toLoad   = new SapCashDaemon{			
                        CodeMa = splittedCashPacket[1],			
                        OdmTaskPalmare = theOnlyCashTransaction.Odm,			
                        TipoDa = 8,			
                        CanaleGettone = 1 ,			
                        CanaleProve = 9,			
                        Ch1 = (float)Convert.ToInt32( splittedCashPacket[12]),			
                        Qty1 = Convert.ToInt32(splittedCashPacket[13]),			
                        Ch2 = (float)Convert.ToInt32( splittedCashPacket[14]),			
                        Qty2 = Convert.ToInt32(splittedCashPacket[15]),			
                        Ch3 = (float)Convert.ToInt32( splittedCashPacket[16]),			
                        Qty3 = Convert.ToInt32(splittedCashPacket[17]),			
                        Ch4 = (float)Convert.ToInt32( splittedCashPacket[18]),			
                        Qty4 = Convert.ToInt32(splittedCashPacket[19]),			
                        Ch5 = (float)Convert.ToInt32( splittedCashPacket[20]),			
                        Qty5 = Convert.ToInt32(splittedCashPacket[21]),			
                        Ch6 = (float)Convert.ToInt32( splittedCashPacket[22]),			
                        Qty6 = Convert.ToInt32(splittedCashPacket[23]),			
                        Ch7 = (float)Convert.ToInt32( splittedCashPacket[24]),			
                        Qty7 = Convert.ToInt32(splittedCashPacket[25]),			
                        Ch8 = (float)Convert.ToInt32( splittedCashPacket[26]),			
                        Qty8 = Convert.ToInt32(splittedCashPacket[27]),			
                        Ch9 = 0,			
                        Qty9 = 0,			
                        MdbVal2 = 0,			
                        MdbInc2 = 0,			
                        MdbTub2 = 0,			
                        MdbVal3 = 0,			
                        MdbInc3 = 0,			
                        MdbTub3 = 0,			
                        MdbVal4 = 0,			
                        MdbInc4 = 0,			
                        MdbTub4 = 0,			
                        MdbVal5 = 0,			
                        MdbInc5 = 0,			
                        MdbTub5 = 0,			
                        MdbVal6 = 0,			
                        MdbInc6 = 0,			
                        MdbTub6 = 0,			
                        Cashless = (float)Convert.ToInt32( splittedCashPacket[29]),			
                        Total = (float)Convert.ToInt32( splittedCashPacket[28]),			
                        Change  = 0,			
                        Sales = 0,			
                        Consumabile = 0,			
                        HopperGettone = 0,			
                        Vend1Prc =(float)Convert.ToInt32( splittedCashPacket[4]),			
                        QtyV1 = Convert.ToInt32( splittedCashPacket[5]),			
                        Vend2Prc = (float)Convert.ToInt32( splittedCashPacket[6]),			
                        QtyV2 = Convert.ToInt32( splittedCashPacket[7]),			
                        Ticket = 0,//Convert.ToInt32( splittedCashPacket[22]),			
                        Price = 0,			
                        Bns1 = 0,			
                        Bns2 = 0,			
                        Bns11 = 0,			
                        Bns21 = 0,			
                        Bns5 = 0,			
                        Bns10 = 0,			
                        Bns20 = 0,			
                        Token = 0,			
                        ContMonViso = 0, 			
                        MechValue = 0,			
                        CashlessNayax =(float)Convert.ToInt32( splittedCashPacket[29]),			
                        CashlessApp = 0,			
                        Status = "0",			
                        SapExitCode = "0",			
                        TimestampNextTry = DateTime.Now.AddMinutes(30).ToString("yyyy-MM-dd HH:mm:ss"),			
                        Counter = 0,			
                        Visible = true,			
                        Message = "0",			
                        ForceStop = 0,			
                        DateB = DateTime.Now,			
                        timestamp_try = DateTime.Now,			
                        timestamp = DateTime.Now			
                    };
               
                    tel_adminDB.SapCashDaemon.Add(SapCashDaemon_toLoad);
                    // salvo per poter prendere l'id di SapCashDaemon_toLoad
                    tel_adminDB.SaveChanges();
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
                }

            }
            catch(Exception e){
                Console.WriteLine("Exception loading SapCashDaemon or SapCashProducts: " + e.StackTrace);
                return 1;
            }finally{
                tel_adminDB.SaveChanges();
                tel_adminDB.DisposeAsync();
            }
            return 0;
        }
        static int buildPacket_better(string CasType,CashTransaction theOnlyCashTransaction, CashTransaction previousTransaction)
        {
            tel_adminContext tel_adminDB = new tel_adminContext();
            try{
                string[] splittedCashPacket = theOnlyCashTransaction.IdMachinesConnectionTraceNavigation.TransferredData.Split(',');
                string[] splittedCashPacket_previous = previousTransaction.IdMachinesConnectionTraceNavigation.TransferredData.Split(',');
                if (CasType=="M1")  
                {
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
                        Ch1 =  (float)Convert.ToInt32( splittedCashPacket[4]),
                        Qty1 = Convert.ToInt32(splittedCashPacket[5]) - Convert.ToInt32(splittedCashPacket_previous[5]),
                        Ch2 = (float)Convert.ToInt32( splittedCashPacket[6]),
                        Qty2 = Convert.ToInt32(splittedCashPacket[7])- Convert.ToInt32(splittedCashPacket_previous[7]),
                        Ch3 = (float)Convert.ToInt32( splittedCashPacket[8]),
                        Qty3 = Convert.ToInt32(splittedCashPacket[9])- Convert.ToInt32(splittedCashPacket_previous[9]),
                        Ch4 = (float)Convert.ToInt32( splittedCashPacket[10]),
                        Qty4 = Convert.ToInt32(splittedCashPacket[11])- Convert.ToInt32(splittedCashPacket_previous[11]),
                        Ch5 = (float)Convert.ToInt32( splittedCashPacket[12]),
                        Qty5 = Convert.ToInt32(splittedCashPacket[13])- Convert.ToInt32(splittedCashPacket_previous[13]),
                        Ch6 = (float)Convert.ToInt32( splittedCashPacket[14]),
                        Qty6 = Convert.ToInt32(splittedCashPacket[15])- Convert.ToInt32(splittedCashPacket_previous[15]),
                        Ch7 = (float)Convert.ToInt32( splittedCashPacket[16]),
                        Qty7 = Convert.ToInt32(splittedCashPacket[17])- Convert.ToInt32(splittedCashPacket_previous[17]),
                        Ch8 = (float)Convert.ToInt32( splittedCashPacket[18]),
                        Qty8 = Convert.ToInt32(splittedCashPacket[19])- Convert.ToInt32(splittedCashPacket_previous[19]),
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
                        Cashless = (float)Convert.ToInt32( splittedCashPacket[24])- (float)Convert.ToInt32(splittedCashPacket_previous[24]),
                        Total = (float)Convert.ToInt32( splittedCashPacket[20]) - (float)Convert.ToInt32(splittedCashPacket_previous[20]),
                        Change  = 0,
                        Sales = Convert.ToInt32( splittedCashPacket[21]) - Convert.ToInt32(splittedCashPacket_previous[21]),
                        Consumabile = 0,
                        HopperGettone = 0,
                        Vend1Prc = 0,
                        QtyV1 = 0,
                        Vend2Prc = 0,
                        QtyV2 = 0,
                        Ticket = Convert.ToInt32( splittedCashPacket[22]) - Convert.ToInt32(splittedCashPacket_previous[22]),
                        Price = (float)Convert.ToInt32( splittedCashPacket[3]),
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
                    // salvo per poter prendere l'id di SapCashDaemon_toLoad
                    tel_adminDB.SaveChanges();
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
                }
                else if (CasType=="M3")
                {
                    SapCashDaemon SapCashDaemon_toLoad   = new SapCashDaemon{																	
                        CodeMa = splittedCashPacket[1],																	
                        OdmTaskPalmare = theOnlyCashTransaction.Odm,																	
                        TipoDa = Convert.ToInt32( splittedCashPacket[3]),																	
                        CanaleGettone = 1,																	
                        CanaleProve = 8,																	
                        Ch1 =  (float)Convert.ToInt32( splittedCashPacket[11]),																	
                        Qty1 = Convert.ToInt32(splittedCashPacket[12]) - Convert.ToInt32(splittedCashPacket_previous[12]),																	
                        Ch2 = (float)Convert.ToInt32( splittedCashPacket[13]),																	
                        Qty2 = Convert.ToInt32(splittedCashPacket[14])- Convert.ToInt32(splittedCashPacket_previous[14]),																	
                        Ch3 = (float)Convert.ToInt32( splittedCashPacket[15]),																	
                        Qty3 = Convert.ToInt32(splittedCashPacket[16])- Convert.ToInt32(splittedCashPacket_previous[16]),																	
                        Ch4 = (float)Convert.ToInt32( splittedCashPacket[17]),																	
                        Qty4 = Convert.ToInt32(splittedCashPacket[18])- Convert.ToInt32(splittedCashPacket_previous[18]),																	
                        Ch5 = (float)Convert.ToInt32( splittedCashPacket[19]),																	
                        Qty5 = Convert.ToInt32(splittedCashPacket[20])- Convert.ToInt32(splittedCashPacket_previous[20]),																	
                        Ch6 = (float)Convert.ToInt32( splittedCashPacket[21]),																	
                        Qty6 = Convert.ToInt32(splittedCashPacket[22])- Convert.ToInt32(splittedCashPacket_previous[22]),																	
                        Ch7 = (float)Convert.ToInt32( splittedCashPacket[23]),																	
                        Qty7 = Convert.ToInt32(splittedCashPacket[24])- Convert.ToInt32(splittedCashPacket_previous[24]),																	
                        Ch8 = (float)Convert.ToInt32( splittedCashPacket[25]),																	
                        Qty8 = Convert.ToInt32(splittedCashPacket[26])- Convert.ToInt32(splittedCashPacket_previous[26]),																	
                        Ch9 = 0,																	
                        Qty9 = 0,																	
                        MdbVal2 = 0,																	
                        MdbInc2 = 0,																	
                        MdbTub2 = 0,																	
                        MdbVal3 = 0,																	
                        MdbInc3 = 0,																	
                        MdbTub3 = 0,																	
                        MdbVal4 = 0,																	
                        MdbInc4 = 0,																	
                        MdbTub4 = 0,																	
                        MdbVal5 = 0,																	
                        MdbInc5 = 0,																	
                        MdbTub5 = 0,																	
                        MdbVal6 = 0,																	
                        MdbInc6 = 0,																	
                        MdbTub6 = 0,																	
                        Cashless = 0,																	
                        Total = (float)Convert.ToInt32( splittedCashPacket[28]) - (float)Convert.ToInt32(splittedCashPacket_previous[28]),																	
                        Change  = 0,																	
                        Sales = 0,																	
                        Consumabile = 0,																	
                        HopperGettone = 0,																	
                        Vend1Prc =(float)Convert.ToInt32( splittedCashPacket[4]) - (float)Convert.ToInt32(splittedCashPacket_previous[4]),					
                        QtyV1 = Convert.ToInt32( splittedCashPacket[5])-Convert.ToInt32(splittedCashPacket_previous[5]),					
                        Vend2Prc = (float)Convert.ToInt32( splittedCashPacket[6])-(float)Convert.ToInt32(splittedCashPacket_previous[6]),					
                        QtyV2 = Convert.ToInt32( splittedCashPacket[7])-Convert.ToInt32(splittedCashPacket_previous[7]),																			
                        Ticket = 0,																	
                        Price = 0,																	
                        Bns1 = 0,																	
                        Bns2 = 0,																	
                        Bns11 = 0,																	
                        Bns21 = 0,																	
                        Bns5 = 0,																	
                        Bns10 = 0,																	
                        Bns20 = 0,																	
                        Token = 0,																	
                        ContMonViso = 0, 																	
                        MechValue = 0,																	
                        CashlessNayax = 0,																	
                        CashlessApp = 0,																	
                        Status = "0",																	
                        SapExitCode = "0",																	
                        TimestampNextTry = DateTime.Now.AddMinutes(30).ToString("yyyy-MM-dd HH:mm:ss"),																	
                        Counter = 0,																	
                        Visible = true,																	
                        Message = "0",																	
                        ForceStop = 0,																	
                        DateB = DateTime.Now,																	
                        timestamp_try = DateTime.Now,																	
                        timestamp = DateTime.Now																	
                    };
               
                    tel_adminDB.SapCashDaemon.Add(SapCashDaemon_toLoad);
                    // salvo per poter prendere l'id di SapCashDaemon_toLoad
                    tel_adminDB.SaveChanges();
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

                }
                else if (CasType=="M5")
                {
                    SapCashDaemon SapCashDaemon_toLoad   = new SapCashDaemon{																	
                        CodeMa = splittedCashPacket[1],																	
                        OdmTaskPalmare = theOnlyCashTransaction.Odm,																	
                        TipoDa = Convert.ToInt32( splittedCashPacket[3]),																	
                        CanaleGettone = 1,																	
                        CanaleProve = 8,																	
                        Ch1 =  (float)Convert.ToInt32( splittedCashPacket[11]),																	
                        Qty1 = Convert.ToInt32(splittedCashPacket[12]) - Convert.ToInt32(splittedCashPacket_previous[12]),																	
                        Ch2 = (float)Convert.ToInt32( splittedCashPacket[13]),																	
                        Qty2 = Convert.ToInt32(splittedCashPacket[14])- Convert.ToInt32(splittedCashPacket_previous[14]),																	
                        Ch3 = (float)Convert.ToInt32( splittedCashPacket[15]),																	
                        Qty3 = Convert.ToInt32(splittedCashPacket[16])- Convert.ToInt32(splittedCashPacket_previous[16]),																	
                        Ch4 = (float)Convert.ToInt32( splittedCashPacket[17]),																	
                        Qty4 = Convert.ToInt32(splittedCashPacket[18])- Convert.ToInt32(splittedCashPacket_previous[18]),																	
                        Ch5 = (float)Convert.ToInt32( splittedCashPacket[19]),																	
                        Qty5 = Convert.ToInt32(splittedCashPacket[20])- Convert.ToInt32(splittedCashPacket_previous[20]),																	
                        Ch6 = (float)Convert.ToInt32( splittedCashPacket[21]),																	
                        Qty6 = Convert.ToInt32(splittedCashPacket[22])- Convert.ToInt32(splittedCashPacket_previous[22]),																	
                        Ch7 = (float)Convert.ToInt32( splittedCashPacket[23]),																	
                        Qty7 = Convert.ToInt32(splittedCashPacket[24])- Convert.ToInt32(splittedCashPacket_previous[24]),																	
                        Ch8 = (float)Convert.ToInt32( splittedCashPacket[25]),																	
                        Qty8 = Convert.ToInt32(splittedCashPacket[26])- Convert.ToInt32(splittedCashPacket_previous[26]),																	
                        Ch9 = 0,																	
                        Qty9 = 0,																	
                        MdbVal2 = 0,																	
                        MdbInc2 = 0,																	
                        MdbTub2 = 0,																	
                        MdbVal3 = 0,																	
                        MdbInc3 = 0,																	
                        MdbTub3 = 0,																	
                        MdbVal4 = 0,																	
                        MdbInc4 = 0,																	
                        MdbTub4 = 0,																	
                        MdbVal5 = 0,																	
                        MdbInc5 = 0,																	
                        MdbTub5 = 0,																	
                        MdbVal6 = 0,																	
                        MdbInc6 = 0,																	
                        MdbTub6 = 0,																	
                        Cashless = (float)Convert.ToInt32( splittedCashPacket[29])- (float)Convert.ToInt32(splittedCashPacket_previous[29]),																	
                        Total = (float)Convert.ToInt32( splittedCashPacket[28]) - (float)Convert.ToInt32(splittedCashPacket_previous[28]),																	
                        Change  = 0,																	
                        Sales = 0,																	
                        Consumabile = 0,																	
                        HopperGettone = 0,																	
                        Vend1Prc = 0,																	
                        QtyV1 = 0,																	
                        Vend2Prc = 0,																	
                        QtyV2 = 0,																	
                        Ticket = 0,																	
                        Price = 0,																	
                        Bns1 = 0,																	
                        Bns2 = 0,																	
                        Bns11 = 0,																	
                        Bns21 = 0,																	
                        Bns5 = 0,																	
                        Bns10 = 0,																	
                        Bns20 = 0,																	
                        Token = 0,																	
                        ContMonViso = 0, 																	
                        MechValue = 0,																	
                        CashlessNayax = 0,																	
                        CashlessApp = 0,																	
                        Status = "0",																	
                        SapExitCode = "0",																	
                        TimestampNextTry = DateTime.Now.AddMinutes(30).ToString("yyyy-MM-dd HH:mm:ss"),																	
                        Counter = 0,																	
                        Visible = true,																	
                        Message = "0",																	
                        ForceStop = 0,																	
                        DateB = DateTime.Now,																	
                        timestamp_try = DateTime.Now,																	
                        timestamp = DateTime.Now																	
                    };
               
                    tel_adminDB.SapCashDaemon.Add(SapCashDaemon_toLoad);
                    // salvo per poter prendere l'id di SapCashDaemon_toLoad
                    tel_adminDB.SaveChanges();
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

                }

                
            }
            catch(Exception e){
                Console.WriteLine("[2] Exception loading SapCashDaemon or SapCashProducts: " + e.StackTrace);
                return 1;
            }finally{
                tel_adminDB.SaveChanges();
                tel_adminDB.DisposeAsync();
            }
            return 0;
        }
    }
}