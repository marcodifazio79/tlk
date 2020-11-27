﻿using System;  
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
    
    //
    public static List<Socket> ModemsSocketList = new List<Socket>();

    public AsynchronousSocketListener() {  
    }  
  
    public static void StartListening() {  

        // Establish the local endpoint for the socket.  
        IPAddress ipAddress = IPAddress.Parse(  Configuration["LocalAddressForConnections"].ToString()); 
        
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

        try{
        // Get the socket that handles the client request.  
        Socket listener = (Socket) ar.AsyncState;  
        Socket handler = listener.EndAccept(ar);  

        
        //figure out if the callback come from the modem or the backend, based on which port it come from, and signal the corrisponding thread to continue
        int ConPort =  ((IPEndPoint)handler.LocalEndPoint).Port;
        if(ConPort == 9005) {  
            // Signal the modem thread to continue.  
            allDoneModem.Set();  
            
            //add the connection to the list, but first remove it, just in case
            
            //ModemsSocketList.Remove(ModemsSocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)handler.RemoteEndPoint).Address  ));
            //ModemsSocketList.Add(handler);

            ModemsSocketList = Functions.SocketList.removeFromList(handler, ModemsSocketList);
            ModemsSocketList = Functions.SocketList.addToList(handler, ModemsSocketList);

            
            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Connection established to modem : "+ IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ())+ " on internal port: " + (((IPEndPoint)handler.RemoteEndPoint).Port.ToString ()));
            Functions.DatabaseFunctions.insertIntoMachinesTable(   ((IPEndPoint)handler.RemoteEndPoint).Address.ToString()); //,(((IPEndPoint)handler.RemoteEndPoint).Port));

            // Create the state object.  
            StateObject state = new StateObject();  
            state.workSocket = handler;  
            handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);  
     
        }
        else if(ConPort == 9909){  
            // Signal the command thread to continue.   
            // TODO: analizzare se è necessario: in teoria il backend potrebbe essere uno solo e quindi non serve aspettarsi nuove connessioni con una aperta.
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
                            Thread t = new Thread(()=>Send (
                            ModemsSocketList.Find(      Soc =>
                                ((IPEndPoint)Soc.RemoteEndPoint).Address.ToString() == remoteComm[1]
                                ),  "#PWD123456"+ remoteComm[2]));
                            t.Start();
                            //answerToBackend = "<Info>Comando inoltrato alla macchina</Info>";
                            answerToBackend = Functions.DatabaseFunctions.checkAnswerToCommand(remoteComm[1] , command_id );
                            
                        break;
                        case "ComandoDaEseguire":
                            answerToBackend = Functions.DatabaseFunctions.IsAliveAnswer(command_id);
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
            Thread responseToBackendThred = new Thread(()=>Send (  handler   ,  answerToBackend  ));
            responseToBackendThred.Start();
        }
    }
    public static void ReadCallback(IAsyncResult ar) {
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
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Read {0} bytes from socket. Data : {1}",content.Length, content);
                //Functions.DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " send "+ content.Length.ToString() + " bytes, data : " + content);
                Functions.DatabaseFunctions.insertIntoMachinesConnectionTrace( ((IPEndPoint)handler.RemoteEndPoint).Address.ToString() ,"RECV", content );
                if(content.Contains("<MID")){
                    //a questo punto mi aspetto che questo sia il primo pacchetto che ricevo dal modem,
                    //nell forma     <MID=1234567890-865291049819286><VER=110>
                    Functions.DatabaseFunctions.updateModemTableEntry(((IPEndPoint)handler.RemoteEndPoint).Address.ToString(), content);
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
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Connessione chiusa dal client");
                    Functions.DatabaseFunctions.insertIntoMachinesConnectionTrace( ((IPEndPoint)handler.RemoteEndPoint).Address.ToString() ,"RECV", "Connessione chiusa dal client" );
                    //ModemsSocketList.Remove(  ModemsSocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)handler.RemoteEndPoint).Address  )  );
                    ModemsSocketList = Functions.SocketList.removeFromList(handler, ModemsSocketList);
                    handler.Shutdown(SocketShutdown.Both);  
                    handler.Close();
                    isAlive = false;
                    return;
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
                try{
                    StateObject stateN = new StateObject();  
                    stateN.workSocket = ModemsSocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)handler.RemoteEndPoint).Address  );
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Listening again for data from :" + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
                    Thread t = new Thread(()=> handler.BeginReceive( stateN.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadOtherCallback), stateN));
                    t.Start();
                }catch(Exception e){
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Something went wrong while reopening the connection " + e.Message);
                }
            }
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
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Read "+ content.Length.ToString()+ "  bytes from socket. Data : " + content);
                    //Functions.DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " send "+ content.Length.ToString() + " bytes, data : " + content);
                    Functions.DatabaseFunctions.insertIntoMachinesConnectionTrace( ((IPEndPoint)handler.RemoteEndPoint).Address.ToString() ,"RECV", content );
                }
                else 
                {  
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,  
                    new AsyncCallback(ReadOtherCallback), state);
                }     
            
            }        
            else if(bytesRead == 0){
                        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Connessione chiusa dal client: "+ IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
                        //DatabaseFunctions.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " connection closed.");
                        Functions.DatabaseFunctions.insertIntoMachinesConnectionTrace( ((IPEndPoint)handler.RemoteEndPoint).Address.ToString() ,"RECV", content );
                        //ModemsSocketList.Remove(  ModemsSocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)handler.RemoteEndPoint).Address  )  );
                        ModemsSocketList = Functions.SocketList.removeFromList(handler, ModemsSocketList);
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        return;
            }


        }catch(Exception e) {
            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss : " ) + e.ToString());
            try{
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Errore comunicazione con: " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + e.Message);
                DatabaseFunctions.insertIntoDB("Errore comunicazione con: " +IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) );
                //ModemsSocketList.Remove(  ModemsSocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)handler.RemoteEndPoint).Address  )  );
                ModemsSocketList = Functions.SocketList.removeFromList(handler, ModemsSocketList);
                handler.Shutdown(SocketShutdown.Both);  
                handler.Close();
            }catch(Exception ex){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss : " ) + ex.ToString());
            }
            return;
        }
        Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + " : Listening again for data from :" + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
        state = new StateObject();
        state.workSocket = handler;
        state.workSocket.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadOtherCallback), state);
        //Thread t = new Thread(()=> state.workSocket.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadOtherCallback), state));
        //t.Start();
    }    

    public static void Send(Socket handler, String data) {  
        
        try{  
            //byte[] byteData = Encoding.ASCII.GetBytes(data);
            // Begin sending the data to the remote device.

            StateObject state = new StateObject();
            state.workSocket = handler;
            state.sb = new StringBuilder(data, data.Length);
            state.buffer = Encoding.ASCII.GetBytes(data);

            handler.BeginSend(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(SendCallback), state);
            
        }catch(Exception e) {
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
                //ModemsSocketList.Remove(  ModemsSocketList.Find(  y=>((IPEndPoint)y.RemoteEndPoint).Address == ((IPEndPoint)state.workSocket.RemoteEndPoint).Address  )  );
                ModemsSocketList = Functions.SocketList.removeFromList(state.workSocket, ModemsSocketList);

            }catch(Exception ex){
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + ex.ToString());
            } 
        } finally {
            
            //Thread t = new Thread(()=>Send (handler, "#PWD123456"));
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
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + ": Parameters missing in appsettings.json file, startup cancelled.");  
            }
            
             
        }
        else{
            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss" ) + ": appsettings.json is missing, startup cancelled.");  
        }  
        return 0;  
    
    }  
}