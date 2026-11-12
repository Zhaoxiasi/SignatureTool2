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

        public bool ProtecterFile(string projectPath)
        {
            if (!IsAvailable)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(projectPath) || !projectPath.IsExistsFile())
            {
                Trace.TraceError($"混淆工程文件不存在：{projectPath}");
                return false;
            }

            try
            {
                string formattedPath = projectPath.Contains(" ") && !commandParameter.Contains("\"{0}\"")
                    ? $"\"{projectPath}\""
                    : projectPath;
                string arguments = string.Format(commandParameter, formattedPath);
                using (Process process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = protecterPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    // 注意：为什么不能在 OutputDataReceived 回调中单纯通过匹配特定字符串（如 "Successfully Protected!"）来判断保护是否成功：
                    // 1. 多行输出与时序干扰：控制台输出通常包含多行（版本声明、版权、编译进度、空行等），按单行判断极易因非目标文本行产生误判；
                    // 2. 部分成功误判（致命风险）：一个项目中往往包含多个 DLL/EXE，工具可能对某个文件输出了成功信息，但其它文件因依赖缺失或错误跳过，简单包含判断无法确认是否“全部程序集均保护成功”；
                    // 3. 动态工具兼容性：保护工具是动态配置的（可能为 .NET Reactor、ConfuserEx、自研混淆器等），不同工具甚至同一工具不同版本的控制台输出格式与关键字均不相同，硬编码字符串匹配不可靠；
                    // 4. 标准可靠做法：此处仅记录日志并依据进程退出码（ExitCode == 0）进行初步判断，业务层再结合 *_Secure 产物目录校验及人工核对确认。
                    process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        if (e.Data != null && !e.Data.IsNullOrEmpty())
                        {
                            Trace.TraceInformation(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        if (e.Data != null && !e.Data.IsNullOrEmpty())
                        {
                            Trace.TraceError(e.Data);
                        }
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("执行保护工具异常：" + ex.Message);
                return false;
            }
        }

    } 
}
