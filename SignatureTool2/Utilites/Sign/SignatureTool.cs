// SignTool.Utilites.Sign.SignatureTool
#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Threading;
using System.Windows.Forms;
using SignatureTool2.Utilites;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Sign;
namespace SignatureTool2.Utilites.Sign
{

    public class SignatureTool
    {
        public static bool _isInputedPasscode = true;

        public static object _lockInputPasscode = new object();

        private static List<string> _supportSignFile = new List<string> { ".dll", ".exe", ".cat" ,".msi"};

        private string _signCommandLine1 = "\"{0}\" sign /sha1 {1} /tr http://timestamp.sectigo.com /td sha256 /fd sha256 /a \"{2}\"";
        private string _signCommandLine = "\"{0}\" sign /sha1 {1} /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a \"{2}\"";

        private string _cmdLine = "cmd /c \" {0} \"";

        private bool _isSkip;

        public static bool IsAllSkip { get; set; } = false;


        public static List<string> SupportSignFile => _supportSignFile;

        public bool SignatureFiles(string sName, List<SignModel> files)
        {
            Trace.TraceInformation($"signature <{sName}> total {files.Count} file(s)");
            SemaphoreTool semaphoreTool = new SemaphoreTool(files.Count, 1);
            int totalCount = files.Count;
            int currentCount = 0;
            foreach (SignModel file in files)
            {
                if (IsAllSkip)
                {
                    break;
                }
                if (_isSkip || RunControl.Token.IsCancellationRequested)
                {
                    semaphoreTool.SkipThread();
                    break;
                }
                if (GetIsSigned(file.FilePath))
                {
                    Interlocked.Increment(ref currentCount);
                    Trace.TraceInformation($"<{sName}>:Has being Signed:{file.FilePath}");
                    continue;
                }
                semaphoreTool.InvokeByTask(delegate (object par)
                {
                    SignatureFile((SignModel)par);
                    Interlocked.Increment(ref currentCount);
                    Trace.TraceInformation($"<{sName}>: {currentCount}/{totalCount}");
                }, file);
            }
            semaphoreTool.WaitAllFinish();
            int count = files.Count;
            int count2 = files.FindAll((SignModel p) => p.SignResult == SignResultEnum.Success).Count;
            var failedFiles = files.FindAll((SignModel p) => p.SignResult == SignResultEnum.Failed);
            int count3 = failedFiles.Count;
            int count4 = files.FindAll((SignModel p) => p.SignResult == SignResultEnum.NotSupport).Count;
            Trace.TraceInformation($"signature <{sName}> all file(s) completed, total<{count}>-success<{count2}>-failed<{count3}>-not support<{count4}>");
            if (count3 > 0)
            {
                Trace.TraceInformation($"Failed files:\r\n" + string.Join("\r\n", failedFiles.Select(p => p.FilePath)));
            }
            return count == count2;
        }

        public int SignatureFile(SignModel model, int triedCount = 0)
        {
            lock (_lockInputPasscode)
            {
                while (!_isInputedPasscode)
                {
                    if (RunControl.Token.IsCancellationRequested || IsAllSkip)
                    {
                        return -4;
                    }
                    Thread.Sleep(200);
                }
                _isInputedPasscode = false;
            }
            return Signature(model, triedCount);
        }

        private int Signature(SignModel model, int triedCount = 0)
        {
            if (IsAllSkip)
            {
                return -4;
            }
            if (RunControl.Token.IsCancellationRequested)
            {
                model.SignResult = SignResultEnum.NotSet;
                return -4;
            }
            int flag = 0;
            if (!IsSupportSign(model.FilePath))
            {
                model.SignResult = SignResultEnum.NotSupport;
                return -1;
            }
            string text = "";
            text = AppDomain.CurrentDomain.BaseDirectory.Combine("signtool.exe");
            if (!text.IsExistsFile())
            {
                Trace.TraceInformation("<\"signtool.exe\"> is not exists");
                return -8;
            }
            string argument = StringExtension.Format(_cmdLine, StringExtension.Format(_signCommandLine, text, model.sha1, model.FilePath));
             var output= Signature(argument);
            string[] array  = output.Split(new string[1] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length > 1)
            {
                if (array[1].Contains("Successfully signed") && array[1].Contains(model.FilePath.GetFileName()))
                {
                    flag = 1;
                }
            }
            else
            {
                if (array[0].Contains("Invalid SHA1 hash format") && array[0].Contains(model.sha1))
                {
                    flag = -2;
                }else if(array[0].Contains("No certificates were found that met all the given criteria."))
                {
                    flag = -2;
                }
            }
            if (flag != 1)
            {
                if(flag == 0)
                {
                    if (GetIsSigned(model.FilePath))
                    {
                        model.SignResult = SignResultEnum.Success;
                        Trace.TraceInformation("signature <" + model.FilePath.GetFileName() + "> success");
                    }
                    else
                    {

                        Thread.Sleep(300);
                        model.SignResult = SignResultEnum.Failed;
                    }
                }
                else
                {
                    Thread.Sleep(300);
                    model.SignResult = SignResultEnum.Failed;
                    if (flag == -2)
                    {
                        var company = CompanyTool.Instance.GetCompanyBySha1(model.sha1);
                        Trace.TraceInformation("signature <" + model.FilePath.GetFileName() + $"> Failed!δ��⵽<{company.Name}>��key��");
                    }
                }
            }
            else
            {
                model.SignResult = SignResultEnum.Success;
                Trace.TraceInformation("signature <" + model.FilePath.GetFileName() + "> success");
            }
            _isInputedPasscode = true;
            return flag;
        }

