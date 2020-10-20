﻿using System;
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
                string mid = s.Substring(0, s.IndexOf(">"));
                mid =mid.Substring(mid.IndexOf("="),mid.Length-1);

                string imei = s.Substring(s.IndexOf("<",3), s.IndexOf(">",s.IndexOf(">")+1));
                imei =imei.Substring(imei.IndexOf("="),imei.Length-1);
                
                MySql.Data.MySqlClient.MySqlConnection conn;
                conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();

                string sql = "UPDATE Modem (imei, mid, version , last_communication) VALUES ('"+imei+","+ mid+","+DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss")+"') WHERE ip_address = "+ ip_addr;
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

                string sql = "SELECT COUNT(*) AS TotalNORows, id FROM Modem WHERE ip_address = '"+ ip_addr +"' GROUP BY ip_address";
                MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if(reader.GetInt32(0) > 0 )
                            {
                                //so there's already a modem with that IP, let's just update the last_communication value with "now"
                                sql = "UPDATE Modem (last_communication) VALUES ('"+DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss")+"') WHERE ip_address = "+ ip_addr;
                                cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                                cmd.ExecuteNonQuery();
                                conn.Close();
                                return;
                            }
                            
                            //Console.WriteLine(string.Format(
                            //    "Reading from table=({0}, {1}, {2})",
                            //    reader.GetInt32(0),
                            //    reader.GetString(1),
                            //    reader.GetInt32(2)));
                        }
                        reader.Close();
                        sql = "INSERT INTO Modem (ip_address,tcp_local_port) VALUES ('"+ip_addr+"')";
                        cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                        cmd.ExecuteNonQuery();
                    }

                
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public static void insertIntoModemModemConnectionTrace(string ip_addr, string send_or_recv, string transferred_data)
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
                            Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : Modem not listed: adding..");
                            insertIntoModemTable(ip_addr);
                        }
                        reader.Close();
                        sql = "INSERT INTO ModemConnectionTrace  (ip_address,send_or_recv,transferred_data) VALUES ('"+ip_addr+"','"+send_or_recv+ "','"+transferred_data+"')";
                        cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                        cmd.ExecuteNonQuery();
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
