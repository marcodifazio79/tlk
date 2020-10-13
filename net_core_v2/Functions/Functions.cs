using System;
using System.Net;  
using System.Net.Sockets;
using System.Collections.Generic;

namespace Functions
{
    public class DatabaseFunctions
    {
        static string myConnectionString = "Server=127.0.0.1;Database=test;Uid=bot_user;Pwd=Qwert@#!99;";
        
        public static void insertIntoModemTable(string send_or_recv, string command, string modemAddress)
        {
            try
            {
                MySql.Data.MySqlClient.MySqlConnection conn;
                conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();

                //Console.WriteLine("DB connection OK!");

                string sql = "SELECT COUNT(*) AS TotalNORows, id FROM Modem WHERE ip_address = "+ modemAddress +" GROUP BY ip_address;";
                MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if(reader.GetInt32(0) > 0 )
                            {
                                Console.WriteLine("There's already a modem with that IP! Solve this.");
                                return;
                            }    
                            sql = "SELECT ";

                            Console.WriteLine(string.Format(
                                "Reading from table=({0}, {1}, {2})",
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetInt32(2)));
                        }
                    }

                
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