        public static SignModel CreateSignModel(string file, string companyID)
        {
            return CreateSignModel(new List<string> { file }, companyID).FirstOrDefault();
        }

        public static List<SignModel> CreateSignModel(List<string> files, string companyID)
        {
            List<SignModel> li = new List<SignModel>();
           var company =  CompanyTool.Instance.GetCompanyByID(companyID);
            files?.ForEach(delegate (string p)
            {
                if (GetIsSigned(p))
                    return;
                SignModel item = new SignModel
                {
                    FilePath = p,
                    sha1 = company.Sha1
                };
                li.Add(item);
            });
            return li;
        }

        internal void StopThread()
        {
            _isSkip = true;
        }

        private bool IsSupportSign(string filePath)
        {
            bool num = _supportSignFile.Contains(filePath.GetExtension());
            if (!num)
            {
                Trace.TraceWarning("<" + filePath.GetFileName() + "> is not support to signature");
            }
            if (!filePath.IsExistsFile())
            {
                Trace.TraceWarning("<" + filePath + "> is not exists");
            }
            return num;
        }

        private string Signature(string argument)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.CreateNoWindow = true;
                processStartInfo.UseShellExecute = false;
                processStartInfo.Arguments = argument;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.FileName = "cmd.exe";
                Process process = new Process();
                process.StartInfo = processStartInfo;
                CancellationTokenSource cts = new CancellationTokenSource();
                System.Threading.Tasks.Task.Run(() => CheckInputWindow(cts.Token));
                process.Start();
                RunControl.Track(process);
                // 两个输出流同时异步读取，避免单个流先读导致缓冲区写满的死锁
                var errorTask = process.StandardError.ReadToEndAsync();
                var outputTask = process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                RunControl.Untrack(process);
                cts.Cancel();
                string output = outputTask.Result;
                string error = errorTask.Result;
                if (RunControl.Token.IsCancellationRequested)
                {
                    Trace.TraceWarning("签名<" + process.Id + ">已被停止");
                }
                return error + output;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>检查文件是否已签名且带时间戳。</summary>
        public static bool GetIsSigned(string filePath)
        {
            string text = "";
            text = AppDomain.CurrentDomain.BaseDirectory.Combine("signtool.exe");
            if (!text.IsExistsFile())
            {
                Trace.TraceInformation("<\"signtool.exe\"> is not exists");
                return false;
            }
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c \"{text}\" verify /v \"{filePath}\"";
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                // 先异步读输出再等退出：先 WaitForExit 会在输出缓冲区写满时死锁
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(30000))
                {
                    RunControl.KillTree(process);
                    Trace.TraceWarning($"verify<{filePath.GetFileName()}> 30秒未返回，已终止");
                    return false;
                }
                string output = outputTask.Result + errorTask.Result;
                return output.Contains("Signing Certificate Chain:") && !output.Contains("File is not timestamped.");
            }
        }

        public static bool CheckKey(string company)
        {
            if (company.ToLower().Equals("gemoo"))
            {

                App.Current.Dispatcher.Invoke(() => Clipboard.SetDataObject("Gemoo2022#"));
                return true;

            }
            else
            {
                App.Current.Dispatcher.Invoke(() => Clipboard.SetDataObject("cIn02x0WqfoJ{172"));
                return true;

            }
            return false;
        }
        private async System.Threading.Tasks.Task CheckInputWindow(CancellationToken token)
        {
            await System.Threading.Tasks.Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await System.Threading.Tasks.Task.Delay(500);
                    if (token.IsCancellationRequested) break;
                    var window = Utilities.WindowForm.NewWindow("�豸��¼");
                    if (window == null) window = Utilities.WindowForm.NewWindow("Token Logon");
                    if (window == null)
                    {
                        continue;
                    }
                    var process = Process.GetProcessById(window.ThreadID);
                    if (process.ProcessName.StartsWith("signtool", StringComparison.OrdinalIgnoreCase))
                    {
                        IntPtr handle;
                        handle = window.FindChild(0x3E9);
                        var data = window.GetControlContent(handle);
                        handle = window.FindChild(0x3EA);
                        string password = "";
                        if (data.StartsWith("imobie", StringComparison.OrdinalIgnoreCase))
                            password = "cIn02x0WqfoJ{172";
                        else if (data.StartsWith("gemoo", StringComparison.OrdinalIgnoreCase))
                            password = "";
                        else if (data.StartsWith("keeprix", StringComparison.OrdinalIgnoreCase))
                            password = "Keeprix2025#";
                        window.SendMessage(handle, Utilities.WindowMessageType.WM_SetText, password);
                        handle = window.FindChild(0x1);
                        window.SendMessage(handle, Utilities.WindowMessageType.WM_LeftButtonDown);
                        window.SendMessage(handle, Utilities.WindowMessageType.WM_LeftButtonUp);
                        break;
                    }
                }
            }, token);
        }
    }
}