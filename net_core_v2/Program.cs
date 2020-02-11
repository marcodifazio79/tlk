using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;  

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
    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);  
   
    
    public AsynchronousSocketListener() {  
    }  
  
    public static void StartListening() {  

        
        // Establish the local endpoint for the socket.  
        IPAddress ipAddress = IPAddress.Parse("10.10.10.71"); 
        //#if DEBUG
        //    ipAddress = IPAddress.Parse("192.168.17.210"); 
        //#endif
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 9005);  
  
        // Create a TCP/IP socket.  
        Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp );  
  
        // Bind the socket to the local endpoint and listen for incoming connections.  
        try {  
            listener.Bind(localEndPoint);  
            listener.Listen(100);  
  
            while (true) {  
                // Set the event to nonsignaled state.  
                allDone.Reset();  
  
                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine("Waiting for a new connection...");
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener );  
  
                // Wait until a connection is made before continuing.  
                allDone.WaitOne();  
            }  
  
        } catch (Exception e) {  
            Console.WriteLine(e.ToString());  
        }  
  
        Console.WriteLine("\nPress ENTER to continue...");  
        Console.Read();  
  
    }  
  
    public static void AcceptCallback(IAsyncResult ar) {  

        // Signal the main thread to continue.  
        allDone.Set();  

       
        // Get the socket that handles the client request.  
        Socket listener = (Socket) ar.AsyncState;  
        Socket handler = listener.EndAccept(ar);  
        Console.WriteLine("Connection established to: " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
        insertIntoDB("Connection established to: " + IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()));
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
                insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " send "+ content.Length.ToString() + " bytes, data : " + content);
        
                string date1 = DateTime.Now.ToString("yy/MM/dd,HH:mm:ss");


                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                var stringChars = new char[8];
                var random = new Random();

                for (int i = 0; i < stringChars.Length; i++)
                {
                    stringChars[i] = chars[random.Next(chars.Length)];
                }

                var finalString = new String(stringChars);

                //Console.WriteLine("data1: {0} \n",date1);
                
                //Thread t = new Thread(()=>Send (handler, "#PWD123456#ROK,"+finalString.ToString() +","+date1));
                Thread t = new Thread(()=>Send (handler, "#PWD123456#ROK,gQkjsp34,"+date1));
                
                t.Start();
                
            }
            else 
                if(bytesRead == 0){
                    Console.WriteLine("Connessione chiusa dal client");
                    insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " connection closed.");
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
                // Create the state object.  
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
            insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + " begin send error.");
          
        }
    }  
  
    private static void SendCallback(IAsyncResult ar) {  
        // Retrieve the socket from the state object.  
        Socket handler = (Socket) ar.AsyncState;  
        try {  
            
  
            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);  
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);  
            insertIntoDB(IPAddress.Parse (((IPEndPoint)handler.RemoteEndPoint).Address.ToString ()) + ": sent " + bytesSent.ToString() + " to this client.");
          
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
////////////////////////////////////////DATABASE STUFF///////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void insertIntoDB(string dataToInsert)
    {  
        MySql.Data.MySqlClient.MySqlConnection conn;
        string myConnectionString;
        myConnectionString = "Server=127.0.0.1;Database=test;Uid=bot_user;Pwd=Qwert@#!99;";
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
            
            Console.WriteLine(ex.Message);
        }
        catch(Exception e)
        {
            
            Console.WriteLine(e.Message);
        }
        finally
        {
            
            
            //Console.WriteLine("Done.");
        }
    }  
    

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public static int Main(String[] args) {  
        StartListening();  
        return 0;  
    }  
}