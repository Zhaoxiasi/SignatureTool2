// SignTool.Utilites.CompilerSettingModel
#define TRACE
using System.Diagnostics;
using System.IO;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Files;

namespace SignatureTool2.Utilites
{
    public class CompilerSettingModel
    {
        public string compilerID;

        public string name;

        public string compilerPath;

        public string compilerIconSavePath;

        public string replaceIconName;

        public string commandParameter;

        public string vsBuilderPath;

        public string wpfResourcePath;

        public bool IsAvailable
        {
            get
            {
                if (!compilerPath.IsExistsFile())
                {
                    Trace.TraceWarning("编译器<" + name + ">的文件<" + compilerPath + ">不存在");
                    return false;
                }
                if (!compilerIconSavePath.IsExistsFolder())
                {
                    Trace.TraceWarning("编译器<" + name + ">的文件夹<" + compilerIconSavePath + ">不存在");
                    return false;
                }
                if (replaceIconName.IsNullOrEmpty())
                {
                    Trace.TraceWarning("编译器<" + name + ">的替换图标未设置");
                    return false;
                }
                if (commandParameter.IsNullOrEmpty())
                {
                    Trace.TraceWarning("编译器<" + name + ">的命令行未设置");
                    return false;
                }
                return true;
            }
        }

        public bool CompilerFile(string nsisPath, string savePath)
        {
            if (!FileTool.DeleteFile(savePath))
            {
                Trace.TraceWarning("删除文件<" + savePath + ">失败");
                return false;
            }
            string arguments = string.Format(commandParameter, compilerPath, nsisPath, savePath);
            Process process = new Process();
            ProcessStartInfo processStartInfo2 = (process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            process.Start();
            var ss = process.StandardOutput.ReadToEnd();
            return savePath.IsExistsFile();
        }
    }
}
