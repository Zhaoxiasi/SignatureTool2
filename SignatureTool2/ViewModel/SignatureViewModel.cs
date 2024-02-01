// SignTool.ViewModel.SignatureViewModel
#define TRACE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using SignatureTool2;
using SignatureTool2.Utilites;
using SignatureTool2.Utilites.DialogWindow;
using SignatureTool2.Utilites.Directorys;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Files;
using SignatureTool2.Utilites.Log;
using SignatureTool2.Utilites.Zip;
using SignatureTool2.Utilites.Sign;
using SignatureTool2.ViewModel;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using System.Management;

namespace SignatureTool2.ViewModel
{

    public class SignatureViewModel : SignBaseViewModel
    {
        private List<string> _errorList = new List<string>();

        private bool _isSkip;

        private ConcurrentQueue<SignatureTool> _signList;

        private SignatureModel selectedItem;

        private string errorText;

        private bool isSigning;

        private bool isEnabled = true;

        private Visibility loadingVisibility = Visibility.Collapsed;

        private int clickCount;

        public SignatureModel SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                SetProperty(ref selectedItem, value, "SelectedItem");
            }
        }

        public string ErrorText
        {
            get
            {
                return errorText;
            }
            set
            {
                SetProperty(ref errorText, value, "ErrorText");
            }
        }

        public bool IsSigning
        {
            get
            {
                return isSigning;
            }
            set
            {
                SetProperty(ref isSigning, value, "IsSigning");
            }
        }

        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
            set
            {
                SetProperty(ref isEnabled, value, "IsEnabled");
            }
        }

        public Visibility LoadingVisibility
        {
            get
            {
                return loadingVisibility;
            }
            set
            {
                SetProperty(ref loadingVisibility, value, "LoadingVisibility");
            }
        }

        public ObservableCollection<SignatureModel> FileList { get; }

        public ICommand AddCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand SaveConfigCommand { get; set; }

        public ICommand SelectSourcePathCommand { get; set; }

        public ICommand SelectTargetPathCommand { get; set; }
        

        public SignatureViewModel()
        {
            _signList = new ConcurrentQueue<SignatureTool>();
            AddCommand = new DelegateCommand(OnAddCommand);
            DeleteCommand = new DelegateCommand(OnDeleteCommand);
            SaveConfigCommand = new DelegateCommand(OnSaveConfigCommand);
            SelectSourcePathCommand = new DelegateCommand(OnSelectSourcePathCommand);
            SelectTargetPathCommand = new DelegateCommand(OnSelectTargetPathCommand);
            SignCommand = new DelegateCommand(OnSignCommand);
            StopCommand = new DelegateCommand(OnStopCommand);
            FileList = new ObservableCollection<SignatureModel>();
            ReadConfig();
            LogTool.Instance.WriteLogEventAdvance += Instance_WriteLogEventAdvance;
        }

        private void Instance_WriteLogEventAdvance(TraceEventType arg1, string msg)
        {
            if(arg1 == TraceEventType.Transfer)
            {
                _errorList.RemoveAt(0);
            }
            _errorList.Insert(0, msg);

            StringBuilder stringBuilder = new StringBuilder();
            foreach (string error in _errorList)
            {
                stringBuilder.AppendLine(error);
            }
            ErrorText = stringBuilder.ToString();
        }

        private void Instance_WriteLogEvent(string msg)
        {
        }

        private void OnStopCommand()
        {
            if (TipsTool.OpenTipsWindow("确定要取消本次签名？", "取消签名", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            _isSkip = true;
            foreach (SignatureTool sign in _signList)
            {
                sign.StopThread();
            }
        }

        private void OnSignCommand()
        {
            List<SignatureModel> list = FileList.ToList().FindAll((SignatureModel p) => p.IsSelected && p.IsAvailable);
            if (list.Count != 0 && TipsTool.OpenTipsWindow("注意：\r\nU盘你插了吗？\r\n首次使用时需要输入密码，若没有提示输入，则本次签名不合格！！！\r\n是否继续？(密码已复制)", "签名警告", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                _errorList.Clear();
                Package(list);
            }
        }

        private bool CheckKey(string company)
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
                    Clipboard.SetDataObject("cIn02x0WqfoJ{172");
                    return true;
                }
            }
            return false;
        }

        private void OnSelectTargetPathCommand()
        {
            if (CheckHasData())
            {
                string text = ChooseDialogTool.OpenSelectFolderDialog();
                if (!text.IsNullOrEmpty())
                {
                    SelectedItem.TargetPath = text;
                }
            }
        }

        private void OnSelectSourcePathCommand()
        {
            if (CheckHasData())
            {
                string text = ChooseDialogTool.OpenSelectFolderDialog();
                if (!text.IsNullOrEmpty())
                {
                    SelectedItem.SourcePath = text;
                }
            }
        }

        private void OnSaveConfigCommand()
        {

            SaveConfig();
        }

        private void OnDeleteCommand()
        {
            List<SignatureModel> list = FileList.ToList().FindAll((SignatureModel p) => p.IsSelected);
            if (list.Count == 0)
            {
                return;
            }
            StringBuilder sb = new StringBuilder();
            list.ForEach(delegate (SignatureModel p)
            {
                sb.AppendLine("<" + p.SoftwareName + ">");
            });
            if (TipsTool.OpenTipsWindow($"确定删除\r\n{sb}\r\n共{list.Count}项？", "删除", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                list.ForEach(delegate (SignatureModel p)
                {
                    FileList.Remove(p);
                });
                SaveConfig();
            }
        }

        private void OnAddCommand()
        {
            SignatureModel signatureModel = new SignatureModel();
            signatureModel.SoftwareName = "新增项";
            signatureModel.IsSelected = true;
            FileList.ToList().ForEach(delegate (SignatureModel p)
            {
                p.IsSelected = false;
            });
            FileList.Insert(0, signatureModel);
        }

        private void ReadConfig()
        {
            List<SignatureModel> li = new List<SignatureModel>();
            ConfigTool.Instance.GetValveOfRoot<List<object>>(CRootKey.Package_7z)?.ForEach(delegate (object p)
            {
                Dictionary<string, object> dictionary = p as Dictionary<string, object>;
                if (dictionary != null)
                {
                    SignatureModel item = new SignatureModel
                    {
                        SoftwareName = dictionary.GetValue<string>("SoftwareName"),
                        ExcutableName = dictionary.GetValue<string>("ExcutableName"),
                        Company = dictionary.GetValue<string>("Company"),
                        ProductInfoPath = dictionary.GetValue<string>("ProductInfoPath"),
                        SourcePath = dictionary.GetValue<string>("SourcePath"),
                        BiuldArgument = dictionary.GetValue<string>("BiuldArgument"),
                        LastSignTime = dictionary.GetValue<DateTime>("LastSignTime"),
                        LastBiuldTime = dictionary.GetValue<DateTime>("LastBiuldTime"),
                        TargetPath = dictionary.GetValue<string>("TargetPath")
                    };
                    li.Add(item);
                }
            });
            if (li.Count > 0)
            {
                li.First().IsSelected = true;
            }
            IOrderedEnumerable<SignatureModel> items = li.OrderBy((SignatureModel p) => p.SoftwareName);
            FileList.AddRange(items);
        }

        private void SaveConfig()
        {
            if (FileList.Count == 0)
            {
                OnAddCommand();
                return;
            }
            List<object> list = new List<object>();
            foreach (SignatureModel file in FileList)
            {
                if (!file.IsAvailable)
                {
                    Trace.TraceWarning("<" + file.SoftwareName + ">'s is not available, please check the selecte path");
                    continue;
                }
                file.IsNew = false;
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("SoftwareName", file.SoftwareName);
                dictionary.Add("SourcePath", file.SourcePath);
                dictionary.Add("Company", file.Company??"");
                dictionary.Add("LastSignTime", file.LastSignTime);
                dictionary.Add("LastBiuldTime", file.LastBiuldTime);
                dictionary.Add("BiuldArgument", file.BiuldArgument);
                dictionary.Add("ExcutableName", file.ExcutableName);
                dictionary.Add("ProductInfoPath", file.ProductInfoPath);
                dictionary.Add("TargetPath", file.TargetPath);
                list.Add(dictionary);
            }
            if (list.Count > 0)
            {
                ConfigTool.Instance.BackupConfig();
                ConfigTool.Instance.SetValueOfRoot(CRootKey.Package_7z, list);
                Trace.TraceInformation("保存配置完成！");
            }
        }

        private void Package(List<SignatureModel> li)
        {
            if (++clickCount > 1)
            {
                return;
            }
            _isSkip = false;
            IsSigning = true;
            IsEnabled = false;
            LoadingVisibility = Visibility.Visible;
            new Thread((ThreadStart)delegate
            {
                int successCount = 0;
                SemaphoreTool semaphoreTool = new SemaphoreTool(li.Count, 1);
                foreach (SignatureModel item in li)
                {
                    if (!CheckKey(item.Company))
                    {
                        Trace.TraceWarning($"无法完成{item.SoftwareName}项，因为{item.Company}的Key没有插入！");
                        continue;

                    }
                    Trace.TraceWarning($"{item.SoftwareName}项Key密码已复制！");
                    semaphoreTool.InvokeByThread(delegate (object par)
                    {
                        if (CopyFile((SignatureModel)par))
                        {
                            successCount++;
                        }
                        ((SignatureModel)par).SignModelList = null;
                        ((SignatureModel)par).LastSignTime = DateTime.Now;
                    }, "Sign_" + item.SoftwareName, item);
                }
                semaphoreTool.WaitAllFinish();
                SaveConfig();
                Trace.TraceInformation($"所有签名完成，总共{li.Count}项，成功{successCount}项，失败{li.Count - successCount}项！");
                var rl=MessageBox.Show("签名完成！是否打包？[Yes:7z;No:Zip;Cancel:Cancel]", "向导", MessageBoxButton.YesNoCancel);
                Com7zHelper com7 = new Com7zHelper();
                if (rl == MessageBoxResult.Yes)
                {
                    
                    foreach (SignatureModel item in li)
                    {
                        string targeFileName = item.TargetPath.GetFileName().Replace("custom", $"{DateTime.Now.Date.ToString("yyyyMMdd")}.7z");
                        string filepath = Path.Combine(Path.GetDirectoryName(item.TargetPath), targeFileName);
                        if (filepath.IsExistsFile())
                        {
                            FileTool.DeleteFile(filepath);
                        }
                        com7.CompressDirectory(item.TargetPath, filepath, "7z");
                        Trace.TraceInformation($"打包<{targeFileName}>完成！");
                        Process.Start("explorer.exe", $"/select,\"{filepath}\"");
                    }
                }
                else if(rl == MessageBoxResult.No)
                {
                    foreach (SignatureModel item in li)
                    {
                        string targeFileName = item.TargetPath.GetFileName().Replace("custom", $"{DateTime.Now.Date.ToString("yyyyMMdd")}.zip");
                        string filepath = Path.Combine(Path.GetDirectoryName(item.TargetPath), targeFileName);
                        if (filepath.IsExistsFile())
                        {
                            FileTool.DeleteFile(filepath);
                        }
                        com7.CompressDirectory(item.TargetPath, filepath, "zip");
                        Trace.TraceInformation($"打包<{targeFileName}>完成！");
                        Process.Start("explorer.exe", $"/select,\"{filepath}\"");
                    }
                }
                LoadingVisibility = Visibility.Collapsed;
                clickCount = 0;
                IsSigning = false;
                IsEnabled = true;
            }).Start();
        }

        private bool CopyFile(SignatureModel model)
        {
            bool result = false;
            if (model.checkNeedBuild())
            {

                var compiler = CompilerTool.Instance.GetCompilerByID(null);
                SlnBuild.Build(model.BiuldArgument, Path.GetDirectoryName(compiler.vsBuilderPath));
                model.LastBiuldTime = DateTime.Now;
            }
            FindNeedCopyFile(model);
            if (CopyFileToTarget(model))
            {
                
                if (SignatureFile(model))
                {
                    result = true;
                    Trace.TraceInformation("签名<" + model.SoftwareName + ">成功！");
                }
                else
                {
                    Trace.TraceWarning("签名<" + model.SoftwareName + ">出现失败，未能完成签名！");
                    //retry
                    var rrl=MessageBox.Show("部分签名没成功，需要重新签名", "注意！", MessageBoxButton.OKCancel);
                    if(rrl == MessageBoxResult.OK)
                    {
                        result= Retry(model);
                    }
                    else
                    {
                    }
                }
            }
            else
            {
                Trace.TraceWarning("拷贝<" + model.SoftwareName + ">出现失败，未能完成签名！");
            }
            return result;
        }

        private bool Retry(SignatureModel model)
        {

            SignatureTool signatureTool = new SignatureTool();
            _signList.Enqueue(signatureTool);
            return signatureTool.SignatureFiles(model.SoftwareName, model.SignModelList.FindAll(p=>p.SignResult == SignResultEnum.Failed));
        }

        private void FindNeedCopyFile(SignatureModel model)
        {
            model.CopyList.Clear();
            List<string> allFiles = FolderTool.GetAllFiles(model.SourcePath);
            List<CopyFileModel> list = new List<CopyFileModel>();
            DateTime dateTime = DateTime.Now.Date;
            List<string> supportSignFile = SignatureTool.SupportSignFile;
            foreach (string item in allFiles)
            {
                if (item.Contains("GemooRecorder.exe"))
                {
                    var 搜索 = item.GetExtension().ToLower();
                }
                DateTime lastWriteTime = new FileInfo(item).LastWriteTime;
                if (model.LastSignTime == DateTime.MinValue)
                    model.LastSignTime = DateTime.Now.Date;
                if (!(lastWriteTime < model.LastSignTime) && supportSignFile.Contains(item.GetExtension().ToLower()))
                {
                    CopyFileModel copyFileModel = new CopyFileModel();
                    copyFileModel.SourcePath = item;
                    copyFileModel.CreateTime = lastWriteTime;
                    if (dateTime < lastWriteTime)
                    {
                        dateTime = lastWriteTime;
                    }
                    list.Add(copyFileModel);
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].SourcePath.GetExtension().ToLower() == ".pdb")
                {
                    list.RemoveAt(i);
                    i--;
                    continue;
                }
                string sourcePath = list[i].SourcePath;
                CopyFileModel copyFileModel2 = new CopyFileModel();
                copyFileModel2.SourcePath = sourcePath;
                copyFileModel2.RelativePath = sourcePath.Replace(model.SourcePath + "\\", "");
                copyFileModel2.TargetPath = model.TargetPath.Combine(copyFileModel2.RelativePath);
                model.CopyList.Add(copyFileModel2);
            }
        }

        private bool CopyFileToTarget(SignatureModel model)
        {
            SemaphoreTool semaphoreTool = new SemaphoreTool(model.CopyList.Count, 10);
            foreach (CopyFileModel copy in model.CopyList)
            {
                semaphoreTool.InvokeByTask(delegate (object par)
                {
                    CopyFileModel copyFileModel = (CopyFileModel)par;
                    FileTool.CopyFile(copyFileModel.SourcePath, copyFileModel.TargetPath);
                }, copy);
            }
            semaphoreTool.WaitAllFinish();
            return !semaphoreTool.HasFailed;
        }

        private bool SignatureFile(SignatureModel model)
        {
            if (_isSkip)
            {
                return false;
            }
            List<string> allFiles = new List<string>();
            model.CopyList.ForEach(delegate (CopyFileModel p)
            {
                allFiles.Add(p.TargetPath);
            });
            SignatureTool signatureTool = new SignatureTool();
            _signList.Enqueue(signatureTool);
            return signatureTool.SignatureFiles(model.SoftwareName, model.CreateModels(allFiles));
        }

        private bool CheckHasData()
        {
            if (FileList.Count == 0)
            {
                OnAddCommand();
                return true;
            }
            if (SelectedItem == null)
            {
                TipsTool.OpenTipsWindow("请选中一个后再操作", "未选中", MessageBoxButton.OK, MessageBoxImage.Hand);
                return false;
            }
            return true;
        }
    } 
}
