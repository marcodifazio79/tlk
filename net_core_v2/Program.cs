using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;
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
    //same as said.
    public static ManualResetEvent allDoneCommand = new ManualResetEvent(false);  
    
    
    public AsynchronousSocketListener() {  
    }  
  
    public static void StartListening() {  

        // Establish the local endpoint for the socket.  
        IPAddress ipAddress = IPAddress.Parse("10.10.10.71"); 
        //#if DEBUG
        //    ipAddress = IPAddress.Parse("192.168.17.210"); 
        //#endif
        
        //endpoint per i modem
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 9005);  

        //endpoint per il backend
        IPEndPoint commandsInputEndPoint = new IPEndPoint(ipAddress, 9909);

        // Create a TCP/IP socket for the modem 
        Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp );  
  
        // Bind the socket to the local endpoint and listen for incoming connections from modems.  
        Thread tModems = new Thread(()=>StartListeningForModems(localEndPoint, listener));
        t.Start();

        Thread tCommands = new Thread(()=>StartListeningForCommands(commandsInputEndPoint, listener));
        t.Start();


        Console.WriteLine("\nPress ENTER to continue...");  
        Console.Read();  
    }  
    
    public static void StartListeningForModems(IPEndPoint localEndPoint, Socket listener){
         try {  
            listener.Bind(localEndPoint);  
            listener.Listen(100);  
  
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
    public static void StartListeningForCommands(IPEndPoint commandsInputEndPoint, Socket listener){
        try {  
            listener.Bind(commandsInputEndPoint);  
            listener.Listen(100);  
  
            while (true) {  
                // Set the event to nonsignaled state.  
                allDoneModem.Reset();  
  
                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine("Waiting for a new connection (commands backend)...");
                //listener.BeginAccept(new AsyncCallback(AcceptCallback), listener );  
                
                int receivedDataSize = 10;
                listener.BeginAccept(null, receivedDataSize, new AsyncCallback(AcceptReceiveDataCallback), listener);

                // Wait until a connection is made before continuing.  
                allDoneModem.WaitOne();  
            }
  
        } catch (Exception e) {  
            Console.WriteLine(e.ToString());  
        }
    }
    public static void AcceptReceiveDataCallback(IAsyncResult ar)
    {
        // Get the socket that handles the client request.
        Socket listener = (Socket) ar.AsyncState;

        // End the operation and display the received data on the console.
        byte[] Buffer;
        int bytesTransferred;
        Socket handler = listener.EndAccept(out Buffer, out bytesTransferred, ar);
        string stringTransferred = Encoding.ASCII.GetString(Buffer, 0, bytesTransferred);

        Console.WriteLine(stringTransferred);
        Console.WriteLine("Size of data transferred is {0}", bytesTransferred);

        // Create the state object for the asynchronous receive.
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
    }



    public static void AcceptCallback(IAsyncResult ar) {  

        // Signal the main thread to continue.  
        allDoneModem.Set();  

       
        // Get the socket that handles the client request.  
        Socket listener = (Socket) ar.AsyncState;  
        Socket handler = listener.EndAccept(ar);  
        Console.WriteLine("Connection established to: " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
        Functions.DatabaseFunctions.insertIntoDB("Connection established to: " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
        // Create the state object.  
        StateObject state = new StateObject();  
        state.workSocket = handler;  
        handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);  

       
        
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
                //Thread t = new Thread(()=>Send (handler, "#PWD123456#ROK,gQkjsp34,"+date1));
                
                t.Start();
                
            }
            else 
                if(bytesRead == 0){
                    Console.WriteLine("Connessione chiusa dal client");
                    Functions.DatabaseFunctionss.insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " connection closed.");
                    handler.Shutdown(SocketShutdown.Both);  
                    handler.Close(); 
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
                //////////////////////////////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////////////////////////////
                //TODO:
                // dopo aver stabilito la connessione (che a questo punto è fatta),                  /////
                // dovranno partire due thread: uno per il receive dei dati dal modem e uno          /////
                // per il send degli eventuali comandi che arriveranno dal beckend al modem          /////
                //////////////////////////////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////////////////////////////
                StateObject stateN = new StateObject();  
                stateN.workSocket = handler;
                Console.WriteLine("Listening again for data from :" + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) );
                handler.BeginReceive( stateN.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), stateN);

            }
        }

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
          
            //handler.Shutdown(SocketShutdown.Both);  
            //handler.Close();  
            //invece di chiudere il socket, lo rimetto in ricezione..
            //StateObject state = new StateObject();  
            //state.workSocket = handler;  
            //handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
  
        } catch (Exception e) {  
            Console.WriteLine(e.ToString());  
        } finally {
            
            //Thread t = new Thread(()=>Send (handler, "#PSW123456"));
            //t.Start();
                
        }  
    }  
  


      
    

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public static int Main(String[] args) {
        StartListening();  
        return 0;  
    }  
}