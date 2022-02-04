using System;
using System.Net;  
using System.Net.Sockets;
using System.Collections.Generic;
using System.Xml;
using System.Threading;
using Functions.database;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Custom;
using MySql.Data.MySqlClient;
using System.Data;

namespace ClearMachineTable
{    
    public class DatabaseClearTable
     {   
        public static IConfiguration Configuration;
        public DatabaseClearTable()
        {
            
        }
        public static string GetIPMode()
        {
            return ConfigurationManager.AppSetting["IPSet:IPFree"];
        }
        public static string GetServerType()
        {
            return ConfigurationManager.AppSetting["ServerType:TypeMachine"];
        }
        public static string GetConnectString()
        {
            string infoserver= GetServerType();
            string strinConn="";
            switch(infoserver)
            {
                case "ITA_PROD":
                    strinConn= ConfigurationManager.AppSetting["ConnectionStrings:DefaultConnectionITA_PROD"];
                break;
                case "ITA_SVI":
                    strinConn= ConfigurationManager.AppSetting["ConnectionStrings:DefaultConnectionITA_SVI"];
                break;
                case "ESP":
                    strinConn= ConfigurationManager.AppSetting["ConnectionStrings:DefaultConnectionESP"];
                break;

            }
            return strinConn;

        }

        public static void DeleteMachineByIP(string ip_addr)
        {
            try
            {
                Console.WriteLine("Strat DeleteMachineByIP :"+ ip_addr);
                listener_DBContext DB = new listener_DBContext ();
                if(DB.Machines.Any( y=> y.IpAddress == ip_addr ))
                {
                    Machines m = DB.Machines.First( y => y.IpAddress == ip_addr );
                    Console.WriteLine("DeleteMachineByIP :id Machines"+ m.Id.ToString());
                    DeleteMachine(m.Id.ToString(),"");
                    Console.WriteLine("DeleteMachineByIP :id Machines Delete OK");
                }
                DB.SaveChanges();
                DB.DisposeAsync();
            }
            catch(Exception e)
            {
                Console.WriteLine("DeleteMachineByIP :id Machines Delete Error");
            }
        }
        public static Boolean DeleteMachine(string idtodelete,string idtoupdate)
        { 
           bool valreturn = false;
            try
            {
                if (UpdateLogTables(idtodelete,idtoupdate))
                {
                    if (UpdateRemoteCommandTables (idtodelete, idtoupdate))
                    {
                        if (DeleteMachinesAttributesTables(idtodelete))
                        {
                            if (UpdateCashTransactionTables(idtodelete, idtoupdate))
                            {
                                if (UpdateMachinesConnectionTraceTables(idtodelete, idtoupdate))
                                {
                                    if (DeleteFromMachinestable(idtodelete)) valreturn = true;
                                }
                            }
                        }
                    }
                }

                return valreturn;
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : DeleteMachine: " + e.Message);
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " : DeleteMachine: " + e.StackTrace);
                return valreturn;
            }
            
        }
        private static bool UpdateLogTables(string id_machinetodelete, string idmachinetoupdate)
        {
            listener_DBContext DB = new listener_DBContext ();
            string connectionString =GetConnectString();
            MySqlConnection connection;
            connection = new MySqlConnection(connectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            try
            {
                MySqlCommand newcmd;
                string query = "Update Log set ID_machine='" + idmachinetoupdate + "'  where  ID_machine  = " + id_machinetodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("Error in UpdateLogTables: " + e.Message);
                return false;
            }
        }
        private static bool UpdateRemoteCommandTables(string id_machinetodelete, string idmachinetoupdate)
        {
            listener_DBContext DB = new listener_DBContext ();
            string connectionString =GetConnectString();
            MySqlConnection connection;
            connection = new MySqlConnection(connectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            try
            {
                MySqlCommand newcmd;
                string query = "Update RemoteCommand set id_Macchina ='" + idmachinetoupdate + "'  where  id_Macchina  = " + id_machinetodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in UpdateRemoteCommandTables: " + e.Message);
                return false;
            }
        }
        private static bool UpdateMachinesConnectionTraceTables(string id_machinetodelete, string idmachinetoupdate)
        {
            listener_DBContext DB = new listener_DBContext ();
            string connectionString =GetConnectString();
            MySqlConnection connection;
            connection = new MySqlConnection(connectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            try
            {
                MySqlCommand newcmd;
                string query = "Update MachinesConnectionTrace set id_Macchina ='" + idmachinetoupdate + "'  where  id_Macchina  = " + id_machinetodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in UpdateMachinesConnectionTraceTables: " + e.Message);
                return false;
            }
        }
        private static bool UpdateCashTransactionTables(string id_machinetodelete, string idmachinetoupdate)
        {
            listener_DBContext DB = new listener_DBContext ();
            string connectionString =GetConnectString();
            MySqlConnection connection;
            connection = new MySqlConnection(connectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            try
            {
                MySqlCommand newcmd;
                string query = "Update CashTransaction set ID_Machines ='" + idmachinetoupdate + "'  where  ID_Machines  = " + id_machinetodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in UpdateCashTransactionTables: " + e.Message);
                return false;
            }
        }
     
        private static bool DeleteLogTables(string id_machine) 
        {
            listener_DBContext DB = new listener_DBContext ();
            string connectionString =GetConnectString();
            MySqlConnection connection;
            connection = new MySqlConnection(connectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            MySqlCommand newcmd;
            string query;
            try
            {
                if( DB.Log.Any( y=> y.IdMachine == Convert.ToInt32(id_machine)) )
                {
                    query = "Delete FROM Log   where ID_machine = " + id_machine;
                    newcmd = new MySqlCommand(query, connection);
                    newcmd.ExecuteNonQuery();
                }
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("ERROR - DeleteLogTables: " + e.Message);
                //connection.Close();
                return false;
            }
        }
        
        private static bool DeleteRemoteCommand(string id_machine)
        {
            listener_DBContext DB = new listener_DBContext ();
            string connectionString =GetConnectString();;
            MySqlConnection connection;
            connection = new MySqlConnection(connectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            MySqlCommand newcmd;
            string query;
            try
            {
                if( DB.RemoteCommand.Any( y=> y.IdMacchina == Convert.ToInt32(id_machine)) )
                {
                    query = "Delete FROM RemoteCommand  where id_Macchina = " + id_machine;
                    newcmd = new MySqlCommand(query, connection);
                    newcmd.ExecuteNonQuery();
                }
                //connection.Close();
                return true;
            }
            catch (MySqlException e)
            {
                Console.WriteLine("ERROR - DeleteRemoteCommand: " + e.Message);
                //connection.Close();
                return false;
            }

        }
        private static bool DeleteMachinesConnectionTrace(string id_machine)
        {
            listener_DBContext DB = new listener_DBContext ();
            string connectionString =GetConnectString();
            MySqlConnection connection;
            connection = new MySqlConnection(connectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            MySqlCommand newcmd;
            string query;
            try
            {
                if( DB.MachinesConnectionTrace.Any( y=> y.IdMacchina == Convert.ToInt32(id_machine)) )
                {
                    query = "Delete from MachinesConnectionTrace where id_Macchina = " + id_machine;
                    newcmd = new MySqlCommand(query, connection);
                    newcmd.ExecuteNonQuery();
                }
                //connection.Close();
                return true;
            }
            catch (MySqlException e)
            {
                Console.WriteLine("ERROR - DeleteMachinesConnectionTrace: " + e.Message);
                //connection.Close();
                return false;
            }
            
        }
      
        private static bool DeleteFromMachinestable(string id_machine)
        {
            string connectionString =GetConnectString();
            MySqlConnection connection;
            connection = new MySqlConnection(connectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            MySqlCommand newcmd;
            string query;
            try
            {
                query = "Delete FROM Machines   where id = " + id_machine;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();

                //connection.Close();
                return true;
            }
            catch(MySqlException e)
            {
                Console.WriteLine("ERROR - DeleteFromMachinestable: " + e.Message);
                connection.Close();
                return false;
            }
        }
        private static bool DeleteCashTransTables(string id_machine)
        {
             listener_DBContext DB = new listener_DBContext ();
            string connectionString =GetConnectString();
            MySqlConnection connection;
            connection = new MySqlConnection(connectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            MySqlCommand newcmd;
            string query;
            try
            {
                if( DB.CashTransaction.Any( y=> y.IdMachines == Convert.ToInt32(id_machine)) )
                {
                    query = "Delete FROM CashTransaction   where ID_Machines = " + id_machine;
                    newcmd = new MySqlCommand(query, connection);
                    newcmd.ExecuteNonQuery();
                }
                //connection.Close();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR - DeleteCashTransTables: " + e.Message);
                //connection.Close();
                return false;
            }
        }
        private static bool DeleteMachinesAttributesTables(string id_machine)
        {
            listener_DBContext DB = new listener_DBContext ();
            string connectionString =GetConnectString();
            MySqlConnection connection;
            connection = new MySqlConnection(connectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            MySqlCommand newcmd;
            string query;
            try
            {
                if( DB.MachinesAttributes.Any( y=> y.IdMacchina == Convert.ToInt32(id_machine)) )
                {
                    query = "Delete FROM MachinesAttributes   where id_Macchina = " + id_machine;
                    newcmd = new MySqlCommand(query, connection);
                    newcmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR - DeleteMachinesAttributesTables: " + e.Message);
                //connection.Close();
                return false;
            }
        }

     }
}