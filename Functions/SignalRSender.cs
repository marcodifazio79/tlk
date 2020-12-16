using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Functions
{
    public class SignalRSender
    {
        public static HubConnection connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:5001/MainHub")
            .WithAutomaticReconnect()
            .Build();
        public SignalRSender()
        {
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

            }
        }
        public async void sendReloadSignalForMachinesConnectionTrace(int id)
        {
            await connection.InvokeAsync("AskToReloadMachConnTrace", id);
        }
    } 
}