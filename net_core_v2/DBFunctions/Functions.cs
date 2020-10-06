using System;

namespace Functions
{
    public class DatabaseFunction
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
}
