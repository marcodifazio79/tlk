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
                    await connection.StopAsync();
                    connection.DisposeAsync();
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " "+e.Message);
                }
        }

        public static async void AskToReloadMachCommandTable(int id)
        {
            try
                {
                HubConnection connection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5000/MainHub")
                    .WithAutomaticReconnect()
                    .Build();
                
                    connection.StartAsync().Wait();
                    await connection.InvokeAsync("AskToReloadMachCommandTable", id);
                    await connection.StopAsync();
                    connection.DisposeAsync();
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " "+e.Message);
                }
        }

        public static async void AskToReloadMachinesTable()
        {
            try
                {
                HubConnection connection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5000/MainHub")
                    .WithAutomaticReconnect()
                    .Build();
                
                    connection.StartAsync().Wait();
                    await connection.InvokeAsync("AskToReloadMachinesTable");
                    await connection.StopAsync();
                    connection.DisposeAsync();
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " "+e.Message);
                }
        }
        public static async void AskToReloadCashTransactionTable(int id)
        {
            try
                {
                HubConnection connection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5000/MainHub")
                    .WithAutomaticReconnect()
                    .Build();
                
                    connection.StartAsync().Wait();
                    await connection.InvokeAsync("AskToReloadCashTransactionTable", id);
                    await connection.StopAsync();
                    connection.DisposeAsync();
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " "+e.Message);
                }
        }

    }
}
