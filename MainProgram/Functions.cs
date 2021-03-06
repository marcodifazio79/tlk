using System;
using System.Net;  
using System.Net.Sockets;
using System.Collections.Generic;
using System.Xml;
using System.Threading;
using Functions.database;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Custom;
using MySql.Data.MySqlClient;
using System.Data;


namespace Functions
{    
    public class DatabaseFunctions
    {   
    
        public static IConfiguration Configuration;
        public DatabaseFunctions()
        {
            
        }
        public static string GetIPMode()
        {
            return ConfigurationManager.AppSetting["IPSet:IPFree"];
        }
        public static string GetServerType()
        {
            return ConfigurationManager.AppSetting["ServerType:TypeMachine"];
        }
        public static string GetConnectString()
        {
            string infoserver= GetServerType();
            string strinConn="";
            switch(infoserver)
            {
                case "ITA_PROD":
                    strinConn= ConfigurationManager.AppSetting["ConnectionStrings:DefaultConnectionITA_PROD"];
                break;
                case "ITA_SVI":
                    strinConn= ConfigurationManager.AppSetting["ConnectionStrings:DefaultConnectionITA_SVI"];
                break;
                case "ESP":
                    strinConn= ConfigurationManager.AppSetting["ConnectionStrings:DefaultConnectionESP"];
                break;

            }
            return strinConn;

        }
//
        /// <summary>
        ///  
        /// </summary>
        public static void updateModemTableEntry(string ip_addr,  string s)
        {
            listener_DBContext DB = new listener_DBContext (); 
            try
            {
                if (ip_addr=="127.0.0.1")return;
                string mid = s.Substring(s.IndexOf("=")+1);
                mid =mid.Substring(0,mid.IndexOf("-"));
                string imei = s.Substring(s.IndexOf("-")+1);
                imei =imei.Substring(0,imei.IndexOf(">"));

                if (mid=="77770001")mid="77770001_"+imei.ToString();
                if (mid.StartsWith("5555555"))mid=mid+"_"+imei.ToString();
                if (mid==("TCC"))mid=mid+"_"+imei.ToString();
                               
                bool isIstagram=false;

                

                string version = s.Substring(s.IndexOf("VER=")+4);
                version = version.Substring(0,version.IndexOf(">"));
                if (s.Contains(".INS"))
                    isIstagram=true;
                
                // controllo se esiste un modem con il mid scritto nel pacchetto, e se
                // il mid ?? collegato allo stesso Ip: in caso contrario potrebbe essere un modem 
                // "sostituto" (partiamo del presupposto che i modem hanno ip statico..)
#if DEBUG 

ip_addr="176.242.21.246";
//int p=Convert.ToInt16(ip_addr);

#endif
                if( DB.Machines.Any( y=> y.IpAddress == ip_addr ) ) //se l'ip ?? gia presente nel db...
                {
                    Machines newModemPacket = DB.Machines.First( y=> y.IpAddress == ip_addr);// seleziono i dati del  modem in base all'ip
                    
                    if(newModemPacket.Mid==mid && newModemPacket.Imei==Convert.ToInt64(imei)) // se modem e CE sono gia associati all'ip 
                    {
                        //DEVO AGGIORNARE SOLO LA VERSIONE perche potrebbe essere diversa
                        newModemPacket.Version = version; 
                        newModemPacket.IsOnline = true;
                        newModemPacket.MarkedBroken=false;
                        newModemPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                    }else if (newModemPacket.MarkedBroken) //se il mid impostato per sostituire 77770001_xx ?? gi?? presente sul DB e lo status ?? broken 
                    {       
                                                  //aggiorno i parametri del vecchio modem e cancello il record del nuovo 
                        newModemPacket.Mid=mid; 
                        newModemPacket.Version = version; 
                        newModemPacket.IpAddress=ip_addr;
                        newModemPacket.Imei=Convert.ToInt64(imei);
                        newModemPacket.IsOnline = true;
                        newModemPacket.MarkedBroken=false;
                        newModemPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                        //ClearMachineTable.DatabaseClearTable.DeleteMachine(newModemPacket.Id.ToString(),MachineToUpdate.Id.ToString()); 
                    } 

                    else if(newModemPacket.Mid.StartsWith("77770001_") && newModemPacket.Imei==Convert.ToInt64(imei)) // se CE diverso e imei gia associato all'ip
                    {
                        if(DB.Machines.Any( y=> y.Mid == mid )) // verifico se il mid impostato per sostituire 77770001_xx ?? gi?? presente nella tabella Machines
                        {
                            Machines MachineToUpdate = DB.Machines.First( y=> y.Mid == mid); 
                            if (MachineToUpdate.MarkedBroken) //se il mid impostato per sostituire 77770001_xx ?? gi?? presente sul DB e lo status ?? broken 
                            {                                 //aggiorno i parametri del vecchio modem e cancello il record del nuovo 
                                MachineToUpdate.Mid=mid; 
                                MachineToUpdate.Version = version; 
                                MachineToUpdate.IpAddress=ip_addr;
                                MachineToUpdate.Imei=Convert.ToInt64(imei);
                                MachineToUpdate.IsOnline = true;
                                MachineToUpdate.MarkedBroken=false;
                                MachineToUpdate.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                ClearMachineTable.DatabaseClearTable.DeleteMachine(newModemPacket.Id.ToString(),MachineToUpdate.Id.ToString()); 
                            }
                            else//se il mid impostato per sostituire 77770001_xx ?? gi?? presente sul DB e lo status NON ?? broken 
                            {                                 //imposto il nuovo modem come duplicato 
                                newModemPacket.Version = version;
                                newModemPacket.IpAddress=ip_addr;
                                newModemPacket.IsOnline = true;
                                newModemPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                newModemPacket.MarkedBroken=false;
                                newModemPacket.Mid = "Duplicato! "+ imei;
                            }
                        }
                        else//se il mid impostato per sostituire 77770001_xx NON ?? gi?? presente sul DB ?? un CE nuovo e quindi
                            {                                 //aggiorno i parametri del modem MID & VERSION
                                newModemPacket.Mid=mid; 
                                newModemPacket.Version = version; 
                                newModemPacket.IsOnline = true;
                                newModemPacket.MarkedBroken=false;
                                newModemPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                        }
                    }
                    else if(newModemPacket.Mid.StartsWith("5555555") && newModemPacket.Imei==Convert.ToInt64(imei)) // se CE diverso e imei gia associato all'ip
                    {
                        if(DB.Machines.Any( y=> y.Mid == mid )) // verifico se il mid impostato per sostituire 5555555 ?? gi?? presente nella tabella Machines
                        {
                            Machines MachineToUpdate = DB.Machines.First( y=> y.Mid == mid); 
                            if (MachineToUpdate.MarkedBroken) //se il mid impostato per sostituire 55555555 ?? gi?? presente sul DB e lo status ?? broken 
                            {                                 //aggiorno i parametri del vecchio modem e cancello il record del nuovo 
                                MachineToUpdate.Mid=mid; 
                                MachineToUpdate.Version = version; 
                                MachineToUpdate.IpAddress=ip_addr;
                                MachineToUpdate.Imei=Convert.ToInt64(imei);
                                MachineToUpdate.IsOnline = true;
                                MachineToUpdate.MarkedBroken=false;
                                MachineToUpdate.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                ClearMachineTable.DatabaseClearTable.DeleteMachine(newModemPacket.Id.ToString(),MachineToUpdate.Id.ToString()); 
                            }
                            else//se il mid impostato per sostituire 5555555 ?? gi?? presente sul DB e lo status NON ?? broken 
                            {                                 //imposto il nuovo modem come duplicato 
                                newModemPacket.Version = version;
                                newModemPacket.IpAddress=ip_addr;
                                newModemPacket.IsOnline = true;
                                newModemPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                newModemPacket.MarkedBroken=false;
                                newModemPacket.Mid = "Duplicato! "+ imei;
                            }
                        }
                        else//se il mid impostato per sostituire 5555555 NON ?? gi?? presente sul DB ?? un CE nuovo e quindi
                            {                                 //aggiorno i parametri del modem MID & VERSION
                                newModemPacket.Mid=mid; 
                                newModemPacket.Version = version; 
                                newModemPacket.IsOnline = true;
                                newModemPacket.MarkedBroken=false;
                                newModemPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                        }
                    }
                    else if(newModemPacket.Mid.StartsWith("TCC") && newModemPacket.Imei==Convert.ToInt64(imei)) // se CE diverso e imei gia associato all'ip
                    {
                        if(DB.Machines.Any( y=> y.Mid == mid )) // verifico se il mid impostato per sostituire TCC_xx ?? gi?? presente nella tabella Machines
                        {
                            Machines MachineToUpdate = DB.Machines.First( y=> y.Mid == mid); 
                            if (MachineToUpdate.MarkedBroken) //se il mid impostato per sostituire TCC_xx ?? gi?? presente sul DB e lo status ?? broken 
                            {                                 //aggiorno i parametri del vecchio modem e cancello il record del nuovo 
                                MachineToUpdate.Mid=mid; 
                                MachineToUpdate.Version = version; 
                                MachineToUpdate.IpAddress=ip_addr;
                                MachineToUpdate.Imei=Convert.ToInt64(imei);
                                MachineToUpdate.IsOnline = true;
                                MachineToUpdate.MarkedBroken=false;
                                MachineToUpdate.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                ClearMachineTable.DatabaseClearTable.DeleteMachine(newModemPacket.Id.ToString(),MachineToUpdate.Id.ToString()); 
                            }
                            else//se il mid impostato per sostituire TCC_xx ?? gi?? presente sul DB e lo status NON ?? broken 
                            {                                 //imposto il nuovo modem come duplicato 
                                newModemPacket.Version = version;
                                newModemPacket.IpAddress=ip_addr;
                                newModemPacket.IsOnline = true;
                                newModemPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                newModemPacket.MarkedBroken=false;
                                newModemPacket.Mid = "Duplicato! "+ imei;
                            }
                        }
                        else//se il mid impostato per sostituire TCC_xx NON ?? gi?? presente sul DB ?? un CE nuovo e quindi
                            {                                 //aggiorno i parametri del modem MID & VERSION
                                newModemPacket.Mid=mid; 
                                newModemPacket.Version = version; 
                                newModemPacket.IsOnline = true;
                                newModemPacket.MarkedBroken=false;
                                newModemPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                        }
                    }
                    
                    else if (newModemPacket.Mid!=mid)// se il modem e CE non  ?? gi?? associato all'ip
                    {
                        if (newModemPacket.Mid.StartsWith("Recupero") |newModemPacket.Mid.StartsWith("Duplicato") ) //se il mid sul DB associato  all'ip inizia con Recupero  
                        {
                            //verifico che il MID che si ?? presentato ?? gia presente nel db.. 
                            if( DB.Machines.Any( y=> y.Mid == mid ) )                            {
                                Machines MachineToUpdate = DB.Machines.First( y=> y.Mid == mid );// carico i dati del modem da sostituire
                                if (MachineToUpdate.MarkedBroken)//verifico che il modem ?? segnalato come da sostituire...
                                {
                                    MachineToUpdate.Version = version; 
                                    MachineToUpdate.IpAddress=ip_addr;
                                    //MachineToUpdate.Mid=mid; 
                                    MachineToUpdate.Imei=Convert.ToInt64(imei);
                                    MachineToUpdate.IsOnline = true;
                                    MachineToUpdate.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                    MachineToUpdate.MarkedBroken=false;
                                    // rimuovo il modem che si era presentato come nuovo, ma che in realt?? era un 
                                    // "sostituto" (perch?? ha lo stesso mid di un modem "MarkedBroken")
                                    ClearMachineTable.DatabaseClearTable.DeleteMachine(newModemPacket.Id.ToString(),MachineToUpdate.Id.ToString()); 
                                }
                                else if(MachineToUpdate.Imei==Convert.ToInt64(imei))//significa che il modem che si presenta con lo stesso
                                {
                                    MachineToUpdate.Version = version; 
                                    MachineToUpdate.IpAddress=ip_addr;
                                    MachineToUpdate.IsOnline = true;
                                    MachineToUpdate.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                    MachineToUpdate.MarkedBroken=false;
                                    // rimuovo il modem che si era presentato come nuovo, ma che in realt?? era un 
                                    // "sostituto" (perch?? ha lo stesso mid di un modem "MarkedBroken")
                                    ClearMachineTable.DatabaseClearTable.DeleteMachine(newModemPacket.Id.ToString(),MachineToUpdate.Id.ToString()); 
                                         
                                }
                                else if (isIstagram)
                                {
                                    //Machines MachineToUpdate = DB.Machines.First( y=> y.Mid == mid );// carico i dati del modem da sostituire
                                    MachineToUpdate.Version = version; 
                                    MachineToUpdate.IpAddress=ip_addr;
                                    //MachineToUpdate.Mid=mid; 
                                    if( MachineToUpdate.Imei.ToString().StartsWith("20")) MachineToUpdate.Imei=Convert.ToInt64(imei);
                                    MachineToUpdate.IsOnline = true;
                                    MachineToUpdate.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                    MachineToUpdate.MarkedBroken=false;
                                    // rimuovo il modem che si era presentato come nuovo, ma che in realt?? era un 
                                    
                                    ClearMachineTable.DatabaseClearTable.DeleteMachine(newModemPacket.Id.ToString(),MachineToUpdate.Id.ToString()); 
                                }
                                else
                                {
                                    newModemPacket.Version = version;
                                    newModemPacket.IpAddress=ip_addr;
                                    newModemPacket.IsOnline = true;
                                    newModemPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                    newModemPacket.MarkedBroken=false;
                                    newModemPacket.Mid = "Duplicato! "+ imei;
                                }
                            }
                            else
                            {
                                //il MID che si ?? presentato non esiste sul db.
                                // a questo punto ?? un modem nuovo e devo aggiornare i dati mdi,imei,version
                                
                                newModemPacket.Version = version; 
                                newModemPacket.Mid=mid;
                                newModemPacket.Imei=Convert.ToInt64(imei);
                                newModemPacket.IsOnline = true;
                                newModemPacket.MarkedBroken=false;
                                newModemPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));

                            }
                        }
                        else
                        {
                            Machines ModemAlreadyPresentPacket = DB.Machines.First( y=> y.Mid == mid);// seleziono i dati del modem gia installato   in base al mid
                          
                            if (ModemAlreadyPresentPacket.Version.Contains(".INS"))// se il mid del modem gia installato ?? di una Istagramm.....
                            {
                                ModemAlreadyPresentPacket.Version = version;
                                ModemAlreadyPresentPacket.IpAddress=ip_addr;
                                    
                                ModemAlreadyPresentPacket.IsOnline = true;
                                ModemAlreadyPresentPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                ModemAlreadyPresentPacket.MarkedBroken=false;
                                // rimuovo il modem che si era presentato come nuovo, ma che in realt?? ha solo cambiato IP 
                                ClearMachineTable.DatabaseClearTable.DeleteMachine(newModemPacket.Id.ToString(),ModemAlreadyPresentPacket.Id.ToString()); 
                            }
                            else
                            {
                                if (ModemAlreadyPresentPacket.Imei==Convert.ToInt64(imei)) //in questo caso anche l'imei che si presenta ?? uguale a quella registata sul db 
                                {
                                    ModemAlreadyPresentPacket.Version = version;
                                    ModemAlreadyPresentPacket.IpAddress=ip_addr;
                                
                                    ModemAlreadyPresentPacket.IsOnline = true;
                                    ModemAlreadyPresentPacket.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                                    ModemAlreadyPresentPacket.MarkedBroken=false;
                                    // rimuovo il modem che si era presentato come nuovo, ma che in realt?? ha solo cambiato IP 
                                    ClearMachineTable.DatabaseClearTable.DeleteMachine(newModemPacket.Id.ToString(),ModemAlreadyPresentPacket.Id.ToString()); 
                                }
                                else
                                {

                                    if (imei=="" | imei.StartsWith("20")) 
                                    {
                                        newModemPacket.Mid = "Duplicato! "+ DateTime.Now.ToString("yyMMddHHmmssfff");
                                    }
                                    else
                                    {
                                        newModemPacket.Mid = "Duplicato! "+ imei;
                                    }
                                                                    newModemPacket.Version = version; 
                                    newModemPacket.Mid=mid;
                                    newModemPacket.Imei=Convert.ToInt64(imei);
                                    newModemPacket.IsOnline = true;
                                    newModemPacket.MarkedBroken=false;
                                   
                             
                                }

                            }
                        }
                    }
                }

