using System;
using System.Net;  
using System.Net.Sockets;
using System.Collections.Generic;
using System.Xml;

using Functions.database;

using System.Linq;

namespace Functions
{
    
    public class DatabaseFunctions
    {
        static string myConnectionString = "Server=10.10.10.71;Database=listener_DB;Uid=bot_user;Pwd=Qwert@#!99;";

        static listener_DBContext DB = new listener_DBContext (); 

        public static void updateModemTableEntry(string ip_addr,  string s)
        {
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
            }
        }


        public static void insertIntoMachinesTable(string ip_addr)
        {
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
                
            }
        }


        /////
        //  
        //  this should be probably be replaced by a trigger on the db, but it's a hard life
        //
        /////
        public static void updateModemlast_connection(string ip_addr)
        {
            try
            {
                MySql.Data.MySqlClient.MySqlConnection conn;
                conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();
                string sql = "UPDATE Modem SET last_communication = '"+DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss")+"' WHERE ip_address ='"+ ip_addr+"'";               
                var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
            }catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : "+ex.Message);
            }
        }


        
        public static void insertIntoMachinesConnectionTrace(string ip_addr, string send_or_recv, string transferred_data)
        {
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
            }catch(Exception e){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " insertIntoMachinesConnectionTrace: "+e.Message);
            }
    
        }



        public static void insertIntoDB(string dataToInsert)
        {  
            try
            {
                DB.Dump.Add( new Dump { Data = dataToInsert });
                DB.SaveChanges();
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss : ") + e.Message);
            }
        }
        public static void insertIntoDB_old(string dataToInsert)
        {  
            MySql.Data.MySqlClient.MySqlConnection conn;
            try
            {
                conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();
                Console.WriteLine("DB connection OK!");
                string sql = "INSERT INTO dump (data) VALUES ('"+ dataToInsert +"')";
                MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss : ") + ex.Message);
            }
            catch(Exception e)
            {
                
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss : ") + e.Message);
            }
            finally
            {
                //Console.WriteLine("Done.");
            }
        }


        public static string selectIpFromModem(string codElettronico)
        {
                MySql.Data.MySqlClient.MySqlConnection conn;
                try
                {
                    conn = new MySql.Data.MySqlClient.MySqlConnection();
                    conn.ConnectionString = myConnectionString;
                    conn.Open();
                    string sql = "SELECT ip_address FROM Modem WHERE mid = '"+ codElettronico +"'";
                    MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if(    string.IsNullOrEmpty(   reader.ToString()    )     )
                            return "nessun modem con quel mid.";
                        return reader.ToString();
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss : ") + e.Message);
                    return "error";
                }
            
        }
        /// <summary>
        /// Insert the command in the DB. 
        /// Return the command id in RemoteCommand table if succesfully,  
        /// -1 if no Machines matches the target (or other error)
        /// </summary>
        public static int insertIntoRemoteCommand(string content , string ipSender)
        {
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

            if(DB.Machines.Any(y=> y.IpAddress == ipSender))
            {

                remCom = new RemoteCommand { 
                    Body = data.ToString(),
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
                    Body = data.ToString(),
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
                return -1;
            }
        }
        /// <summary>
        /// Based on the elapsed time between the last communication and the Keep Alive value, estimate if the machines is online
        /// </summary>
        public static bool IsMachineAlive(int machineId)
        {

            if(DB.Machines.Any(y=> y.Id == machineId))
            {
                Machines m = DB.Machines.First(y=> y.Id == machineId);
                if(m.KalValue!=null)
                {
                    double miutesFromLastKalorPacket = (  DateTime.Now - DateTime.Parse( m.last_communication.ToString())).TotalMinutes;
                    if (m.KalValue >= miutesFromLastKalorPacket )
                    {
                        return true;
                    }   
                }   
            }
            return false;
        }

        /// <summary>
        /// Check the last M1 packet for the machine with Machine ID = machineID and return the credits needed for a run
        /// </summary>
        public static int CalculateCreditForARun(int machineID)
        {
            try
            {
                string mPacket = DB.MachinesConnectionTrace.First( k=>k.IdMacchina == machineID &&  k.SendOrRecv.StartsWith("<TPK=$M1,")   ).SendOrRecv;
                string[] mPacketArray = mPacket.Split(',');
                return (Convert.ToInt32(mPacketArray[3]) / Convert.ToInt32( mPacketArray[4]));
            }
            catch(Exception e)
            {
                return -1;
            }
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
            try
            {
                RemoteCommand commandToExecute = DB.RemoteCommand.First(  y=>y.Id == targetMachinesId  );
                XmlDocument data = new XmlDocument();
                data.LoadXml(commandToExecute.Body);

                switch(data.SelectSingleNode(@"/data/command").InnerText)
                {
                    case "IsAlive":
                        return new string[] {   "ComandoDaEseguire" , "" , ""   };
                    
                    case "PlayTheGame":
                        return new string[] {   "ComandoDaGirare" , "" , "#PSW123456#PU1"   };
                    
                    default:
                        return new string[] {   "ComandoNonRiconosciuto" , "" , ""   };
                }
                  
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss FetchRemoteCommand : ") + e.Message);
                return new string[] { };   
            }
        }
            
        /// <summary>
        /// Return the coin need to play if alive, else return Mid+OFFLINE
        /// </summary>
        public static string IsAliveAnswer( int machineId )
        {
            bool isAlive = IsMachineAlive(machineId);
            if(isAlive)
            {
                return "<Error>" + DB.Machines.Last(y=>y.Id == machineId).Mid + " offline</Error>" ;
            }
            else
            {
                int costoDiUnGiro = CalculateCreditForARun(machineId);
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
