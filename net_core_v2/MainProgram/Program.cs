using System;  
using System.IO;
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;
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
    public const int BufferSize = 1024;  
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];  
// Received data string.  
    public StringBuilder sb = new StringBuilder();    
}  

  
public class AsynchronousSocketListener {  
    
    // allDoneModem is used to block and release the threads manually.
    public static ManualResetEvent allDoneModem = new ManualResetEvent(false);  
    public static ManualResetEvent allDoneCommand = new ManualResetEvent(false);  
    
    //configuration file, loaded at startup
    public static IConfiguration Configuration;
    
    //
    public static List<Socket> ModemsSocketList = new List<Socket>();

    public AsynchronousSocketListener() {  
    }  
  
    public static void StartListening() {  

        // Establish the local endpoint for the socket.  
        IPAddress ipAddress = IPAddress.Parse(  Configuration["LocalAddressForConnections"].ToString()); 
        //#if DEBUG
        //    ipAddress = IPAddress.Parse("192.168.17.210"); 
        //#endif
        
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
                Console.WriteLine("Waiting for a new connection (modem)...");
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
                Console.WriteLine("Waiting for a connection (backend)...");
                listenerForCommand.BeginAccept(new AsyncCallback(AcceptCallback),listenerForCommand );
  
                // Wait until a connection is made before continuing.  
                allDoneCommand.WaitOne();  
            }  
  
        } catch (Exception e) {  
            Console.WriteLine(e.ToString());  
        }  
    }

    



    public static void AcceptCallback(IAsyncResult ar) {  

        // Get the socket that handles the client request.  
        Socket listener = (Socket) ar.AsyncState;  
        Socket handler = listener.EndAccept(ar);  

        
        //figure out if the callback come from the modem or the backend, based on which port it come from, and signal the corrisponding thread to continue
        int ConPort =  ((IPEndPoint)handler.LocalEndPoint).Port;
        if(ConPort == 9005) {  
            // Signal the modem thread to continue.  
            allDoneModem.Set();  
            //add the connection to the list
            
            //ModemsSocketList = Functions.SocketListFunctions.addToList(handler, ModemsSocketList);
            ModemsSocketList.Add(handler);

            Console.WriteLine("Connection established to modem : " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ())+ " on internal port: " + (((IPEndPoint)handler.RemoteEndPoint).Port.ToString ()));
            Functions.DatabaseFunctions.insertIntoDB("Connection established to: " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()+ " on internal port: " + (((IPEndPoint)handler.RemoteEndPoint).Port.ToString ())));

            // Create the state object.  
            StateObject state = new StateObject();  
            state.workSocket = handler;  
            handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadFirstCallback), state);  
     
        }
        else if(ConPort == 9909){  
            // Signal the command thread to continue.   
            // TODO: analizzare se è necessario: in teoria il backend potrebbe essere uno solo e quindi non serve aspettarsi nuove connessioni con una aperta.
            allDoneCommand.Set();

            Console.WriteLine("Connection established to backend : " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ())  + " on internal port: " + (((IPEndPoint)handler.RemoteEndPoint).Port.ToString ()  ) );
            Functions.DatabaseFunctions.insertIntoDB("Connection established to backend: " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()+ " on internal port: " + (((IPEndPoint)handler.RemoteEndPoint).Port.ToString ())));

            StateObject state = new StateObject();  
            state.workSocket = handler;  
            handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCommandsCallback), state);  
     
        }  
    }  
  
    public static void ReadCommandsCallback(IAsyncResult ar) {
        String content = String.Empty;  
  
        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject) ar.AsyncState;  
        Socket handler = state.workSocket;  
        
        // Read data from the client socket.   
        try{
            int bytesRead = handler.EndReceive(ar);
            
            if (bytesRead > 0) {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));  
                content = state.sb.ToString();  
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",content.Length, content);
                Functions.DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " sent the command: "  + content);

                XmlDocument receivedCommand = new XmlDocument();
                receivedCommand.LoadXml(content);

                //implement a targeting solution, i shuold not send to handler but to someone defined in receivedCommand

                //ModemsSocketList.Find( m => ((IPEndPoint)m.RemoteEndPoint).Address.ToString()   == receivedCommand.InnerXml   )
                String targetModemIP = receivedCommand.SelectSingleNode(@"/data/targetip").InnerText;
                String command = receivedCommand.SelectSingleNode(@"/data/command").InnerText;
                //String port = receivedCommand.SelectSingleNode(@"/data/targetport").InnerText;
                
                Thread t = new Thread(()=>Send (  
                    ModemsSocketList.Find(      Soc => 
                        ((IPEndPoint)Soc.RemoteEndPoint).Address.ToString() == targetModemIP      
                       //&& ((IPEndPoint)Soc.RemoteEndPoint).Port.ToString()    == port         )  
                        ), command ));
                t.Start();

            }
        }catch(Exception e) {
            Console.WriteLine(e.ToString());
        }    

    }  
  

    public static void ReadFirstCallback(IAsyncResult ar) {
        String content = String.Empty;  
  
        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject) ar.AsyncState;  
        Socket handler = state.workSocket;  
        bool isAlive = true;
        // Read data from the client socket.   
        try{
            int bytesRead = handler.EndReceive(ar);
            
            if (bytesRead > 0) {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));  
        
                content = state.sb.ToString();  
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",content.Length, content);
                Functions.DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " send "+ content.Length.ToString() + " bytes, data : " + content);
        
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
                //de2BUl48  gQkjsp34 examples.
                //response << boost::format("%02X") % (int)checksum;

                byte b = Convert.ToByte(checksum);
                String hex = b.ToString("X");

                if (hex.Length < 2)
                    hex = '0' + hex;
                var finalString = new String(stringChars);
                finalString = finalString + hex;
                Thread t = new Thread(()=>Send (handler, "#PWD123456#ROK,"+finalString.ToString() +","+date1));
                
                t.Start();
                
            }
            else 
                if(bytesRead == 0){
                    Console.WriteLine("Connessione chiusa dal client");
                    DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " connection closed.");
                    handler.Shutdown(SocketShutdown.Both);  
                    handler.Close();
                    ModemsSocketList.Remove(  ModemsSocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)handler.RemoteEndPoint).Address  )  );
         

                    isAlive = false;
                }
        }
        catch(System.Net.Sockets.SocketException a){
            Console.WriteLine(a.ToString()); 
            isAlive = false;
        }
        catch(Exception e){
            Console.WriteLine(e.ToString());  
            isAlive = false;
        }finally {
            if(isAlive)
            {
                StateObject stateN = new StateObject();  
                stateN.workSocket = handler;
                Console.WriteLine("Listening again for data from :" + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
                Thread t = new Thread(()=> handler.BeginReceive( stateN.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadOtherCallback), stateN));
                t.Start();
            }
        }
    }
    public static void ReadOtherCallback(IAsyncResult ar) {
        StateObject state = (StateObject) ar.AsyncState;
        Socket handler = state.workSocket;
        String content = String.Empty;
           
        int bytesRead = handler.EndReceive(ar);
        try{
            if (bytesRead > 0) {
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));  
                content = state.sb.ToString();  
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",content.Length, content);
                Functions.DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " send "+ content.Length.ToString() + " bytes, data : " + content);
            }        
            else if(bytesRead == 0){
                        Console.WriteLine("Connessione chiusa dal client: "+ IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
                        DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " connection closed.");
                        ModemsSocketList.Remove(  ModemsSocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)handler.RemoteEndPoint).Address  )  );
         
                        handler.Shutdown(SocketShutdown.Both);  
                        handler.Close();
                        return;
            }
        }catch(Exception e) {
            Console.WriteLine("Errore comunicazione con: " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + e.Message);
            DatabaseFunctions.insertIntoDB("Errore comunicazione con: " +IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) );
            ModemsSocketList.Remove(  ModemsSocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)handler.RemoteEndPoint).Address  )  );
         
            handler.Shutdown(SocketShutdown.Both);  
            handler.Close();
            return;
        }
        Console.WriteLine("Listening again for data from :" + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
        state = new StateObject();
        Thread t = new Thread(()=> handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadOtherCallback), state));
        t.Start(); 

    }
      
    

    public static void Send(Socket handler, String data) {  
        
        //Console.WriteLine("Waiting for user command: ");
        //data = data + Console.ReadLine();
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);  

        try{  
        // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
          
        }catch(Exception e) {  
            Console.WriteLine("Begin send error: \n" + e.ToString());  
            Functions.DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " begin send error.");
          
        }
    }  
  
    private static void SendCallback(IAsyncResult ar) {  
        // Retrieve the socket from the state object.  
        Socket handler = (Socket) ar.AsyncState;  
        try {  
            
  
            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);  
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);  
            Functions.DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + ": sent " + bytesSent.ToString() + " to this client.");
          

        } catch (Exception e) {  
            ModemsSocketList.Remove(  ModemsSocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)handler.RemoteEndPoint).Address  )  );
            Console.WriteLine(e.ToString());  
        } finally {
            
            //Thread t = new Thread(()=>Send (handler, "#PSW123456"));
            //t.Start();
                
        }  
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
                !string.IsNullOrEmpty(Configuration["Port:Backend"])    
            ){
                // ...start Listening (for connection), it's hard to comment on this one.
                StartListening(); 
            }else{
                Console.WriteLine("Parameters missing in appsettings.json file, startup cancelled.");  
            }
            
             
        }
        else{
            Console.WriteLine("appsettings.json is missing, startup cancelled.");  
        }  
        return 0;  
    
    }  
}