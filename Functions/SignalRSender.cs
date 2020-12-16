using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Functions
{
    public class SignalRSender
    {
        public static HubConnection connection;
        public SignalRSender()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/MainHub")
                .WithAutomaticReconnect()
                .Build();
            try
            {
                connection.Closed += async (error) =>
                {
                    await Task.Delay(new Random().Next(0,5) * 1000);
                    await connection.StartAsync();
                };
                connection.StartAsync().Wait();
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " SignalRSender: "+e.Message);
            }
        }
        public async void sendReloadSignalForMachinesConnectionTrace(int id)
        {
            try
            {
                await connection.InvokeAsync("AskToReloadMachConnTrace", id);
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("yy/MM/dd,HH:mm:ss") + " sendReloadSignalForMachinesConnectionTrace: "+e.Message);
            }
        }
    } 
}