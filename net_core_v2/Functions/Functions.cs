using System;
using System.Net;  
using System.Net.Sockets;
using System.Collections.Generic;

namespace Functions
{
    public class DatabaseFunctions
    {
        static string myConnectionString = "Server=127.0.0.1;Database=listener_DB;Uid=bot_user;Pwd=Qwert@#!99;";
        
        public static void updateModemTableEntry(string ip_addr,  string s)
        {
            try
            {
                //   s = <MID=1234567890-865291049819286><VER=110>
                string mid = s.Substring(s.IndexOf("=")+1);
                mid =mid.Substring(0,mid.IndexOf("-"));

                string imei = s.Substring(s.IndexOf("-")+1);
                imei =imei.Substring(0,imei.IndexOf(">"));
                

                string version = s.Substring(     s.IndexOf("VER=")+4     );
                version = version.Substring(0,version.IndexOf(">"));

                MySql.Data.MySqlClient.MySqlConnection conn;
                conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();

                string sql = "UPDATE Modem SET imei = "+imei+", mid="+mid+", version = "+version+", last_communication='"+DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss")+"' WHERE ip_address = '"+ ip_addr+"'";
                MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }catch(Exception e){
                Console.WriteLine(e.Message);
            }
        }


        public static void insertIntoModemTable(string ip_addr)
        {
            try
            {
                MySql.Data.MySqlClient.MySqlConnection conn;
                conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();

                string sql = "SELECT id FROM Modem WHERE ip_address = '"+ ip_addr +"'";
                MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            reader.Close();
                            updateModemlast_connection(ip_addr);
                            
                            reader.Close();
                            sql = "INSERT INTO Modem (ip_address) VALUES ('"+ip_addr+"')";
                            cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                            cmd.ExecuteNonQuery();
                        }
                    }
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
                Console.WriteLine("sbadamiao: "+ip_addr);
                string sql = "UPDATE Modem (last_communication) VALUES ('"+DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss")+"') WHERE ip_address ='"+ ip_addr+"'";
                Console.WriteLine("sbadastinga: "+sql);
                
                var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
            }catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : "+ex.Message);
            }
        }
        public static void insertIntoModemConnectionTrace(string ip_addr, string send_or_recv, string transferred_data)
        {
            try
            {
                MySql.Data.MySqlClient.MySqlConnection conn;
                conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();
                string sql = "SELECT id FROM Modem WHERE ip_address = '"+ ip_addr +"'";
                MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            if(!ip_addr.StartsWith("172.16."))
                            {
                                //if the ip is in the 172.16 net, it's a modem, otherwise is the backend, and i don't wont
                                //to add the backand to the modem list
                                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : Modem not listed: adding..");
                                insertIntoModemTable(ip_addr);
                            }
                            else
                            {
                                reader.Close();
                                sql = "INSERT INTO ModemConnectionTrace  (ip_address,send_or_recv,transferred_data) VALUES ('"+ip_addr+"','"+send_or_recv+ "','"+transferred_data+"')";
                                cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                                cmd.ExecuteNonQuery();                            }
                        }
                        else
                        {
                            reader.Close();
                            sql = "INSERT INTO ModemConnectionTrace  (ip_address,send_or_recv,transferred_data) VALUES ('"+ip_addr+"','"+send_or_recv+ "','"+transferred_data+"')";
                            cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                            cmd.ExecuteNonQuery();
                            updateModemlast_connection(ip_addr);
                        }
                    }
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : "+ex.Message);
            }

        }

        public static void insertIntoDB(string dataToInsert)
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
