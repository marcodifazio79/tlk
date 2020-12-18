
//SignalRSender si rifiuta testardamente di funzionare.
//finchè non funzionerà, chiamerò programmini esterni per forzare l'aggiornaemnto
//delle pagine web tramite signalR.

using System;
using System.Diagnostics;

namespace Functions
{
    public static class ShellHelper
    {
        public static string Bash(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    //Arguments = $"-c \"{escapedArgs}\"",
                    Arguments = " -c \"dotnet /home/kdl_admin/tlk/SignalR/bin/Release/net5.0/linux-x64/publish/SignalR_.dll AskToReloadMachConnTrace 23\""
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }
}