﻿using System;
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
        public DatabaseFunctions()
        {
            
        }

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
                    MachineToUpdate.IsOnline = true;
                    MachineToUpdate.Version = version;
                    MachineToUpdate.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));
                }
                DB.SaveChanges();
               
                //reload web page
                try{
                    new Thread(()=>
                        Functions.SignalRSender.AskToReloadMachinesTable() 
                    ).Start();                        
                }
                catch(Exception exc){
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " AskToReloadMachinesTable: "+exc.Message);
                }

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
                    MachineToUpdate.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));

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
        public static void insertIntoMachinesConnectionTrace(string ip_addr, string send_or_recv, string transferred_data)
        {
            listener_DBContext DB = new listener_DBContext ();
            MachinesConnectionTrace MachineTraceToAdd = null;
            try
            {
                if(DB.Machines.Any( y=> y.IpAddress == ip_addr ))
                {
                    Machines m = DB.Machines.First( y=> y.IpAddress == ip_addr );
                    MachineTraceToAdd = new MachinesConnectionTrace 
                    {
                        IpAddress = m.IpAddress,
                        SendOrRecv = send_or_recv,
                        TransferredData = transferred_data,
                        IdMacchina = m.Id
                    };
                    DB.MachinesConnectionTrace.Add(MachineTraceToAdd);
                    Thread t = new Thread(()=> MachineExtendedAttributeUpdater( m.Id, transferred_data ));
                    t.Start();
                    m.last_communication = DateTime.Parse( DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"));

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
                        MachineTraceToAdd = new MachinesConnectionTrace 
                        {
                            IpAddress = ip_addr,
                            SendOrRecv = send_or_recv,
                            TransferredData = transferred_data
                        };
                        DB.MachinesConnectionTrace.Add(MachineTraceToAdd);
                    }
                }
                DB.SaveChanges();

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
                DB.DisposeAsync();
            }
        }
        
       

        /// <summary>
        /// Update Machine values(LGG, KalValue, ...)
        /// </summary>
        public static void MachineExtendedAttributeUpdater(int id_macchina, string data)
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
                //the TPK package contains various machine details, i'll use it to update the various field here.
                if(data.StartsWith("<TPK=$M1"))
                {
                    //let's make sure we are working on ONE packet (from time to time two packet get aggregated)
                    data = data.Substring(  0 , data.IndexOf(">") );
                    string[] mPacketArray = data.Split(',');
                    if(mPacketArray.Length == 40)
                    {
                        //<TCA=9876543210-21 LGG=00030LGA=00240KAL=00300>
                        //<TCA=9876543210-22 +CSQ: 17,0OKATC-OK >
                        MachineExtendedAttributeUpdater(id_macchina, "LGA=" + mPacketArray[38].PadLeft(  5, '0' ));
                        MachineExtendedAttributeUpdater(id_macchina, "LGG=" + mPacketArray[37].PadLeft(  5, '0' ));
                        MachineExtendedAttributeUpdater(id_macchina, "+CSQ:" +mPacketArray[32].PadLeft(  5, '0' ));
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss : ") +"MachineExtendedAttributeUpdater : TPK packet wrong size (" + 
                            mPacketArray.Length.ToString() + ")");
                    }
                }


            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss MachineExtendedAttributeUpdater : ") + e.Message);
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
                // if the command is like LGA600 or KAL500, whe must remove the param or get fucked. 
                // but removing the numbers from commands broke #PU1, so #PU1 get a special case,
                // becouse i cannot think of anything better at the moment
                char[] MyChar = {'1','2','3','4','5','6','7','8','9','0'};
                
                if(commandtext == "#PU1")
                    expectedAnswer = DB.CommandsMatch.Single(y=>y.ModemCommand.StartsWith( commandtext.Trim(MyChar) )).expectedAnswer;        
                else
                    expectedAnswer = DB.CommandsMatch.Single(y=>y.ModemCommand == commandtext.Trim(MyChar) ).expectedAnswer;        
                
                int Seconds = 12; // secondi max in cui aspetto che il modem mi risponda
                for(int i=0; i < (Seconds/2); i++){
                    Thread.Sleep(2000);
                
                    MachinesConnectionTrace lastReceivedFromModem = DB.MachinesConnectionTrace
                        .Where(j => j.SendOrRecv == "RECV")
                        .OrderByDescending(z=>z.time_stamp)
                        .First(l => l.IpAddress == modemIp);
                    
                    double secondsFromLastPacket = (  DateTime.Now - DateTime.Parse( lastReceivedFromModem.time_stamp.ToString())).TotalSeconds;
                    if(secondsFromLastPacket < Seconds)
                    {
                        if(lastReceivedFromModem.TransferredData.Contains(expectedAnswer))
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
                    Functions.SignalRSender.AskToReloadMachCommandTable ( command_id ) 
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
        public static string[] FetchRemoteCommand(   int RemoteCommand_ID   )
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
                    //se il comando è parametrizzadile (quindi impostare un valore sul modem, cerco il valore da impostare)
                    if(CM.IsParameterizable)
                    {
                        commandForModem = commandForModem + data.SelectSingleNode(@"/data/value").InnerText;
//------------------------------------------------------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------------------------------------------------------
// Il comando MHD imposta il MID del modem, è MOLTO brutto averne due con lo stesso mid. Quindi controllo se il comando è MHD, nel caso controllo il
// parametro (il nuovo mid) e se già presente nel db non invio il comando
                        if( commandForModem.Contains( "#MHD") )
                        {
                            if(DB.Machines.Select( j => j.Mid ==  data.SelectSingleNode(@"/data/value").InnerText).Count() > 0)
                            {
                                returnValues = new string[] {"ComandoDaScartare","",""}; 
                                return returnValues;
                            }
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

    }

    //la lista non tiene conto degli accentratori: comefunziona con n kiddie sotto un solo modem? indagare.

    public class SocketList
    {
        public static List<Socket> removeFromList(Socket SocketToRemove, List<Socket> SocketList)
        {
            try
            {
                
                try
                {
                    listener_DBContext DB = new listener_DBContext (); 
                    
                    //controllare se funziona il .Contains invece di .Exist, al momento non funziona intellisense e non mi va di scapocciarci.
                    if (SocketList.Exists(  x=>((IPEndPoint)x.RemoteEndPoint).Address.ToString() == ((IPEndPoint)SocketToRemove.RemoteEndPoint).Address.ToString()  ))
                    {
                        DB.Machines.First(  j=> j.IpAddress ==  ((IPEndPoint)SocketToRemove.RemoteEndPoint).Address.ToString() ).IsOnline = false;
                        SocketList.Remove(  SocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)SocketToRemove.RemoteEndPoint).Address  )  );                
                    }

                    DB.SaveChanges();
                    DB.DisposeAsync();

                    //reload web page
                    try{
                        new Thread(()=>
                            Functions.SignalRSender.AskToReloadMachinesTable() 
                        ).Start();                        
                    }
                    catch(Exception exc){
                            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " AskToReloadMachinesTable: "+exc.Message);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss removeFromList2 : ") + e.Message);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss removeFromList : ") + e.Message);
            }
            return SocketList;
        }

        public static List<Socket> addToList(Socket SocketToAdd, List<Socket> SocketList)
        {
            try
            {
                SocketList.Add(  SocketToAdd  ); 
                listener_DBContext DB = new listener_DBContext (); 
                DB.Machines.First(  j=> j.IpAddress ==  ((IPEndPoint)SocketToAdd.RemoteEndPoint).Address.ToString() ).IsOnline = true;
                DB.SaveChanges();
                DB.DisposeAsync();

                //reload web page
                try{
                    new Thread(()=>
                        Functions.SignalRSender.AskToReloadMachinesTable() 
                    ).Start();                        
                }
                catch(Exception exc){
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " AskToReloadMachinesTable: "+exc.Message);
                }

            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss addToList : ") + e.Message);
            }
            return SocketList;
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
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss IsConnected : ") + e.Message);
                return false;
            }
            
        }
        
    }

    
}