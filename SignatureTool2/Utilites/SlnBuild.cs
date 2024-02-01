using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SignatureTool2.Utilites
{
    internal static class SlnBuild
    {
        internal static void Build(string buildArguments,string WorkingDirectory)
        {
            ModifyReleaseData();
            ProcessStartInfo oInfo = new ProcessStartInfo();
            oInfo.FileName = @"cmd.exe";
            oInfo.WorkingDirectory = WorkingDirectory;
            oInfo.Arguments = buildArguments;
            oInfo.UseShellExecute = false;
            oInfo.CreateNoWindow = true;
            oInfo.RedirectStandardOutput = true;
            oInfo.RedirectStandardError = false;
            try
            {
                Process proc = new Process();
                proc.StartInfo = oInfo;
                proc.OutputDataReceived += (e, e1) =>
                {
                    //1>  SignatureTool2 ->
                    if (string.IsNullOrEmpty(e1.Data))
                    {
                        return;
                    }
                    Regex regex = new Regex(@"\d>\s+([a-zA-Z0-9\.]+)\s->\s.+");
                    if (regex.IsMatch(e1.Data))
                    {
                        Match m = regex.Match(e1.Data);
                        Trace.TraceInformation("生成<" + m.Groups[1] + ">成功！");

                    }
                    Console.WriteLine(e1.Data);
                };
                proc.Start();
                proc.BeginOutputReadLine();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {

            }
        }


        private static void ModifyReleaseData()
        {

        }
    }
}
