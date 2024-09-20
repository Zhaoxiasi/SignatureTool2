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

        private string _signCommandLine = "\"{0}\" sign /tr http://timestamp.sectigo.com /td sha256 /fd sha256 /a \"{1}\"";

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
                if (_isSkip)
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
            int count3 = files.FindAll((SignModel p) => p.SignResult == SignResultEnum.Failed).Count;
            int count4 = files.FindAll((SignModel p) => p.SignResult == SignResultEnum.NotSupport).Count;
            Trace.TraceInformation($"signature <{sName}> all file(s) completed, total<{count}>-success<{count2}>-failed<{count3}>-not support<{count4}>");
            return count == count2;
        }

        public bool SignatureFile(SignModel model, int triedCount = 0)
        {
            lock (_lockInputPasscode)
            {
                while (!_isInputedPasscode)
                {
                    Thread.Sleep(500);
                }
                _isInputedPasscode = false;
            }
            return Signature(model, triedCount);
        }

        private bool Signature(SignModel model, int triedCount = 0)
        {
            if (IsAllSkip)
            {
                return false;
            }
            bool flag = false;
            if (!IsSupportSign(model.FilePath))
            {
                model.SignResult = SignResultEnum.NotSupport;
                return false;
            }
            string text = "";
            text = AppDomain.CurrentDomain.BaseDirectory.Combine("signtool.exe");
            if (!text.IsExistsFile())
            {
                Trace.TraceInformation("<\"signtool.exe\"> is not exists");
                return false;
            }
            string argument = StringExtension.Format(_cmdLine, StringExtension.Format(_signCommandLine, text, model.FilePath));
            string[] array = Signature(argument).Split(new string[1] { "\r\n" }, StringSplitOptions.None);
            if (array.Length > 1)
            {
                flag = array[1].Contains("Successfully signed") && array[1].Contains(model.FilePath.GetFileName());
            }
            if (!flag)
            {
                Thread.Sleep(300);
                model.SignResult = SignResultEnum.Failed;
            }
            else
            {
                model.SignResult = SignResultEnum.Success;
                Trace.TraceInformation("signature <" + model.FilePath.GetFileName() + "> success");
            }
            _isInputedPasscode = true;
            return flag;
        }

        public static SignModel CreateSignModel(string file)
        {
            return CreateSignModel(new List<string> { file }).FirstOrDefault();
        }

        public static List<SignModel> CreateSignModel(List<string> files)
        {
            List<SignModel> li = new List<SignModel>();
            files?.ForEach(delegate (string p)
            {
                if (GetIsSigned(p))
                    return;
                SignModel item = new SignModel
                {
                    FilePath = p
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
            string text = "";
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.CreateNoWindow = true;
                processStartInfo.UseShellExecute = false;
                processStartInfo.Arguments = argument;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.FileName = "cmd.exe";
                Process process = new Process();
                process.StartInfo = processStartInfo;
                process.Start();
                return process.StandardOutput.ReadToEnd();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static bool GetIsSigned(string filePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            string text = "";
            text = AppDomain.CurrentDomain.BaseDirectory.Combine("signtool.exe");
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c {text} verify  /v " + filePath;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Start();
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();

            return output.Contains("Signing Certificate Chain:");
        }

        public static bool CheckKey(string company)
        {
            if (company.ToLower().Equals("gemoo"))
            {
                if (SafeNetTool.GemooIn)
                {
                    App.Current.Dispatcher.Invoke(() => Clipboard.SetDataObject("Gemoo2022#"));
                    return true;
                }
            }
            else
            {
                if (SafeNetTool.iMobieIn)
                {
                    App.Current.Dispatcher.Invoke(() => Clipboard.SetDataObject("cIn02x0WqfoJ{172"));
                    return true;
                }
            }
            return false;
        }
    } 
}
