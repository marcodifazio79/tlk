using System;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace Functions
{
    class SignalRSender
    {
        public static async void AskToReloadMachConnTrace(int id)
        {
            try
                {
                HubConnection connection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5000/MainHub")
                    .WithAutomaticReconnect()
                    .Build();

                
                    connection.StartAsync().Wait();
                    await connection.InvokeAsync("AskToReloadMachConnTrace", id);
                    //await connection.InvokeAsync("AskToReloadMachCommandTable", 24);
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " "+e.Message);

                    //throw;
                }
        }
    }
}
