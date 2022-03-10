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
                    string idtoremove=m.Id.ToString();
                    DB.DisposeAsync();

                    if (RemoveMachine(idtoremove)) 
                    {
                        Console.WriteLine("DeleteMachineByIP :id Machines "+ m.Id.ToString() +" Delete OK");
                    }
                }
                
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
                if (idtoupdate!="")
                {   
                    if (UpdateMachines( idtodelete, idtoupdate)) valreturn=true;;
                }
                else
                {
                    if (RemoveMachine(idtodelete)) valreturn=true;
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
        private static bool UpdateMachines( string IDMachinesTodelete,string IDMachinesToUpdate)
        {
             MySqlConnection connection;
            string connectionString =GetConnectString();
            connection = new MySqlConnection(connectionString);
                                             
            if (connection.State == ConnectionState.Closed) connection.Open();
            MySqlCommand newcmd;
            string query;
            try
            {
                query = "Update Log set ID_machine='" + IDMachinesToUpdate + "'  where  ID_machine  = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();

                query = "Update RemoteCommand set id_Macchina ='" + IDMachinesToUpdate + "'  where  id_Macchina  = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();

                query = "Delete FROM MachinesAttributes   where id_Macchina = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();

                query = "Update CashTransaction set ID_Machines ='" + IDMachinesToUpdate + "'  where  ID_Machines  = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();

                query = "Update CashTransaction set ID_Machines ='" + IDMachinesToUpdate + "'  where  ID_Machines  = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();

                query = "Delete FROM Machines   where id = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("ERROR - UpdateMachines: " + e.Message);
                if (connection.State == ConnectionState.Open) connection.Close();
                Console.WriteLine("ERROR - RemoveMachine: connection.State= " +connection.State.ToString() );
                return false;
            }
        }
        private static bool RemoveMachine( string IDMachinesTodelete)
        {
            MySqlConnection connection;
            string connectionString =GetConnectString();
            connection = new MySqlConnection(connectionString);
                                             
            if (connection.State == ConnectionState.Closed) connection.Open();
            MySqlCommand newcmd;
            string query;
            try
            {
                
                query = "Delete FROM Log where ID_machine = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();
                
                query = "Delete FROM RemoteCommand  where id_Macchina = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();
                
                query = "Delete FROM MachinesAttributes   where id_Macchina = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();
                
                query = "Delete FROM CashTransaction   where ID_Machines = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();
                
                query = "Delete from MachinesConnectionTrace where id_Macchina = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();
                
                query = "Delete FROM Machines   where id = " + IDMachinesTodelete;
                newcmd = new MySqlCommand(query, connection);
                newcmd.ExecuteNonQuery();

                Console.WriteLine("Deleted Id Machines: " + IDMachinesTodelete);
                connection.Close();
                return true;
					
            }
            catch(Exception e)
            {
                Console.WriteLine("ERROR - RemoveMachine: " + e.Message);
                if (connection.State == ConnectionState.Open) connection.Close();
                Console.WriteLine("ERROR - RemoveMachine: connection.State= " +connection.State.ToString() );
                return false;
            }
        }
       public static void ClearDB()
        {

            MySqlConnection connection;
            MySqlDataReader dataReader;
            MySqlCommand cmd;
            //Dictionary<string, string> IDMachinesToDelTemp = new Dictionary<string, string>();
            Dictionary<string, string> RowMCTToDel = new Dictionary<string, string>();
            List<string> IDMachinesTodelete = new List<string>();
            List<string> TMPIDMachinesTodelete = new List<string>();
            
            Dictionary<string, string> TMPIDMachinesToUpdate = new Dictionary<string, string>();

            string query = "";
         
            string connectionString =GetConnectString();
            connection = new MySqlConnection(connectionString);
            connection.Open();
            query = "select id from  Machines where mid like '77770001_%' or mid like '5555555%' or mid like 'TCC%' and IsOnline=0";
            cmd = new MySqlCommand(query, connection);
            cmd.ExecuteNonQuery();
//            cmd.CommandTimeout = 60;
            dataReader = cmd.ExecuteReader();
            int k = 0;
            
            while (dataReader.Read())
            {
                IDMachinesTodelete.Add(dataReader["id"].ToString());
            }
            dataReader.Close();
            foreach (string id in IDMachinesTodelete)
            {
                DeleteMachine(id, "");
            }

            IDMachinesTodelete.Clear();

            query = "select id from  Machines where mid like 'RecuperoInCorso%' and IsOnline=0";
            cmd = new MySqlCommand(query, connection);
            cmd.ExecuteNonQuery();
            //            cmd.CommandTimeout = 60;
            dataReader = cmd.ExecuteReader();
            k = 0;

            while (dataReader.Read())
            {
                TMPIDMachinesTodelete.Add(dataReader["id"].ToString());
            }
            dataReader.Close();  

            foreach (string id in TMPIDMachinesTodelete)
            {
                query = "select id_Macchina,transferred_data from  MachinesConnectionTrace where id like '" + id+"' limit 1;";
                cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                dataReader = cmd.ExecuteReader();
                k = 0;
                if (dataReader.RecordsAffected==-1) IDMachinesTodelete.Add(id);

                dataReader.Close();

            }
            foreach (string id in IDMachinesTodelete)
            {
                DeleteMachine(id, "");
            }

            query = "select id from  Machines where mid like 'Duplicato%' and IsOnline=0";
            cmd = new MySqlCommand(query, connection);
            cmd.ExecuteNonQuery();
            //            cmd.CommandTimeout = 60;
            dataReader = cmd.ExecuteReader();
            k = 0;
TMPIDMachinesTodelete.Clear();
            while (dataReader.Read())
            {
                TMPIDMachinesTodelete.Add(dataReader["id"].ToString());
            }
            dataReader.Close(); 

            Dictionary<string, string> tmp_mid = new Dictionary<string, string>();
            
            foreach (string idtodel in TMPIDMachinesTodelete)
            {
tmp_mid.Clear();
                query = "select transferred_data from  MachinesConnectionTrace where id_Macchina = "+idtodel+" and transferred_data like '<TPK=%' limit 1 ;";
                cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                //            cmd.CommandTimeout = 60;
                dataReader = cmd.ExecuteReader();
                k = 0;
                if (!dataReader.HasRows) DeleteMachine(idtodel,"");
                while (dataReader.Read())
                {
                    string[] split_tdata=dataReader["transferred_data"].ToString().Split(",");

                    if (split_tdata[0]=="<TPK=W5")
                    {
                        if (Functions.DatabaseFunctions.IsNumeric(split_tdata[4]))
                        {
                            if (split_tdata[4]=="77770001" | split_tdata[4].StartsWith("5555555"))
                            {
                                 DeleteMachine(idtodel,"");
                            }
                            else
                            {
                                tmp_mid.Add(split_tdata[4],idtodel);
                            }
                        }
                        else
                        {
                            DeleteMachine(idtodel,"");
                        }
                    }
                    else //"<TPK=$M1" | "<TPK=$P1" | "<TPK=$M3"  | "<TPK=$M5"  | "<TPK=$I1"  | "<TPK=$I2")
                    {
                        if (Functions.DatabaseFunctions.IsNumeric(split_tdata[1]))
                        {
                            if (split_tdata[1]=="77770001" | split_tdata[1].StartsWith("5555555"))
                            {
                                 DeleteMachine(idtodel,"");
                            }
                            else
                            {
                                tmp_mid.Add(split_tdata[1],idtodel);
                            }
                        }
                        else
                        {
                            DeleteMachine(idtodel,"");
                        }
                    }
                }
                dataReader.Close(); 
        
    int Count=0;
TMPIDMachinesToUpdate.Clear();
                foreach ( string mid in tmp_mid.Keys)
                {
                    query = "select id from  Machines where mid ="+ mid + ";";
                    cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();
                    dataReader = cmd.ExecuteReader();
                    k = 0;

                    while (dataReader.Read())
                    {
                        TMPIDMachinesToUpdate.Add(dataReader["id"].ToString(),tmp_mid.Values.ElementAt(Count));
                    }
                    dataReader.Close(); 
                    Count++;

                }
            Count=0;
                foreach ( string updateid in TMPIDMachinesToUpdate.Keys)
                {
                    string deleteid=TMPIDMachinesToUpdate.Values.ElementAt(Count);
                    DeleteMachine(deleteid,updateid);
                    Count++;
                }

            }
        }

     }
}