                DB.SaveChanges();
               
                //reload web page
                try
                {
                    new Thread(()=>
                        Functions.SignalRSender.AskToReloadMachinesTable() 
                    ).Start();                        
                }
                catch(Exception exc){
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " AskToReloadMachinesTable: "+exc.Message);
                }

            }catch(Exception e){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : updateModemTableEntry: " + e.Message);
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : updateModemTableEntry: " + e.StackTrace);
            }finally{
                DB.DisposeAsync();
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
       
        public static void insertIntoMachinesTable(string ip_addr)
        {
            
            listener_DBContext DB = new listener_DBContext ();
            try
            {   
                 if (ip_addr=="127.0.0.1")return;
#if DEBUG 
ip_addr="176.242.21.246";
#endif

                if(DB.Machines.Any( y=> y.IpAddress == ip_addr )   )
                {
                    Machines MachineToUpdate = DB.Machines.First( y=> y.IpAddress == ip_addr ) ;
                    MachineToUpdate.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                    MachineToUpdate.IsOnline=true;
                }
                else
                {
                    DB.Machines.Add( new Machines{
                        IpAddress = ip_addr,
                        Mid  = "RecuperoInCorso.." + DateTime.Now.ToString("yyMMddHHmmssff"),
                        Imei = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssf")),
                        Version = "",
                        last_communication =null,
                        time_creation =null,
                        sim_serial="0"
                        });
                }
                DB.SaveChanges();
            }
            catch(Exception ex)
            {
                //Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : insertIntoMachinesTable: " + ex.Message);
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : insertIntoMachinesTable InnerExc: " + ex.InnerException);
            }
            finally{
                DB.DisposeAsync();
            }
        }

        /// <summary>
        ///  
        /// </summary>
        public static bool IsNumeric(string strText)
        {
            bool bres = false;
            try
            {
                //Console.WriteLine(strText);   
                Int64 result = Convert.ToInt64(strText);
                bres = true;
                return bres;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return bres;
            }
        }
   
       public static void insertIntoMachinesConnectionTrace(string ip_addr, string send_or_recv, string transferred_data)
        {
            listener_DBContext DB = new listener_DBContext ();
            
            AsynchronousSocketListener.aTimer.Stop();
            MachinesConnectionTrace MachineTraceToAdd = null;
            try
            {
#if DEBUG 
ip_addr="176.242.21.246";
#endif
                if (AsynchronousSocketListener.GetTimeCheckStatus().ToUpper()=="TRUE")AsynchronousSocketListener.aTimer.Stop();
                
                if(DB.Machines.Any( y=> y.IpAddress == ip_addr ))
                {
                    string MIDValue="";
                    string imeiValue="";
                  
                    Machines m = DB.Machines.First( y => y.IpAddress == ip_addr );
              
                    MachineTraceToAdd = new MachinesConnectionTrace 
                    {
                        IpAddress = m.IpAddress,
                        SendOrRecv = send_or_recv,
                        TransferredData = transferred_data,
                        IdMacchina = m.Id
                    };

                    if(transferred_data.StartsWith("<TPK="))
                    {
                    //         Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertIntoMachinesConnectionTrace: Row 271 ");                        
                        MachineTraceToAdd.telemetria_status = 2;

                        string[] splitTrData= transferred_data.Split(",");

                        if (transferred_data.StartsWith("<TPK=W5")) 
                        {
                            MIDValue=splitTrData[4];
                        }
                        else
                        {
                            MIDValue=splitTrData[1];
                        }

                        if (m.Mid==MIDValue)
                        {    
                            string SerialSIM="";
                            
                            if (transferred_data.StartsWith("<TPK=$M1")|transferred_data.StartsWith("<TPK=$P1")) 
                            {
                                SerialSIM=splitTrData[30];
                                imeiValue=splitTrData[31];
                            }

                            if (transferred_data.StartsWith("<TPK=$M3"))
                            {
                                SerialSIM=splitTrData[37];
                                imeiValue=splitTrData[38];
                            }
                            
                            if (transferred_data.StartsWith("<TPK=$M5"))
                            {
                                SerialSIM=splitTrData[45];
                                imeiValue=splitTrData[46];
                            }
                            
                            if (transferred_data.StartsWith("<TPK=W5")) 
                            { 
                                SerialSIM=splitTrData[31];
                                imeiValue=splitTrData[32];
                            }

                            if (transferred_data.StartsWith("<TPK=$I2") | transferred_data.StartsWith("<TPK=$I1"))
                            {
                                if (splitTrData[43]!="")
                                {
                                    SerialSIM=splitTrData[43];
                                }
                                else
                                {
                                    SerialSIM="0";
                                }       
                                if (splitTrData[44]!="")
                                {
                                    imeiValue=splitTrData[44];
                                }
                                else
                                {
                                    imeiValue="0";
                                }

                            }   

                            if (m.sim_serial!=SerialSIM) m.sim_serial=SerialSIM;
                            if(imeiValue!="0")
                            {
                                if (m.Imei!=Convert.ToInt64(imeiValue)) m.Imei=Convert.ToInt64(imeiValue);
                            }
                        }
                    //    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertIntoMachinesConnectionTrace: Row 323 "); 
   
                    }
                    m.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                    m.IsOnline=true;
                    DB.MachinesConnectionTrace.Add(MachineTraceToAdd);
                    //Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertIntoMachinesConnectionTrace: Row 328 "); 
                    // I need the MachineTraceToAdd ID which is generated by the db, i have to call  SaveChanges() to have it.
                    DB.SaveChanges();
                    //Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertIntoMachinesConnectionTrace: Row 331 "); 
                    Thread t = new Thread(()=> MachinePacketAnalyzer( m.Id, transferred_data , MachineTraceToAdd.Id ));
                    t.Start();
                                       
                }
                
                DB.SaveChanges();
                
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertIntoMachinesConnectionTrace: Row 567 "); 
                //reload the web pages
                if(MachineTraceToAdd.IdMacchina!= null)
                {              
                    try{
                        new Thread(()=>
                            Functions.SignalRSender.AskToReloadMachConnTrace ((int)MachineTraceToAdd.IdMacchina  ) 
                        ).Start();                        
                    }
                    catch(Exception exc){
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " AskToReloadMachConnTrace: "+exc.Message);
                    }
                    try{
                        new Thread(()=>
                            Functions.SignalRSender.AskToReloadMachinesTable ( ) 
                        ).Start();                        
                    }
                    catch(Exception exc){
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " AskToReloadMachinesTable: "+exc.Message);
                    }
                }
            }
            catch(Exception e)
            {
                
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertIntoMachinesConnectionTrace: "+e.Message);
                //Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertIntoMachinesConnectionTrace: "+e.InnerException);
                
            }
            finally
            {
                if (AsynchronousSocketListener.GetTimeCheckStatus().ToUpper()=="TRUE")AsynchronousSocketListener.aTimer.Start();
                DB.DisposeAsync();
            }
        }


        /// <summary>
        /// Update Machine values(LGG, KalValue, ...)
        /// </summary>
        public static void MachinePacketAnalyzer(int id_macchina, string data, int id_MachinesConnectionTrace)
        {
            listener_DBContext DB = new listener_DBContext ();
            try
            {
                // Machines machinaDaAggiornare = DB.Machines.Single(h=> h.Id == id_macchina);
                // char[] delimiterChars = {'=', '<', '>',' '};
                // string[] mPacketArray = data.Split(delimiterChars);
                // List<string> list = new List<string>(mPacketArray);

                //<TCA=9876543210-21 LGG=00030LGA=00240KAL=00300>
                //<TCA=9876543210-22 +CSQ: 17,0OKATC-OK >

                //Since the new answer format from the C3 sucks we do not update values. 

                if(data.Contains("LGG="))
                {
                    if(DB.MachinesAttributes.Any(h=>h.IdAttributeNavigation.Name =="LGG" && h.IdMacchina == id_macchina))
                    {
                        DB.MachinesAttributes.Single(h=>h.IdAttributeNavigation.Name =="LGG" && h.IdMacchina == id_macchina)
                            .Value = data.Substring(data.IndexOf("LGG=")+4,   5);
                        DB.MachinesAttributes.Single(h=>h.IdAttributeNavigation.Name =="LGG" && h.IdMacchina == id_macchina)
                            .CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                    }
                    else
                    {
                        DB.MachinesAttributes.Add( 
                            new MachinesAttributes {
                                IdMacchina = id_macchina,
                                IdAttribute = DB.Attr.Single(l=>l.Name == "LGG").Id,
                                Value = data.Substring(data.IndexOf("LGG=")+4,  5   )
                                ,CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"))
                            }
                          );
                    }
                }
                if(data.Contains("LGA="))
                {
                    if(DB.MachinesAttributes.Any(h=>h.IdAttributeNavigation.Name =="LGA" && h.IdMacchina == id_macchina))
                    {
                        DB.MachinesAttributes.Single(h=>h.IdAttributeNavigation.Name =="LGA" && h.IdMacchina == id_macchina)
                            .Value = data.Substring(data.IndexOf("LGA=")+4,   5);
                        DB.MachinesAttributes.Single(h=>h.IdAttributeNavigation.Name =="LGA" && h.IdMacchina == id_macchina)
                            .CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                    }
                    else
                    {
                        DB.MachinesAttributes.Add( 
                            new MachinesAttributes {
                                IdMacchina = id_macchina,
                                IdAttribute = DB.Attr.Single(l=>l.Name == "LGA").Id,
                                Value = data.Substring(data.IndexOf("LGA=")+4,  5   )
                                ,CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"))
                            }
                          );
                    }
                }
                if(data.Contains("KAL="))
                {
                    if(DB.MachinesAttributes.Any(h=>h.IdAttributeNavigation.Name =="KAL" && h.IdMacchina == id_macchina))
                    {
                        DB.MachinesAttributes.Single(h=>h.IdAttributeNavigation.Name =="KAL" && h.IdMacchina == id_macchina)
                            .Value = data.Substring(data.IndexOf("KAL=")+4,   5);
                        DB.MachinesAttributes.Single(h=>h.IdAttributeNavigation.Name =="KAL" && h.IdMacchina == id_macchina)
                            .CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                    }
                    else
                    {
                        DB.MachinesAttributes.Add( 
                            new MachinesAttributes {
                                IdMacchina = id_macchina,
                                IdAttribute = DB.Attr.Single(l=>l.Name == "KAL").Id,
                                Value = data.Substring(data.IndexOf("KAL=")+4,  5   )
                                ,CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"))
                            }
                          );
                    }
                }
                if(data.Contains("+CSQ"))
                {
                    if(DB.MachinesAttributes.Any(h=>h.IdAttributeNavigation.Name =="CSQ" && h.IdMacchina == id_macchina))
                    {
                        DB.MachinesAttributes.Single(h=>h.IdAttributeNavigation.Name =="CSQ" && h.IdMacchina == id_macchina)
                            .Value = data.Substring(data.IndexOf("CSQ:")+4,   5);
                        DB.MachinesAttributes.Single(h=>h.IdAttributeNavigation.Name =="CSQ" && h.IdMacchina == id_macchina)
                            .CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));

                    }
                    else
                    {
                        DB.MachinesAttributes.Add( 
                            new MachinesAttributes {
                                IdMacchina = id_macchina,
                                IdAttribute = DB.Attr.Single(l=>l.Name == "CSQ").Id,
                                Value = data.Substring(data.IndexOf("CSQ:")+4,  5   )
                                ,CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"))
                            }
                          );
                    }
                }


                if (data.StartsWith("<TCA=>"))  
                {
                    if(DB.MachinesAttributes.Any(h=>h.IdAttributeNavigation.Name =="TCA" && h.IdMacchina == id_macchina))
                    {
                        DB.MachinesAttributes.Single(h=>h.IdAttributeNavigation.Name =="TCA" && h.IdMacchina == id_macchina)
                            .Value = data.Substring(data.IndexOf("TCA=")+4,   5);
                        DB.MachinesAttributes.Single(h=>h.IdAttributeNavigation.Name =="TCA" && h.IdMacchina == id_macchina)
                            .CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                    }
                    else
                    {
                        DB.MachinesAttributes.Add( 
                            new MachinesAttributes {
                                IdMacchina = id_macchina,
                                IdAttribute = DB.Attr.Single(l=>l.Name == "TCA").Id,
                                Value = data.Substring(data.IndexOf("TCA=")+4,  5   )
                                ,CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"))
                            }
                          );
                    }
                }
                              
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss MachinePacketAnalyzer : ") + e.Message);
            }
            DB.SaveChanges();
            DB.DisposeAsync();
        }


        /// <summary>
        ///
        /// </summary>
        public static void insertIntoDB(string dataToInsert)
        {  
            listener_DBContext DB = new listener_DBContext (); 

            try
            {
                DB.Dump.Add( new Dump { Data = dataToInsert });
                DB.SaveChanges();
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss : ") + e.Message);
            }
            finally{
                DB.DisposeAsync();
            }
        }
        
        /// <summary>
        /// Insert the command in the DB. 
        /// Return the command id in RemoteCommand table if succesfully,  
        /// -1 if no Machines matches the target (or other error)
        /// </summary>
        public static int insertIntoRemoteCommand(string content , string ipSender)
        {
            listener_DBContext DB = new listener_DBContext (); 
            try{
                //  esempio di come dovrebbe essere "data" 
                //
                //  <data>
                //    <transactionTarget>modem<transactionTarget/>
                //    <codElettronico>123456789</codElettronico>
                //    <command>TakeARide!</command>
                //  </data>

                XmlDocument data = new XmlDocument();
                data.LoadXml(content);

                String codElettronico = data.SelectSingleNode(@"/data/codElettronico").InnerText;
                String command = data.SelectSingleNode(@"/data/command").InnerText;
                RemoteCommand remCom = null;
#if DEBUG 

ipSender="176.242.21.246";


#endif
                if(DB.Machines.Any(y=> y.Mid == codElettronico))
                {
                    remCom = new RemoteCommand { 
                        Body = content,
                        Sender = ipSender,
                        IdMacchina = DB.Machines.First( y=> y.Mid == codElettronico    ).Id,
                        Status = "Pending",
                        ReceivedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss")),
                        SendedAt = null,
                        AnsweredAt = null
                    } ;
                    DB.RemoteCommand.Add( remCom  );
                }
                // if(DB.Machines.Any(y=> y.Mid == codElettronico))
                // {
                //     remCom = new RemoteCommand { 
                //         Body = content,
                //         Sender = ipSender,
                //         IdMacchina = DB.Machines.First( y=> y.Mid == codElettronico    ).Id,
                //         Status = "Pending",
                //         ReceivedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss")),
                //         SendedAt = null,
                //         AnsweredAt = null
                //     } ;
                //     DB.RemoteCommand.Add( remCom  );
                // }
                else
                {
                    remCom = new RemoteCommand { 
                        Body = content,
                        Sender = ipSender,
                        IdMacchina = null,
                        Status = "Error",
                        ReceivedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss")),
                        SendedAt = null,
                        AnsweredAt = null
                    } ;
                    DB.RemoteCommand.Add(remCom );
                }
                DB.SaveChanges();

                //reload the web page
                if(remCom.IdMacchina!= null)
                {              
                    try{
                        new Thread(()=>
                            Functions.SignalRSender.AskToReloadMachCommandTable ( (int)remCom.IdMacchina ) 
                        ).Start();
                    }
                    catch(Exception exc){
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " AskToReloadMachCommandTable: "+exc.Message);
                    }
                }

                if(remCom.Status=="Pending")
                    return remCom.Id;
                else 
                    return -1; //error

            }catch(Exception e )
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss insertIntoRemoteCommand : ") + e.Message);
                DB.DisposeAsync();
                return -1;
            }
            finally{DB.DisposeAsync();}
        }
        

        /// <summary>
        /// Check the last M1 packet for the machine with Machine ID = machineID and return the credits needed for a run
        /// </summary>
        public static int CalculateCreditForARun(int machineID)
        {
            listener_DBContext DB = new listener_DBContext (); 
            try
            {
                string mPacket = DB.MachinesConnectionTrace.Where( k=>k.IdMacchinaNavigation.Id == machineID &&  k.TransferredData.StartsWith("<TPK=$M1,")   )
                    .OrderByDescending(j=>j.Id).First().TransferredData;
                string[] mPacketArray = mPacket.Split(',');
                return (Convert.ToInt32(mPacketArray[3]) / Convert.ToInt32( mPacketArray[4]));
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " CalculateCreditForARun: "+e.Message);
                return -1;
            }
            finally{DB.DisposeAsync();}
        }   
        /// <summary>
        /// 
        /// </summary>
        public static string checkAnswerToCommand(string modemIp, int command_id , string commandtext)
        {
            listener_DBContext DB = new listener_DBContext ();
            
            string answer = "NoAnswer";
            string expectedAnswer = "";
            bool IsCommandSuccesful = false;
            try{
                char[] MyChar = {'1','2','3','4','5','6','7','8','9','0'};
                expectedAnswer = DB.CommandsMatch.First(y=>y.ModemCommand.StartsWith( commandtext.Trim(MyChar) )).expectedAnswer;
                string pattern = @"<TCA=[0-9\- ]+"+expectedAnswer+@"[0-9a-zA-Z \-\=]+>$";
                int Seconds = 15; // secondi max in cui aspetto che il modem mi risponda
                for(int i=0; i < (Seconds/2); i++)
                {
                    Thread.Sleep(2000);
                    MachinesConnectionTrace lastReceivedFromModem = DB.MachinesConnectionTrace
                        .Where(j => j.SendOrRecv == "RECV")
                        .OrderByDescending(z=>z.time_stamp)
                        .First(l => l.IpAddress == modemIp);
                    Match m = Regex.Match(lastReceivedFromModem.TransferredData, pattern, RegexOptions.IgnoreCase);
                    if(m.Success)
                    {
                        answer = lastReceivedFromModem.TransferredData;
                        IsCommandSuccesful = true;
                        break;
                    }
                    else
                    {
                        IsCommandSuccesful = false;
                    }
                }
            }
            catch(Exception e)
            {
                IsCommandSuccesful = false;
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " checkAnswerToCommand: "+e.Message);
            }
            finally
            {
                Thread t = new Thread(()=> updateRemoteCommandStatus( IsCommandSuccesful, command_id ));
                t.Start();
                DB.DisposeAsync();
            }
            return answer;
        }

        /// <summary>
        /// update the command status to its final state: Done or Error
        /// </summary>
        public static void updateRemoteCommandStatus(   bool IsCommandSuccesful, int command_id   )
        {
            listener_DBContext DB = new listener_DBContext (); 
            RemoteCommand commandToUpdate = DB.RemoteCommand.First(l => l.Id == command_id );
            commandToUpdate.AnsweredAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
            if(IsCommandSuccesful)
            {
                commandToUpdate.Status = "Done";
            }
            else
                commandToUpdate.Status = "Error";
            DB.SaveChanges();

            try{
                //let's reload the machcommandstable
                new Thread(()=>
                    Functions.SignalRSender.AskToReloadMachCommandTable ( (int)commandToUpdate.IdMacchina ) 
                ).Start();
                }
            catch(Exception exc){
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " AskToReloadMachCommandTable: "+exc.Message);
            }
            DB.DisposeAsync();
        }

        /// <summary>
        /// Match the commands in queue with an appopriate action: 
        /// IsAlive command can be answered from us,
        /// PlayTheGame must be forwarded to modem
        /// </summary>
        public static string[] FetchRemoteCommand( int RemoteCommand_ID )
        {   
            //  return:
            //  [0] =  ComandoNonRiconosciuto / ComandoDaGirare / ComandoDaEseguire
            //  [1] =  ip of the target machine 
            //  [2] =  command 
            //
            listener_DBContext DB = new listener_DBContext (); 
            string[] returnValues;
            try
            {
                RemoteCommand commandToExecute = DB.RemoteCommand.First(  y=>y.Id == RemoteCommand_ID  );
                XmlDocument data = new XmlDocument();
                data.LoadXml(commandToExecute.Body);

                string targetCodElettronico = data.SelectSingleNode(@"/data/codElettronico").InnerText;
                string webCom = data.SelectSingleNode(@"/data/command").InnerText;
                
                //match the web command with the 
                if(DB.CommandsMatch.Where(v=> v.WebCommand == webCom).Count() > 0)
                {
                    CommandsMatch CM = DB.CommandsMatch.First(v=> v.WebCommand == webCom);
                    string commandForModem = CM.ModemCommand;
                    //se il comando ?? parametrizzadile (quindi impostare un valore sul modem, cerco il valore da impostare)
                    if(CM.IsParameterizable)
                    {
                        string paramValue = data.SelectSingleNode(@"/data/value").InnerText;
                        if (!string.IsNullOrEmpty(paramValue))
                        {
                            commandForModem = commandForModem + paramValue;
//------------------------------------------------------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------------------------------------------------------
// Il comando MHD imposta il MID del modem, ?? MOLTO brutto averne due con lo stesso mid. Quindi controllo se il comando ?? MHD, nel caso controllo il
// parametro (il nuovo mid) e se gi?? presente nel db non invio il comando
                            if( commandForModem.Contains( "#MHD") )
                            {
                                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") +"diocane ce semo arrivati");
                                if(DB.Machines.Where( j => j.Mid ==  paramValue).Count() > 0)
                                {
                                    Machines MachineToUpdate = DB.Machines.First( y=> y.Mid == paramValue );
                        		    if (MachineToUpdate.MarkedBroken)
                                    {
                                        Machines machOrig = DB.Machines.First( y=> y.Mid == targetCodElettronico );
                                        if(MachineToUpdate != machOrig )
                                        {
                                            returnValues = new string[] { "ComandoDaGirare" , DB.Machines.First(y=>y.Mid == targetCodElettronico).IpAddress , commandForModem   };
                                            return returnValues;
                                        }
                                         returnValues = new string[] {"ComandoDaScartare","",""}; 
                                         return returnValues;
                                    }
                                    else
                                    {
                                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") +" mid: "+ paramValue + " already present in DB. Discarding command..");
                                        returnValues = new string[] {"ComandoDaScartare","",""}; 
                                        return returnValues;
                                    }
                        
                                }
                                else
                                {
                                    // insieme al nuovo MID mando il comando #RES, in modo da riavviare il modem e registare il nuovo MID nel DB
                                    // CONCATENZAIONE TEMPORANEAMENTE DISABILITATA
                                    //commandForModem += "#RES";
                                }
                            }
                        }
                        else{
                            // param value null 
                            returnValues = new string[] {"ComandoDaScartare","",""};                                     
                        }
                    }
//------------------------------------------------------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------------------------------------------------------
                    returnValues = new string[] {   "ComandoDaGirare" , DB.Machines.First(y=>y.Mid == targetCodElettronico).IpAddress , commandForModem   };
                }
                else{
                    if(webCom == "IsAlive")
                    {
                        returnValues = new string[] {   "ComandoDaEseguire" , "" , ""   };
                    }
                    else
                    {
                        returnValues = new string[] {   "ComandoNonRiconosciuto" , "" , ""   };

                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss FetchRemoteCommand : ") + e.Message);
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss FetchRemoteCommand : ") + e.InnerException);

                return new string[] { };   
            }
            finally
            {
                RemoteCommand commandToUpdate = DB.RemoteCommand.First(l => l.Id == RemoteCommand_ID );
                commandToUpdate.SendedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                DB.SaveChanges();
                DB.DisposeAsync();
            }
            return returnValues;
        }
            
        /// <summary>
        /// Return the coin need to play if alive, else return Mid+OFFLINE
        /// </summary>
        public static string IsAliveAnswer( int commandid )
        {
            listener_DBContext DB = new listener_DBContext (); 
            bool IsCommandSuccesful = false;
            string returnValue ="";
            try{
                Machines target =  DB.Machines.Single(h=> h.Id  ==   DB.RemoteCommand.Single(m=>m.Id == commandid).IdMacchina);
                bool isAlive = target.IsOnline;
                if(!isAlive)
                {
                    returnValue= "<Error>" + target.Mid + " offline</Error>" ;
                    IsCommandSuccesful = false;
                }
                else
                {
                    int costoDiUnGiro = CalculateCreditForARun(target.Id);
                    if(costoDiUnGiro == -1)
                    {
                        returnValue= "<Error>Errore nel calcolo del costo in crediti</Error>" ;
                        IsCommandSuccesful = false;
                    }
                    else
                    {
                        returnValue=  "<CreditsForARun>"+ costoDiUnGiro.ToString()+"</CreditsForARun>";
                        IsCommandSuccesful = true;
                    }
                }
            }catch(Exception e){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss IsAliveAnswer : ") + e.Message);
                returnValue= "<Error>Qualcosa non ha funzionato</Error>";
                IsCommandSuccesful = false;
            }
            finally
            {
                Thread t = new Thread(()=> updateRemoteCommandStatus( IsCommandSuccesful, commandid ));
                t.Start();
            }
            DB.DisposeAsync();
            return returnValue;
        }

    //la lista non tiene conto degli accentratori: comefunziona con n kiddie sotto un solo modem? indagare.


        public static void setModemOffline(IPAddress ip)
        {
          try
          {
            listener_DBContext DB = new listener_DBContext (); 
            DB.Machines.Single( j=> j.IpAddress ==  ip.ToString()).IsOnline = false;
            DB.SaveChanges();
            DB.DisposeAsync();
          }
          catch(Exception e)
          {
            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " setModemOffline failed for: " + ip.ToString());
          }         
        }
        public static void setModemOnline(IPAddress ip)
        {
          try
          {

            listener_DBContext DB = new listener_DBContext (); 
            DB.Machines.Single( j=> j.IpAddress ==  ip.ToString()).IsOnline = true;
            DB.SaveChanges();
            DB.DisposeAsync();
          }
          catch(Exception e)
          {
            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " setModemOnline failed for: " + ip.ToString());
          }
        }
        
        /// <summary>
        /// Checks the connection state
        /// </summary>
        /// <returns>True on connected. False on disconnected.</returns>
        public static bool IsConnected(Socket s)
        {
            try
            {
               bool part1 = s.Poll(1000, SelectMode.SelectRead);
                bool part2 = (s.Available == 0);
                if (part1 && part2)
                    return false;
                else
                    return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + "IsConnected : " + e.Message);
                return false;
            }
        }
        public static int StartCheckID_MCT() 
        {  
            try
            {
                int valRet=0;
                string connectionString=GetConnectString();
                MySqlConnection connection;
                MySqlDataReader dataReader;
                MySqlCommand cmd;
                string query = "";
                int k=0;
                connection = new MySqlConnection(connectionString);
                connection.Open();
                query = "select id_Val from  CheckConnTrace";
                cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                dataReader = cmd.ExecuteReader();
                Int32 last_idCheckTable=0;
                while (dataReader.Read())
                {
                    last_idCheckTable=Convert.ToInt32(dataReader["id_Val"].ToString());
                }
                dataReader.Close();
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + "  StartCheckID_MCT : last_idCheckTable ="+last_idCheckTable.ToString());

                query = "select id from  MachinesConnectionTrace order by id desc limit 1";
                cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                dataReader = cmd.ExecuteReader();
                Int32 last_id_MCT=0;
                while (dataReader.Read())
                {
                    last_id_MCT=Convert.ToInt32(dataReader["id"].ToString());
                }
                dataReader.Close();
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + "  StartCheckID_MCT : last_id_MCT = " +last_id_MCT.ToString());
                if (last_id_MCT==last_idCheckTable)
                {
                    valRet=1;
                }
                else
                {
                    if (last_idCheckTable==0)
                    {
                        query = "Insert into CheckConnTrace (id_Val, time_stamp) VALUE (" + last_id_MCT +",'" + DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss") + "');";
                        cmd = new MySqlCommand(query, connection);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + "  StartCheckID_MCT : inserito ultimo ID rilevato = " +last_id_MCT.ToString());
                    }
                    else
                    {
                        query = "Update  CheckConnTrace set id_Val =" + last_id_MCT +", time_stamp = '" + DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss") + "';";
                        cmd = new MySqlCommand(query, connection);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + "  StartCheckID_MCT : aggiornato ultimo ID rilevato = " +last_id_MCT.ToString());
                    }
                    valRet=0;
                    
                }
                return valRet;      
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + "StartCheckID_MCT : " + e.Message);
                return 1;
            }
        }  

        private class Logger
        {
            private static void modemConnectionDrop(IPAddress ip)
            {
                using(listener_DBContext DB = new listener_DBContext())
                {
                    Log logEntryToAdd = new Log{
                        DataCreazione = DateTime.Now,
                        
                    };
                }
            }

        }

        
    }    
}
