using System;
using System.Net;  
using System.Net.Sockets;
using System.Collections.Generic;
using System.Xml;
using System.Threading;

using Functions.database;

using System.Linq;

namespace Functions
{
    
    public class DatabaseFunctions
    {
        //static string myConnectionString = "Server=10.10.10.71;Database=listener_DB;Uid=bot_user;Pwd=Qwert@#!99;";
      
            
        /// <summary>
        ///  
        /// </summary>
        public static void updateModemTableEntry(string ip_addr,  string s)
        {
            listener_DBContext DB = new listener_DBContext (); 
            try
            {
                //   s = <MID=1234567890-865291049819286><VER=110>
                string mid = s.Substring(s.IndexOf("=")+1);
                mid =mid.Substring(0,mid.IndexOf("-"));

                string imei = s.Substring(s.IndexOf("-")+1);
                imei =imei.Substring(0,imei.IndexOf(">"));
                
                string version = s.Substring(s.IndexOf("VER=")+4);
                version = version.Substring(0,version.IndexOf(">"));

                if(DB.Machines.Any( y=> y.IpAddress == ip_addr )   )
                {
                    Machines MachineToUpdate = DB.Machines.First( y=> y.IpAddress == ip_addr );
                    MachineToUpdate.Imei =  Convert.ToInt64(imei);
                    MachineToUpdate.Mid = mid;
                    MachineToUpdate.Version = version;
                    MachineToUpdate.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                }
                DB.SaveChanges();
               
            }catch(Exception e){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : updateModemTableEntry: " + e.Message);
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
                if(DB.Machines.Any( y=> y.IpAddress == ip_addr )   )
                {
                    Machines MachineToUpdate = DB.Machines.First( y=> y.IpAddress == ip_addr ) ;
                    updateModemlast_connection(ip_addr);
                }
                else
                {
                    DB.Machines.Add( new Machines{
                        IpAddress = ip_addr,
                        Mid = "",
                        Version = "",
                        last_communication =null,
                        time_creation =null
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
        public static void updateModemlast_connection(string ip_addr)
        {
            listener_DBContext DB = new listener_DBContext (); 
            try
            {
                
                if( DB.Machines.Any(  s=>s.IpAddress == ip_addr ))
                {
                    Machines m = DB.Machines.First(  s=>s.IpAddress == ip_addr );
                    m.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                    DB.SaveChanges();
                }
            }catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : "+ex.Message);
            }
        }


        /// <summary>
        ///  
        /// </summary>
        public static void insertIntoMachinesConnectionTrace(string ip_addr, string send_or_recv, string transferred_data)
        {
            listener_DBContext DB = new listener_DBContext ();       
            try
            {
                if(   DB.Machines.Any( y=> y.IpAddress == ip_addr )   )
                {
                    Machines m = DB.Machines.First( y=> y.IpAddress == ip_addr );
                    DB.MachinesConnectionTrace.Add(new MachinesConnectionTrace 
                    {
                        IpAddress = m.IpAddress,
                        SendOrRecv = send_or_recv,
                        TransferredData = transferred_data,
                        IdMacchina = m.Id
                    });
                }
                else
                {
                    if(ip_addr.StartsWith("172.16."))
                    {
                        //if the ip is in the 172.16 net, it's a modem, otherwise is the backend, 
                        //and i don't wont to add the backand to the modem list
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : Machines not listed: adding..");
                        insertIntoMachinesTable(ip_addr);
                        //at this point i can just call me again to pupolate ModemConnectionTrace
                        insertIntoMachinesConnectionTrace( ip_addr, send_or_recv, transferred_data );
                    }
                    else
                    {
                        DB.MachinesConnectionTrace.Add(new MachinesConnectionTrace 
                        {
                            IpAddress = ip_addr,
                            SendOrRecv = send_or_recv,
                            TransferredData = transferred_data
                        });
                    }
                }
            DB.SaveChanges();
            }catch(Exception e){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertIntoMachinesConnectionTrace: "+e.Message);
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertIntoMachinesConnectionTrace: "+e.InnerException);
                
            }
            finally{
                DB.DisposeAsync();
            }
    
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
            //esempio di come dovrebbe essere "data" 
            //
            //<data>
	        //    <transactionTarget>modem<transactionTarget/>
	        //    <codElettronico>123456789</codElettronico>
	        //    <command>TakeARide!</command>
            //</data>

            XmlDocument data = new XmlDocument();
            data.LoadXml(content);

            String codElettronico = data.SelectSingleNode(@"/data/codElettronico").InnerText;
            String command = data.SelectSingleNode(@"/data/command").InnerText;
            RemoteCommand remCom = null;

            if(DB.Machines.Any(y=> y.Mid == codElettronico))
            {

                remCom = new RemoteCommand { 
                    Body = content,
                    Sender = ipSender,
                    IdMacchina = DB.Machines.First(   y=> y.Mid == codElettronico    ).Id,
                    Status = "Pending",
                    ReceivedAt = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss")),
                    SendedAt = null,
                    AnsweredAt = null
                } ;
                DB.RemoteCommand.Add( remCom  );
                
            }
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
                DB.RemoteCommand.Add( remCom  );
            }
            DB.SaveChanges();

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
        /// Based on the elapsed time between the last communication and the Keep Alive value, estimate if the machines is online
        /// </summary>
        public static bool IsMachineAlive(int machineId)
        {
            listener_DBContext DB = new listener_DBContext (); 
            try{
                if(DB.Machines.Any(y=> y.Id == machineId))
                {
                    Machines m = DB.Machines.First(y=> y.Id == machineId);
                    if(m.KalValue!=null)
                    {
                        double miutesFromLastKalorPacket = (  DateTime.Now - DateTime.Parse( m.last_communication.ToString())).TotalMinutes;
                        if (m.KalValue >= miutesFromLastKalorPacket )
                        {
                            DB.DisposeAsync();
                            return true;
                        }   
                    }   
            }
            }catch(Exception e){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " CalculateCreditForARun: "+e.Message);

            }
            finally{DB.DisposeAsync();}
            return false;
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
                .OrderByDescending(j=>j.Id).First().SendOrRecv;
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
        public static string checkAnswerToCommand(string modemIp)
        {
            listener_DBContext DB = new listener_DBContext (); 
            string answer = "NoAnswer";
            try{
            
                int Millisec = 15000; // millisecondi in cui aspetto che il modem mi risponda
                Thread.Sleep(10000);
                MachinesConnectionTrace lastReceivedFromModem = DB.MachinesConnectionTrace
                    .OrderByDescending(z=>z.time_stamp)
                    .First(l => l.IpAddress == modemIp);
                double secondsFromLastPacket = (  DateTime.Now - DateTime.Parse( lastReceivedFromModem.time_stamp.ToString())).TotalSeconds;
                if(secondsFromLastPacket > Millisec)
                    answer = lastReceivedFromModem.TransferredData;
            }
            catch(Exception e){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " checkAnswerToCommand: "+e.Message);
            }
            finally{
                DB.DisposeAsync();
            }
            return answer;
        }

        /// <summary>
        /// Match the commands in queue with an appopriate action: 
        /// IsAlive command can be answered from us,
        /// PlayTheGame must be forwarded to modem
        /// </summary>
        public static string[] FetchRemoteCommand(   int targetMachinesId   )
        {   
            //  return:
            //  [0] =  ComandoNonRiconosciuto / ComandoDaGirare / ComandoDaEseguire
            //  [1] =  ip of the target machine 
            //  [2] =  command 
            //
            listener_DBContext DB = new listener_DBContext (); 
            try
            {
                RemoteCommand commandToExecute = DB.RemoteCommand.First(  y=>y.Id == targetMachinesId  );
                XmlDocument data = new XmlDocument();
                data.LoadXml(commandToExecute.Body);
                
                string targetCodElettronico = data.SelectSingleNode(@"/data/codElettronico").InnerText;
                switch(data.SelectSingleNode(@"/data/command").InnerText)
                {
                    case "IsAlive":
                        return new string[] {   "ComandoDaEseguire" , "" , ""   };
                    
                    case "PlayTheGame":
                        return new string[] {   "ComandoDaGirare" , DB.Machines.First(y=>y.Mid == targetCodElettronico).IpAddress , "#PU1"   };
                    
                    case "Cassa":
                        return new string[] {   "ComandoDaGirare" , DB.Machines.First(y=>y.Mid == targetCodElettronico).IpAddress , "#CAS?"   };
                    

                    default:
                        return new string[] {   "ComandoNonRiconosciuto" , "" , ""   };
                }
                  
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss FetchRemoteCommand : ") + e.Message);
                return new string[] { };   
            }
            finally{
                DB.DisposeAsync();
                }
        }
            
        /// <summary>
        /// Return the coin need to play if alive, else return Mid+OFFLINE
        /// </summary>
        public static string IsAliveAnswer( int commandid )
        {
            listener_DBContext DB = new listener_DBContext (); 

            Machines t = DB.RemoteCommand.First( h=>h.Id == commandid ).IdMacchinaNavigation;
            bool isAlive = IsMachineAlive(  t.Id);
            if(isAlive)
            {
                DB.DisposeAsync();
                return "<Error>" + t.Mid + " offline</Error>" ;
            }
            else
            {
                int costoDiUnGiro = CalculateCreditForARun(t.Id);
                DB.DisposeAsync();
                if(costoDiUnGiro == -1)
                    return "<Error>Errore nel calcolo del costo in crediti</Error>" ;
                return  "<CreditsForARun>"+ costoDiUnGiro.ToString()+"</CreditsForARun>";
            }
        }      

    }

    //la lista non tiene conto degli accentratori: comefunziona con n kiddie sotto un solo modem? indagare.

    public class SocketListFunctions
    {
        
        
        public static List<Socket> removeFromList(Socket SocketToRemove, List<Socket> SocketList)
        {      
            
            if (SocketList.Exists(  x=>((IPEndPoint)x.RemoteEndPoint).Address.ToString() == ((IPEndPoint)SocketToRemove.RemoteEndPoint).Address.ToString()  ))
            {
                SocketList.Remove(  SocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)SocketToRemove.RemoteEndPoint).Address  )  );
            }

            return SocketList;
        }

        public static List<Socket> checkIfAlive(Socket SocketToCheck, List<Socket> SocketList)
        {      

            if( !((SocketToCheck.Poll(1200, SelectMode.SelectRead) && (SocketToCheck.Available == 0)) || !SocketToCheck.Connected))
            {
                return SocketList;    
            }
            else
            {
                SocketList.Remove(  SocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)SocketToCheck.RemoteEndPoint).Address  )  );
                return SocketList;
            }
                
        }
    }

    
}
