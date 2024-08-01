// SignTool.Utilites.ProtecterSettingModel
#define TRACE
using System;
using System.Diagnostics;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Files;

namespace SignatureTool2.Utilites
{
    public class ProtecterSettingModel
    {
        public string protecterID;

        public string name;

        public string protecterPath;
        public string commandParameter = " -project {0}";


        public bool IsAvailable
        {
            get
            {
                if (!protecterPath.IsExistsFile())
                {
                    Trace.TraceWarning("编译器<" + name + ">的文件<" + protecterPath + ">不存在");
                    return false;
                }
                return true;
            }
        }

        public bool ProtecterFile(string nsisPath)
        {
            string arguments = string.Format(commandParameter, nsisPath);
            Process process = new Process();
            ProcessStartInfo processStartInfo2 = (process.StartInfo = new ProcessStartInfo
            {
                FileName = protecterPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            bool Success = true;
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null && !e.Data.IsNullOrEmpty())
                {
                    if (Success)
                    {
                        if( !e.Data.Contains(" Successfully Protected!"))
                        {
                            Success = false;
                        }
                    }
                    Trace.TraceInformation(e.Data);
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            var outPut = "";// process.StandardOutput.ReadToEnd().ToLower();
            return Success;
        }

    } 
}
