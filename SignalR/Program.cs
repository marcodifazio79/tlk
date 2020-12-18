using System;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace SignalR_
{
    class Program
    {
        static void Main(string[] args)
        {
            //args[0] = target Page To Update
            //args[1] = target ID
            int id = Convert.ToInt32(args[1]);
            reloadThePage(args[0], id);
        }
        
        

        public static async void reloadThePage(string s, int id)
        {
            try
                {
                    HubConnection connection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5000/MainHub")
                    .Build();
                
                    connection.StartAsync().Wait();

                    switch (s)
                    {
                        case "AskToReloadMachConnTrace":
                            await connection.InvokeAsync("AskToReloadMachConnTrace", id);
                            break;

                        case "AskToReloadMachCommandTable":
                            await connection.InvokeAsync("AskToReloadMachCommandTable", id);
                            break;

                        case "AskToReloadMachinesTable":
                            await connection.InvokeAsync("AskToReloadMachinesTable", id);
                            break;
                            
                        default:
                            break;
                    }
                    connection.StopAsync().Wait();


                }
                catch (System.Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " "+e.Message);

                    //throw;
                }
                
        }
    }
}
