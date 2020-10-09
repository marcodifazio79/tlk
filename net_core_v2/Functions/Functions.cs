using System;
using System.Net;  
using System.Net.Sockets;
using System.Collections.Generic;

namespace Functions
{
    public class DatabaseFunctions
    {        public static void insertIntoDB(string dataToInsert)
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
    }

    public class SocketListFunctions
    {
        public static List<Socket> addToList(Socket SocketToInsert, List<Socket> SocketList)
        {      
            
            if (SocketList.Exists(  x=>( (IPEndPoint)x.RemoteEndPoint).Address.ToString() == ((IPEndPoint)SocketToInsert.RemoteEndPoint).Address.ToString()  ))
            {
                //bool status = SocketList.Find(  x=>( (IPEndPoint)x.RemoteEndPoint).Address.ToString() == ((IPEndPoint)SocketToInsert.RemoteEndPoint).Address.ToString()   )
                //.Poll(-1, SelectMode.SelectWrite);

            }
            return SocketList;
        }
    }
}
