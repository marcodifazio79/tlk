using System;  
using System.IO;
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Xml;

using Functions;

//using System.Collections.Generic;
// State object for reading client data asynchronously  
public class StateObject {  
    // Client  socket.  
    public Socket workSocket = null;  
    // Size of receive buffer.  
    public const int BufferSize = 10240;  
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];  
    // Received data string.  
    public StringBuilder sb = new StringBuilder();    
}

  
public class AsynchronousSocketListener {  
    
    // allDoneModem is used to block and release the threads manually.
    public static ManualResetEvent allDoneModem = new ManualResetEvent(false);  
    public static ManualResetEvent allDoneCommand = new ManualResetEvent(false);  
    //connection requested by the server to the modem
    //private static ManualResetEvent connectDone = new ManualResetEvent(false); 
    
    //configuration file, loaded at startup
    public static IConfiguration Configuration;
    
    public static Dictionary<IPAddress, Socket> ConnectedModems = new Dictionary<IPAddress, Socket>(); 

    public AsynchronousSocketListener() {  
    }  
  
    public static void StartListening() {  
        //Establish the local endpoint for the socket.  
        #if DEBUG
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());  
            //IPAddress ipAddress = IPAddress.Parse("192.168.17.210"); //portatile
            IPAddress ipAddress = IPAddress.Parse("192.168.17.202"); //fisso lavoro
            //IPAddress ipAddress = IPAddress.Parse("192.168.117.127"); //ipHostInfo.AddressList[0];
        #else
            IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
            //IPAddress ipAddress = IPAddress.Parse(Configuration["LocalAddressForConnections"].ToString()); 
        #endif

        
        //endpoint per i modem
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress,Convert.ToInt32( Configuration["Port:Modem"]));

        //endpoint per il backend
        IPEndPoint commandsInputEndPoint = new IPEndPoint(ipAddress, Convert.ToInt32( Configuration["Port:Backend"]) );

        // Create a TCP/IP socket for the modem 
        Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp );  
  
        Socket listenerForCommand = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp );  
  
        // Bind the socket to the local endpoint and listen for incoming connections from modems.  
        Thread tModems = new Thread(()=>StartListeningForModems(localEndPoint, listener));
        tModems.Start();

        Thread tCommands = new Thread(()=>StartListeningForCommands(commandsInputEndPoint, listenerForCommand));
        tCommands.Start();


        //Console.WriteLine("\nPress ENTER to continue...");  
        Console.Read();  
    }  
    
    public static void StartListeningForModems(IPEndPoint localEndPoint, Socket listener){

         try {  
            listener.Bind(localEndPoint);  
            listener.Listen(1000);  
  
            while (true) {  
                // Set the event to nonsignaled state.  
                allDoneModem.Reset();  
  
                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : Waiting for a new connection (modem)...");
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener );  
  
                // Wait until a connection is made before continuing.  
                allDoneModem.WaitOne();  
            }  
  
        } catch (Exception e) {  
            Console.WriteLine(e.ToString());  
        }  
    }
    public static void StartListeningForCommands(IPEndPoint commandsInputEndPoint, Socket listenerForCommand){
        try {  
            listenerForCommand.Bind(commandsInputEndPoint);  
            listenerForCommand.Listen(1000);  
   
            while (true) {  
                // Set the event to nonsignaled state.  
                allDoneCommand.Reset();
  
                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" )+" : Waiting for a connection (backend)...");
                listenerForCommand.BeginAccept(new AsyncCallback(AcceptCallback),listenerForCommand );
  
                // Wait until a connection is made before continuing.  
                allDoneCommand.WaitOne();  
            }  
  
        } catch (Exception e) {  
            Console.WriteLine(e.ToString());  
        }  
    }

    public static void AcceptCallback(IAsyncResult ar) {  
//primo ingresso dati
        try{
        // Get the socket that handles the client request.  
        Socket listener = (Socket) ar.AsyncState;  
        Socket handler = listener.EndAccept(ar);  

        
        //figure out if the callback come from the modem or the backend, based on which port it come from, and signal the corrisponding thread to continue
        int ConPort =  ((IPEndPoint)handler.LocalEndPoint).Port;
        if(ConPort == 9005) {  
            string ip_as_string = IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()).ToString();
            
            // controllo che l'ip sia effettivamente quello di un modem
            //int val_ipset=Convert.ToInt16(DatabaseFunctions.GetIPMode());
            // controllo modificato per permettere l'utilizzo di SIM non VODAFONE
            
            // if(ip_as_string.StartsWith("172.16.")|val_ipset==1)  //if(ip_as_string.StartsWith("172.16."))
            // 
#if DEBUG 
ip_as_string="172.16.140.154";
#endif

                if (ip_as_string.StartsWith("10.10")| ip_as_string=="192.168.209.188")
                                {
                    try{
                        handler.Shutdown(SocketShutdown.Both);
                    }catch{}
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " tentativo di connessione da un dispositivo fuori la sottorete attesa ignorato");    
                }
                else
                {
                    // Signal the modem thread to continue.  
                    allDoneModem.Set();  

                    IPAddress ip = ((IPEndPoint)handler.RemoteEndPoint).Address;
                    if (ConnectedModems.ContainsKey(ip))
                    {
                        try{
                            // mi è arrivata una nuova connessione per un modem che risulta già connesso,
                            // chiudo e riapro.
                            Console.WriteLine("Closing old socket "+((IPEndPoint)ConnectedModems[ip].RemoteEndPoint).Address.ToString()+":"+((IPEndPoint)ConnectedModems[ip].RemoteEndPoint).Port.ToString());
                            ConnectedModems[ip].Shutdown(SocketShutdown.Both);
                            ConnectedModems[ip].Close(2);
                            ConnectedModems[ip].Dispose();
                            
                        }catch{
                            Console.WriteLine("Error closing the old socket for a modem conenction, check for socket leaks.");
                            return;
                        }
                        finally{
                            ConnectedModems[ip] = handler;
                        }
                        
                    }else{
                        ConnectedModems.Add(ip,handler);
                    }            
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Connection established to modem : "+ ip_as_string+ " on internal port: " + (((IPEndPoint)handler.RemoteEndPoint).Port.ToString ()));
                    Functions.DatabaseFunctions.insertIntoMachinesTable( ((IPEndPoint)handler.RemoteEndPoint).Address.ToString());

                    Functions.DatabaseFunctions.setModemOnline(ip);
                    
                    // Create the state object.  
                    StateObject state = new StateObject();  
                    state.workSocket = handler;
                    
                    //set the keep alive values for the socket
                    state.workSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    state.workSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);
                    state.workSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 6); //old value: 16
                    state.workSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 10); 

                    handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);  
                }
            
        }
        else if(ConPort == 9909){  
            // Signal the command thread to continue.   
            allDoneCommand.Set();

            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Connection established to backend : " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ())  + " on internal port: " + (((IPEndPoint)handler.RemoteEndPoint).Port.ToString ()  ) );

            StateObject state = new StateObject();  
            state.workSocket = handler;  
            handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCommandsCallback), state);  
     
        }
        }catch(Exception e){
            Console.WriteLine(e.ToString());
            
        }  
    }  
  
    public static void ReadCommandsCallback(IAsyncResult ar) {
        String content = String.Empty;  
  
        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject) ar.AsyncState;  
        Socket handler = state.workSocket;  
        string answerToBackend = "";
        // Read data from the client socket.   
        try{
            int bytesRead = handler.EndReceive(ar);
            
            if (bytesRead > 0) {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                content = state.sb.ToString();
            
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : Read {0} bytes from socket. Data : {1}",content.Length, content);
                Functions.DatabaseFunctions.insertIntoMachinesConnectionTrace( ((IPEndPoint)handler.RemoteEndPoint).Address.ToString() ,"RECV", content );
                
                //At this point content should look like = <data><codElettronico>9876543210</codElettronico><command>IsAlive</command></data>
              
             
                int command_id = Functions.DatabaseFunctions.insertIntoRemoteCommand(  content, ((IPEndPoint)handler.RemoteEndPoint).Address.ToString()  );
                if(  command_id == -1   )
                    answerToBackend = "<Error>command-error codice elettronico non collegato a una macchina nel db</Error>";
                else{
                    string[] remoteComm = Functions.DatabaseFunctions.FetchRemoteCommand(command_id);
                    //qua si aprono 3 casi in remoteComm[]: comando non riconosciuto, comando da girare a una macchina, comando a cui rispondere direttamente
                    switch(remoteComm[0])
                    {
                        case "ComandoNonRiconosciuto":
                            answerToBackend = "<Error>Comando non riconosciuto</Error>";
                        break;
                        case "ComandoDaGirare":
                            // meglio non mandare comandi a modem non collegati..
                            if(ConnectedModems.ContainsKey(IPAddress.Parse(remoteComm[1])))
                            {
                                Thread t = new Thread(()=>Send(
                                    ConnectedModems[IPAddress.Parse(remoteComm[1])],"#PWD123456"+ remoteComm[2]));
                                t.Start();
                                //answerToBackend = "<Info>Comando inoltrato alla macchina</Info>";
                                answerToBackend = Functions.DatabaseFunctions.checkAnswerToCommand(remoteComm[1] , command_id,  remoteComm[2]  );
                            }
                            else{
                                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : Comando ignorato, modem offline");
                            }
                        break;
                        case "ComandoDaEseguire":
                            answerToBackend = Functions.DatabaseFunctions.IsAliveAnswer(command_id);
                        break;
                        case "ComandoDaScartare":
                            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : Comando ignorato, controllare i parametri");
                        break;
                    }
                }
            }
        }
        catch(Exception e) {
            answerToBackend = "error";
            Console.WriteLine(e.ToString());
        }
        finally
        {
            //response for the backend
            //Thread responseToBackendThred = new Thread(()=> Send (  handler   ,  answerToBackend  ));
            //responseToBackendThred.Start();
            try{
                Send (  handler   ,  answerToBackend  );
                Console.WriteLine("Closing frontend connection -> {0}:{1}",((IPEndPoint)state.workSocket.RemoteEndPoint).Address.ToString(),((IPEndPoint)state.workSocket.RemoteEndPoint).Port.ToString());
                state.workSocket.Shutdown(SocketShutdown.Both);
                state.workSocket.Close(3);
                state.workSocket.Dispose();
            }catch(Exception e){
                Console.WriteLine("Error closing connection w command sender: "+ e.StackTrace);
            }
        }
    }
  
    public static async void ReadCallback(IAsyncResult ar) 
    {

       // listener_DBContext DB = new listener_DBContext ();
        //primo accesso dopo send data
        String content = String.Empty;  
  
        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject) ar.AsyncState;  
        Socket handler = state.workSocket;  
        // Read data from the client socket.   
        try{
            int bytesRead = handler.EndReceive(ar);
            
            if (bytesRead > 0) 
            {
                
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));  
                content = System.Text.RegularExpressions.Regex.Replace(state.sb.ToString(), @"\t|\n|\r", "");
                
                if (content=="<^>")   /*&& !content.EndsWith("^") */  
                {
                    await Task.Run(() => Send (handler, "#PWD123456#,"+"WDR"));
                    return;
                }
                if (content.IndexOf("<VER=") > -1  /*&& !content.EndsWith("^") */  ) 
                {
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Read {0} bytes from socket. Data : {1}",content.Length, content);
                    //Functions.DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " send "+ content.Length.ToString() + " bytes, data : " + content);
                    Functions.DatabaseFunctions.insertIntoMachinesConnectionTrace( ((IPEndPoint)handler.RemoteEndPoint).Address.ToString() ,"RECV", content );

                    if(content.Contains("<TYP=2"))
                    {
                        //a questo punto mi aspetto che questo sia il primo pacchetto che ricevo dal modem di una instagramm,
                        //nell forma     <MID=1234567890><VER=105><TYP=2>
                        string[] contentSplit= content.Split('>');
                        string fakeImei= DateTime.Now.ToString("yyyyMMddHHmmssf");
                        content=contentSplit[0]+"-" + fakeImei+">"+contentSplit[1]+">";
                        Functions.DatabaseFunctions.updateModemTableEntry(((IPEndPoint)handler.RemoteEndPoint).Address.ToString(), content);
                    }
                    else
                    {
                    //a questo punto mi aspetto che questo sia il primo pacchetto che ricevo dal modem,
                    //nell forma     <MID=1234567890-865291049819286><VER=110>
                        Functions.DatabaseFunctions.updateModemTableEntry(((IPEndPoint)handler.RemoteEndPoint).Address.ToString(), content);
                    }

                    if(content.Contains("<VER=500>")) // variante per modem del cazzo che non vuole una risposta
                    {
                        state = new StateObject();
                        state.workSocket = ConnectedModems[((IPEndPoint)handler.RemoteEndPoint).Address];
                        Thread th = new Thread(()=> handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadOtherCallback), state));
                        th.Start();
                        return;
                    }


                    string date1 = DateTime.Now.ToString("yy/MM/dd,HH:mm:ss");

                    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                    char checksum = '0';
                    var stringChars = new char[6];
                    var random = new Random();
                    for (int i = 0; i < stringChars.Length; i++)
                    {
                        stringChars[i] = chars[random.Next(chars.Length)];
                        if (i == 0)
                            checksum = stringChars[i];
                        else
                            checksum ^= stringChars[i];
                    }
    
                    byte b = Convert.ToByte(checksum);
                    String hex = b.ToString("X");

                    if (hex.Length < 2)
                        hex = '0' + hex;
                    var finalString = new String(stringChars);
                    finalString = finalString + hex;
                    //Thread t = new Thread(()=>Send (handler, "#PWD123456#ROK,"+finalString.ToString() +","+date1));
                    //t.Start();
                    await Task.Run(() => Send (handler, "#PWD123456#ROK,"+finalString.ToString() +","+date1));
                    
                    try
                    {
                        StateObject stateN = new StateObject();  
                        stateN.workSocket = ConnectedModems[((IPEndPoint)handler.RemoteEndPoint).Address];                        
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Listening again for data from :" + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
                        Thread th = new Thread(()=> handler.BeginReceive( stateN.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadOtherCallback), stateN));
                        th.Start();
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Something went wrong while reopening the connection " + e.Message);
                    }
                }
                else 
                {  
                    // Not all data received. Get more.  
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : getting more data.. (First connection)");
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,  
                        new AsyncCallback(ReadCallback), state);
                }    
            }
            else 
            {
               if(bytesRead == 0)
                {
                    IPAddress ip = ((IPEndPoint)handler.RemoteEndPoint).Address;
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Connessione chiusa dal client");
                    //Functions.DatabaseFunctions.insertIntoMachinesConnectionTrace( ip.ToString() ,"RECV", "Connessione chiusa dal client" );
                    ConnectedModems.Remove(ip);
                    Functions.DatabaseFunctions.setModemOffline(ip);
                    handler.Shutdown(SocketShutdown.Both);  
                    handler.Close(2); //wait 2 seconds before close
                    handler.Dispose();
                    return;
                }
            }
        }
        catch(System.Net.Sockets.SocketException a)
        {
            Console.WriteLine(a.ToString()); 
        }
        catch(Exception e)
        {
            Console.WriteLine(e.ToString());  
        }
    }
    public static void ReadOtherCallback(IAsyncResult ar) {
        StateObject state = (StateObject) ar.AsyncState;
        Socket handler = state.workSocket;
        String content = String.Empty;
           
        try{
            int bytesRead = handler.EndReceive(ar);
        
            if (bytesRead > 0) 
            {
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));  
                content = System.Text.RegularExpressions.Regex.Replace(state.sb.ToString(), @"\t|\n|\r", "");
                
                if (content.IndexOf(">") > -1) 
                {
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " - from ReadOtherCallback: Read "+ content.Length.ToString()+ "  bytes from socket. Data : " + content);
                    Functions.DatabaseFunctions.insertIntoMachinesConnectionTrace( ((IPEndPoint)handler.RemoteEndPoint).Address.ToString() ,"RECV", content );
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " - from ReadOtherCallback: conferma insertIntoMachinesConnectionTrace " + content);

                    // //a quick regex operation to check if it's a response to a response, 
                    // //in that case no response is nedeed
                    // string pattern = @"<TCA=[0-9]+-[0-9]+ >";
                    // Match m = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
                    // if( ! m.Success)
                    // {
                    //     //send response to modem
                    //     Thread responseToModemThread = new Thread(()=>Send (  handler   ,  "#CRK"  ));
                    //     responseToModemThread.Start();
                    // }
                }
                else 
                {
                    // Not all data received. Get more.  
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " - from ReadOtherCallback: getting more data..");
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,  
                    new AsyncCallback(ReadOtherCallback), state);
                }     
            
            }        
            else if(bytesRead == 0){
                IPAddress ip = ((IPEndPoint)handler.RemoteEndPoint).Address;
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " - from ReadOtherCallback: Connessione chiusa dal client: "+ ip.ToString ());
//                Functions.DatabaseFunctions.insertIntoMachinesConnectionTrace( ip.ToString() ,"RECV", content );
                ConnectedModems.Remove(ip);
                Functions.DatabaseFunctions.setModemOffline(ip);
                Console.WriteLine( "Shutting down socket "+ ip.ToString () +":"+((IPEndPoint)handler.RemoteEndPoint).Port.ToString() );
                handler.Shutdown(SocketShutdown.Both); 
                Console.WriteLine( "Closing socket "+ ip.ToString () +":"+((IPEndPoint)handler.RemoteEndPoint).Port.ToString() +" ...");
                handler.Close(2); //wait 2 seconds before close
                handler.Dispose();
                try{
                // SOCKET LEAKs DEBUG
                    StateObject originalState = (StateObject) ar.AsyncState;
                    Socket originalSocket = originalState.workSocket;
                    Console.WriteLine("Original socket: "+ ((IPEndPoint)originalSocket.RemoteEndPoint).Address.ToString () +":"+((IPEndPoint)originalSocket.RemoteEndPoint).Port.ToString());
                // SOCKET LEAKs DEBUG
                }catch{}
                return;
            }


        }catch(Exception e) {
            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss : " ) + e.ToString());
            try{
                IPAddress ip = ((IPEndPoint)handler.RemoteEndPoint).Address;
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " - from ReadOtherCallback: Errore comunicazione con: " +ip.ToString () +" -> " + e.Message);
                DatabaseFunctions.insertIntoDB("Errore comunicazione con: " + ip.ToString () );
                ConnectedModems.Remove(ip);
                Functions.DatabaseFunctions.setModemOffline(ip);
                //Console.WriteLine( "Shutting down socket "+ ip.ToString () +":"+((IPEndPoint)handler.RemoteEndPoint).Port.ToString() );
                Console.WriteLine( "Closing socket "+ ip.ToString () +":"+((IPEndPoint)handler.RemoteEndPoint).Port.ToString() +" ...");
                handler.Shutdown(SocketShutdown.Both);
                handler.Close(2); //wait 2 seconds before close
                handler.Dispose();
                try{
                //SOCKET LEAKs DEBUG
                    StateObject originalState = (StateObject) ar.AsyncState;
                    Socket originalSocket = originalState.workSocket;
                    Console.WriteLine("Original socket: "+ ((IPEndPoint)originalSocket.RemoteEndPoint).Address.ToString () +":"+((IPEndPoint)originalSocket.RemoteEndPoint).Port.ToString());
                // SOCKET LEAKs DEBUG
                }catch{}

            }catch(Exception ex){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss - from ReadOtherCallback: " ) + ex.ToString());
            }
            return;
        }
        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " - from ReadOtherCallback: Listening again for data from :" + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
        state = new StateObject();
        state.workSocket = handler;
        state.workSocket.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadOtherCallback), state);
        
    }    

    public static void Send(Socket handler, String data) {  
        StateObject state = new StateObject();
        IPAddress ip = IPAddress.Parse("127.0.0.1"); //a dummy value to initialize the variable.
        int sock_port = 0;
        try{  
            //byte[] byteData = Encoding.ASCII.GetBytes(data);
            // Begin sending the data to the remote device.

            ip = ((IPEndPoint)handler.RemoteEndPoint).Address; 
            sock_port = ((IPEndPoint)handler.RemoteEndPoint).Port;
            state.workSocket = handler;
            state.sb = new StringBuilder(data, data.Length);
            state.buffer = Encoding.ASCII.GetBytes(data);

            handler.BeginSend(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(SendCallback), state);
            
        }catch(Exception e) {
            if(e is System.Net.Sockets.SocketException)
            {
                // the modem get stucked in "send mode" from time to time. If this happen, 
                // trying to send to the modem cause a SocketException, i'll reset the connection if this occur
                ConnectedModems.Remove(ip);
                Functions.DatabaseFunctions.setModemOffline(ip);
                Console.WriteLine("Send error, closing "+ip.ToString()+":"+sock_port.ToString()); 
                try{
                    state.workSocket.Shutdown(SocketShutdown.Both); 
                    state.workSocket.Close(2); //wait 2 seconds before close
                    state.workSocket.Dispose();
                }catch{}
            }
            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + ": Begin send error: " + e.ToString());
            Functions.DatabaseFunctions.insertIntoDB("begin send error.");
        }
    }  
  
    private static void SendCallback(IAsyncResult ar) {  
        
        StateObject state = (StateObject) ar.AsyncState;
        
        try {  
  
            // Complete sending the data to the remote device.  
            int bytesSent = state.workSocket.EndSend(ar);
            
            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Sent {0} bytes to client.", bytesSent);  
            Functions.DatabaseFunctions.insertIntoMachinesConnectionTrace( ((IPEndPoint)state.workSocket.RemoteEndPoint).Address.ToString() ,"SEND", state.sb.ToString()  );
            
        } catch (Exception e) {  
            try{
                Console.WriteLine(e.ToString()); 
                IPAddress ip = ((IPEndPoint)state.workSocket.RemoteEndPoint).Address;
                int sock_port = ((IPEndPoint)state.workSocket.RemoteEndPoint).Port;
                Console.WriteLine("SendCallback error, closing "+ip.ToString()+":"+sock_port.ToString()); 
                ConnectedModems.Remove(ip);
                Functions.DatabaseFunctions.setModemOffline(ip);
                
                //state.workSocket.Shutdown(SocketShutdown.Both); 
                state.workSocket.Close(2); //wait 2 seconds before close
                state.workSocket.Dispose();
            }catch(Exception ex){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + ex.ToString());
            }    
        }
        // finally {
            
        //     Thread t = new Thread(()=>Send (handler, "#PWD123456"));
        //     t.Start();
                
        // }  
    }  
  

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public static int Main(String[] args) {
        
        //check if the config file exiast:
        if(  File.Exists("appsettings.json")){
            Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

            if(  
                //check if the config file have necessary entries:
                !string.IsNullOrEmpty(Configuration["LocalAddressForConnections"].ToString())    &
                !string.IsNullOrEmpty(Configuration["Port:Modem"].ToString())                    &
                !string.IsNullOrEmpty(Configuration["Port:Backend"])                             &
                !string.IsNullOrEmpty(Configuration["ConnectionStrings:DefaultConnection"])
            ){

            // ...start Listening (for connection), it's hard to comment on this one.
            StartListening(); 

            }else{
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + ": Parameters missing in appsettings.json file, startup cancelled.");  
            }
        }
        else{
            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + ": appsettings.json is missing, startup cancelled.");  
        }  
        return 0;
    }
}