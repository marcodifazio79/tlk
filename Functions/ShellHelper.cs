//SignalRSender si rifiuta testardamente di funzionare.
//finchè non funzionerà, chiamerò programmini esterni per forzare l'aggiornaemnto
//delle pagine web tramite signalR.

using System;
using System.Diagnostics;

namespace Functions
{
    public static class ShellHelper
    {
        public static void Bash(this string cmd)
        {
            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments =  " /home/kdl_admin/tlk/SignalR/bin/Release/net5.0/linux-x64/publish/SignalR_.dll " + cmd 
                });
            process.WaitForExit();
        }
    }
}