using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SignatureTool2.Utilites.Zip
{
    internal class Com7zHelper
    {

        public bool CompressDirectory(string strInDirectoryPath, string strOutFilePath, string format)
        {
            bool result = false;
            KillProcess();
            string arguments = string.Format("a -bsp1 \"{0}\" \"{1}\\*\" -t{2} zip -r", strOutFilePath, strInDirectoryPath, format);
            if (!RunProcess(arguments))
            {
                result = false;
            }
            else
            { result = true; }

            if (File.Exists(strOutFilePath))
            { result = true; }
            return result;
        }

        private bool RunProcess(string arguments)
        {
            bool result = false;
            string exeName = "7z.exe";
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName);
            if (!File.Exists(exePath))
            { return result; }
            else
            {
                ProcessStartInfo oInfo = new ProcessStartInfo();
                oInfo.FileName = exePath;
                oInfo.Arguments = arguments;
                oInfo.UseShellExecute = false;
                oInfo.WindowStyle = ProcessWindowStyle.Hidden;
                oInfo.CreateNoWindow = true;
                oInfo.RedirectStandardError = true;
                oInfo.RedirectStandardInput = true;
                oInfo.RedirectStandardOutput = true;
                try
                {
                    Process proc = new Process();
                    proc.StartInfo = oInfo;
                    proc.OutputDataReceived += (e, e1) =>
                    {
                        if (string.IsNullOrEmpty(e1.Data))
                        {
                            return;
                        }
                        Regex regex = new Regex(@"\s+([0-9]+)%\s+[0-9]+");
                        if (regex.IsMatch(e1.Data))
                        {
                            Match m = regex.Match(e1.Data);
                            Trace.WriteLine("打包<" + m.Groups[1] + "%>完成！", "Transfer");

                        }
                        //if ()
                        //Trace.WriteLine();
                    };
                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.WaitForExit();
                     result = true;

                    Trace.WriteLine("打包完成！", "Transfer");
                }
                catch
                { result = false; }
            }
            return result;
        }

        private void KillProcess()
        {
            try
            {
                Process[] processArr = Process.GetProcessesByName("7z.exe");
                if (processArr != null && processArr.Count() > 0)
                {
                    foreach (Process item in processArr)
                    {
                        item.Kill();
                    }
                }
            }
            catch
            {
                Trace.TraceInformation("Kill 7z.exe progress is failed.");
            }
        }
    }
}
