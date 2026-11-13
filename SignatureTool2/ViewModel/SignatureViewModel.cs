// SignTool.ViewModel.SignatureViewModel
#define TRACE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
using SignatureTool2.ViewModel.Setting;
using System.Windows.Controls.Primitives;

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
        private bool isOpenProtecterPanel;
        private bool isOpenCompanyPanel;

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

        public bool IsOpenProtecterPanel
        {
            get
            {
                return isOpenProtecterPanel;
            }
            set
            {
                SetProperty(ref isOpenProtecterPanel, value, "IsOpenProtecterPanel");
            }
        }
        public bool IsOpenCompanyPanel
        {
            get
            {
                return isOpenCompanyPanel;
            }
            set
            {
                SetProperty(ref isOpenCompanyPanel, value, "IsOpenCompanyPanel");
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

        private string nameTemplate = "";

        /// <summary>全局默认压缩包名模板；留空时单项也留空则沿用原 TargetPath 替换规则。</summary>
        public string NameTemplate
        {
            get
            {
                return nameTemplate;
            }
            set
            {
                SetProperty(ref nameTemplate, value, "NameTemplate");
            }
        }

        public ObservableCollection<SignatureModel> FileList { get; }

        public ObservableCollection<ProtecterModel> ProtecterList { get; }
        public ObservableCollection<CompanyModel> CompanyList { get; }

        public ICommand AddCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand SaveConfigCommand { get; set; }
        public ICommand ReCompressCommand { get; set; }

        public ICommand PostActionsCommand { get; set; }

        public ICommand SelectSourcePathCommand { get; set; }

        public ICommand SelectTargetPathCommand { get; set; }
        public ICommand SelectProtectPathCommand { get; set; }

        public ICommand SelectMsiSourceCommand { get; set; }

        public ICommand SelectMsiAipCommand { get; set; }

        public ICommand SelectMsiToolCommand { get; set; }
        

        public SignatureViewModel()
        {
            _signList = new ConcurrentQueue<SignatureTool>();
            AddCommand = new DelegateCommand(OnAddCommand);
            DeleteCommand = new DelegateCommand(OnDeleteCommand);
            SaveConfigCommand = new DelegateCommand(OnSaveConfigCommand);
            ReCompressCommand = new DelegateCommand(OnReCompressCommand);
            PostActionsCommand = new DelegateCommand(OnPostActionsCommand);
            SelectSourcePathCommand = new DelegateCommand(OnSelectSourcePathCommand);
            SelectTargetPathCommand = new DelegateCommand(OnSelectTargetPathCommand);
            SelectProtectPathCommand = new DelegateCommand(OnSelectProtectPathCommand);
            SelectMsiSourceCommand = new DelegateCommand(OnSelectMsiSourceCommand);
            SelectMsiAipCommand = new DelegateCommand(OnSelectMsiAipCommand);
            SelectMsiToolCommand = new DelegateCommand(OnSelectMsiToolCommand);
            SignCommand = new DelegateCommand(OnSignCommand);
            StopCommand = new DelegateCommand(OnStopCommand);
            FileList = new ObservableCollection<SignatureModel>();
            ProtecterList = new ObservableCollection<ProtecterModel>();
            CompanyList = new ObservableCollection<CompanyModel>();
            FileList.CollectionChanged += OnFileListChanged;
            _isLoadingConfig = true;
            ReadConfig();
            _isLoadingConfig = false;
            LogTool.Instance.WriteLogEventAdvance += Instance_WriteLogEventAdvance;
        }

        /// <summary>加载配置期间抑制自动保存，避免半初始化状态写盘。</summary>
        private bool _isLoadingConfig;

        /// <summary>修改即自动保存到 config.plist 的字段。</summary>
        private static readonly HashSet<string> AutoSaveProperties = new HashSet<string>
        {
            "PackageMsi", "MsiVersion", "MsiSourcePath", "MsiAipPath", "MsiToolPath"
        };

        private void OnFileListChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
            {
                return;
            }
            foreach (SignatureModel item in e.NewItems)
            {
                item.PropertyChanged += OnItemSettingChanged;
            }
        }

        private void OnItemSettingChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isLoadingConfig || !AutoSaveProperties.Contains(e.PropertyName))
            {
                return;
            }
            SaveConfig();
        }


        private void Instance_WriteLogEventAdvance(TraceEventType arg1, string msg)
        {
            if(arg1 == TraceEventType.Transfer)
            {
                if (_errorList.Any())
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
            // 立即终止构建/签名/压缩子进程
            RunControl.Stop();
        }

        private void OnSignCommand()
        {
            List<SignatureModel> list = FileList.ToList().FindAll((SignatureModel p) => p.IsSelected && p.IsAvailable);
            if (list.Count != 0)
            {
                _errorList.Clear();
                Package(list);
            }
        }

        private bool CheckKey(string company)
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
        private void OnSelectProtectPathCommand()
        {
            if (CheckHasData())
            {
                (string, string) filter = default((string, string));
                filter.Item1 = "混淆工程文件";
                filter.Item2 = "*.nrproj";
                string text = ChooseDialogTool.OpenSelectFileDialog(filter,false);
                if (!text.IsNullOrEmpty())
                {
                    SelectedItem.ProtectProject = text;
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

        private void OnSelectMsiSourceCommand()
        {
            if (CheckHasData())
            {
                string text = ChooseDialogTool.OpenSelectFolderDialog();
                if (!text.IsNullOrEmpty())
                {
                    SelectedItem.MsiSourcePath = text;
                }
            }
        }

        private void OnSelectMsiAipCommand()
        {
            if (CheckHasData())
            {
                (string, string) filter = default((string, string));
                filter.Item1 = "Advanced Installer工程";
                filter.Item2 = "*.aip";
                string text = ChooseDialogTool.OpenSelectFileDialog(filter, false);
                if (!text.IsNullOrEmpty())
                {
                    SelectedItem.MsiAipPath = text;
                }
            }
        }

        private void OnSelectMsiToolCommand()
        {
            if (CheckHasData())
            {
                (string, string) filter = default((string, string));
                filter.Item1 = "AdvancedInstaller.com";
                filter.Item2 = "AdvancedInstaller.com";
                string text = ChooseDialogTool.OpenSelectFileDialog(filter, false);
                if (!text.IsNullOrEmpty())
                {
                    SelectedItem.MsiToolPath = text;
                }
            }
        }

        private void OnSaveConfigCommand()
        {

            SaveConfig();
        }

        private void OnReCompressCommand()
        {
            if (SelectedItem != null)
            {
                new Thread((ThreadStart)delegate
                {

                    Com7zHelper com7 = new Com7zHelper();
                string targeFileName = BuildArchiveName(SelectedItem, ".7z");
                string filepath = Path.Combine(Path.GetDirectoryName(SelectedItem.TargetPath), targeFileName);
                if (filepath.IsExistsFile())
                {
                    FileTool.DeleteFile(filepath);
                }
                com7.CompressDirectory(SelectedItem.TargetPath, filepath, "7z");
                Trace.TraceInformation($"打包<{targeFileName}>完成！");
                RunPostActions(SelectedItem, filepath);
                Process.Start("explorer.exe", $"/select,\"{filepath}\"");
                }).Start();
            }
        }
        private void OnPostActionsCommand()
        {
            if (SelectedItem == null)
            {
                TipsTool.OpenTipsWindow("请先选择一个软件项！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            View.PostActionsDialog.Show(SelectedItem);
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
            signatureModel.SelectProtecterCommand =new DelegateCommand<SignatureModel>(OnSelectProtecter);
            signatureModel.SelectCompanyCommand =new DelegateCommand<SignatureModel>(OnSelectCompany);
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
                        CompanyID = dictionary.GetValue<string>("CompanyID"),
                        ProductInfoPath = dictionary.GetValue<string>("ProductInfoPath"),
                        SourcePath = dictionary.GetValue<string>("SourcePath"),
                        BiuldArgument = dictionary.GetValue<string>("BiuldArgument"),
                        LastSignTime = dictionary.GetValue<DateTime>("LastSignTime"),
                        LastBiuldTime = dictionary.GetValue<DateTime>("LastBiuldTime"),
                        TargetPath = dictionary.GetValue<string>("TargetPath"),
                        ProtecterID = dictionary.GetValue<string>("ProtecterID"),
                        ProtecterName= dictionary.GetValue<string>("ProtecterName"),
                        ProtectProject = dictionary.GetValue<string>("ProtectProject"),
                        SyncMode = string.IsNullOrWhiteSpace(dictionary.GetValue<string>("SyncMode")) ? "增量" : dictionary.GetValue<string>("SyncMode"),
                        FileNameTemplate = dictionary.GetValue<string>("FileNameTemplate") ?? "",
                        VerifySign = string.Equals(dictionary.GetValue<string>("VerifySign"), "True", StringComparison.OrdinalIgnoreCase),
                        PackageMsi = string.Equals(dictionary.GetValue<string>("PackageMsi"), "True", StringComparison.OrdinalIgnoreCase),
                        MsiVersion = dictionary.GetValue<string>("MsiVersion") ?? "",
                        MsiSourcePath = dictionary.GetValue<string>("MsiSourcePath") ?? "",
                        MsiAipPath = dictionary.GetValue<string>("MsiAipPath") ?? "",
                        MsiToolPath = dictionary.GetValue<string>("MsiToolPath") ?? "",
                        CopyTargets = dictionary.GetValue<string>("CopyTargets") ?? "",
                        FtpTargets = dictionary.GetValue<string>("FtpTargets") ?? "",
                    };

                    item.SelectProtecterCommand = new DelegateCommand<SignatureModel>(OnSelectProtecter);
                    item.SelectCompanyCommand = new DelegateCommand<SignatureModel>(OnSelectCompany);
                    li.Add(item);
                }
            });
            if (li.Count > 0)
            {
                li.First().IsSelected = true;
            }
            IOrderedEnumerable<SignatureModel> items = li.OrderBy((SignatureModel p) => p.SoftwareName);
            FileList.AddRange(items);
            string template = ConfigTool.Instance.GetValveOfRoot<string>(CRootKey.Package_NameTemplate);
            if (!string.IsNullOrWhiteSpace(template))
            {
                NameTemplate = template;
            }
        }

        private void OnSelectProtecter(SignatureModel model)
        {
            IsOpenProtecterPanel = false;
            IsOpenProtecterPanel = true;
            ProtecterList.ToList().ForEach(delegate (ProtecterModel p)
            {
                p.SelectionChanged -= Model_SelectionChanged;
            });
            ProtecterList.Clear();
            var nmodel = new ProtecterModel() { Name = "空" };
            nmodel.SelectionChanged += Model_SelectionChanged;
            ProtecterList.Add(nmodel);
            foreach (ProtecterSettingModel compilerSetting in ProtecterTool.Instance.ProtecterSettingList)
            {
                ProtecterModel compilerModel = new ProtecterModel
                {
                    ProtecterID = compilerSetting.protecterID,
                    ProtecterPath = compilerSetting.protecterPath,
                    Name = compilerSetting.name
                };
                compilerModel.SelectionChanged += Model_SelectionChanged;
                if (model.ProtecterID == compilerSetting.protecterID)
                {
                    compilerModel.SelectionChanged -= Model_SelectionChanged;
                    compilerModel.IsSelected = true;
                    compilerModel.SelectionChanged += Model_SelectionChanged;
                }
                ProtecterList.Add(compilerModel);
            }
            if (ProtecterList.FirstOrDefault((ProtecterModel p) => p.IsSelected) == null)
            {
                model.ProtecterID = "";
                model.ProtecterName = "";
            }

        }

        private void OnSelectCompany(SignatureModel model)
        {
            IsOpenCompanyPanel = false;
            IsOpenCompanyPanel = true;
            CompanyList.ToList().ForEach(delegate (CompanyModel p)
            {
                p.SelectionChanged -= Model_SelectionChanged;
            });
            CompanyList.Clear();
            var nmodel = new CompanyModel() { Name = "空" };
            nmodel.SelectionChanged += Model_SelectionChanged;
            CompanyList.Add(nmodel);
            foreach (CompanyModel item in CompanyTool.Instance.CompanySettingList)
            {
                CompanyModel compilerModel = new CompanyModel()
                {
                    CompanyID = item.CompanyID,
                    Name = item.Name,
                    Password = item.Password,
                    Sha1 = item.Sha1,
                };
                compilerModel.SelectionChanged += Model_SelectionChanged;
                if (model.CompanyID == compilerModel.CompanyID)
                {
                    compilerModel.SelectionChanged -= Model_SelectionChanged;
                    compilerModel.IsSelected = true;
                    compilerModel.SelectionChanged += Model_SelectionChanged;
                }
                CompanyList.Add(compilerModel);
            }
            if (CompanyList.FirstOrDefault((CompanyModel p) => p.IsSelected) == null)
            {
                model.CompanyID = "";
                model.Company = "";
            }
        }

        private void Model_SelectionChanged(CompanyModel model)
        {
            IsOpenCompanyPanel = false;
            if (SelectedItem != null && model.IsSelected)
            {
                if (string.IsNullOrEmpty(model.Sha1))
                {

                    SelectedItem.ProtecterID = null;
                    SelectedItem.ProtecterName = model.Name;
                }
                else
                {
                    SelectedItem.CompanyID = model.CompanyID;
                    SelectedItem.Company = model.Name;
                }
            }
        }
        private void Model_SelectionChanged(ProtecterModel model)
        {
            IsOpenProtecterPanel = false;
            if (SelectedItem != null && model.IsSelected)
            {
                if(string.IsNullOrEmpty(model.ProtecterPath)){

                    SelectedItem.ProtecterID = null;
                    SelectedItem.ProtecterName = model.Name;
                }
                else
                {
                    SelectedItem.ProtecterID = model.ProtecterID;
                    SelectedItem.ProtecterName = model.Name;
                }
            }
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
                        // 仅提示，不丢弃：路径缺失的项也要保留在配置中，避免换机器/改路径时数据丢失
                        Trace.TraceWarning("<" + file.SoftwareName + ">'s is not available, please check the selecte path");
                    }
                    file.IsNew = false;
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("SoftwareName", file.SoftwareName);
                dictionary.Add("SourcePath", file.SourcePath);
                dictionary.Add("Company", file.Company??"");
                dictionary.Add("CompanyID", file.CompanyID??"");
                dictionary.Add("LastSignTime", file.LastSignTime);
                dictionary.Add("LastBiuldTime", file.LastBiuldTime);
                dictionary.Add("BiuldArgument", file.BiuldArgument);
                dictionary.Add("ExcutableName", file.ExcutableName);
                dictionary.Add("ProductInfoPath", file.ProductInfoPath);
                dictionary.Add("TargetPath", file.TargetPath);
                dictionary.Add("ProtecterID", file.ProtecterID??"");
                dictionary.Add("ProtecterName", file.ProtecterName??"");
                dictionary.Add("ProtectProject", file.ProtectProject ?? "");
                dictionary.Add("SyncMode", file.SyncMode ?? "增量");
                dictionary.Add("FileNameTemplate", file.FileNameTemplate ?? "");
                dictionary.Add("VerifySign", file.VerifySign.ToString());
                dictionary.Add("PackageMsi", file.PackageMsi.ToString());
                dictionary.Add("MsiVersion", file.MsiVersion ?? "");
                dictionary.Add("MsiSourcePath", file.MsiSourcePath ?? "");
                dictionary.Add("MsiAipPath", file.MsiAipPath ?? "");
                dictionary.Add("MsiToolPath", file.MsiToolPath ?? "");
                dictionary.Add("CopyTargets", file.CopyTargets ?? "");
                dictionary.Add("FtpTargets", file.FtpTargets ?? "");
                list.Add(dictionary);
            }
            ConfigTool.Instance.SetValueOfRoot(CRootKey.Package_NameTemplate, NameTemplate ?? "");
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
            if (!PrepareStrictSync(li))
            {
                clickCount = 0;
                return;
            }
            _isSkip = false;
            IsSigning = true;
            IsEnabled = false;
            LoadingVisibility = Visibility.Visible;
            RunControl.Start();
            new Thread((ThreadStart)delegate
            {
                int successCount = 0;
                try
                {
                    SemaphoreTool semaphoreTool = new SemaphoreTool(li.Count, 1);
                    foreach (SignatureModel item in li)
                    {
                        item.StatusText = "";
                        var company = CompanyTool.Instance.GetCompanyByID(item.CompanyID);
                        if (company != null && !_isSkip && !RunControl.Token.IsCancellationRequested)
                        {

                            App.Current.Dispatcher.Invoke(() => Clipboard.SetDataObject(company.Password));
                            Trace.TraceWarning($"{item.SoftwareName}项<{company.Name}>公司的Key密码已复制！");
                        }
                        else if (company == null)
                        {
                            Trace.TraceWarning($"{item.SoftwareName}项公司属性未找到！");
                            continue;
                        }
                        semaphoreTool.InvokeByThread(delegate (object par)
                        {
                            if (_isSkip || RunControl.Token.IsCancellationRequested)
                            {
                                return;
                            }
                            SignatureModel currentModel = (SignatureModel)par;
                            if (CopyFile(currentModel))
                            {
                                Interlocked.Increment(ref successCount);
                                currentModel.LastSignTime = DateTime.Now;
                            }
                            currentModel.SignModelList = null;
                        }, "Sign_" + item.SoftwareName, item);
                    }
                    semaphoreTool.WaitAllFinish();
                    SaveConfig();
                    if (_isSkip || RunControl.Token.IsCancellationRequested)
                    {
                        Trace.TraceWarning("本次签名已停止！");
                    }
                    else
                    {
                        Trace.TraceInformation($"所有签名完成，总共{li.Count}项，成功{successCount}项，失败{li.Count - successCount}项！");
                        if (successCount > 0)
                        {
                            AskCompress(li);
                        }
                    }
                }
                finally
                {
                    RunControl.End();
                    LoadingVisibility = Visibility.Collapsed;
                    clickCount = 0;
                    IsSigning = false;
                    IsEnabled = true;
                }
            }).Start();
        }

        /// <summary>严格同步预检：列出目标目录多余文件供用户勾选保留；取消的项目从本次运行中移除。</summary>
        private bool PrepareStrictSync(List<SignatureModel> li)
        {
            foreach (SignatureModel item in li.ToList())
            {
                if (!item.IsStrictSync)
                {
                    continue;
                }
                List<string> staleFiles = GetStaleFiles(item);
                if (staleFiles.Count == 0)
                {
                    item.SyncKeepFiles = new HashSet<string>();
                    continue;
                }
                List<string> keepFiles = View.SyncCleanDialog.Show(staleFiles);
                if (keepFiles == null)
                {
                    li.Remove(item);
                    Trace.TraceWarning("<" + item.SoftwareName + ">严格同步被取消，本次已跳过该项目！");
                    continue;
                }
                item.SyncKeepFiles = new HashSet<string>(keepFiles, StringComparer.OrdinalIgnoreCase);
            }
            if (li.Count == 0)
            {
                Trace.TraceWarning("所有项目均被取消，本次签名未执行！");
                return false;
            }
            return true;
        }

        /// <summary>计算目标目录中“源目录已不存在”的多余文件。</summary>
        private List<string> GetStaleFiles(SignatureModel model)
        {
            List<string> result = new List<string>();
            string sourcePath = model.SourcePath;
            string targetPath = model.TargetPath;
            if (!targetPath.IsExistsFolder() || !sourcePath.IsExistsFolder())
            {
                return result;
            }
            List<string> allSource = FolderTool.GetAllFiles(sourcePath);
            HashSet<string> sourceRelative = new HashSet<string>(
                allSource.Select(p => p.Substring(sourcePath.Length).TrimStart('\\')),
                StringComparer.OrdinalIgnoreCase);
            foreach (string targetFile in FolderTool.GetAllFiles(targetPath))
            {
                string relative = targetFile.Substring(targetPath.Length).TrimStart('\\');
                if (!sourceRelative.Contains(relative))
                {
                    result.Add(targetFile);
                }
            }
            return result;
        }

        /// <summary>严格同步：删除目标目录中源目录已不存在的文件（保留用户勾选的），并清理空目录。</summary>
        private void DeleteStaleFiles(SignatureModel model)
        {
            List<string> staleFiles = GetStaleFiles(model);
            if (staleFiles.Count == 0)
            {
                return;
            }
            int deleted = 0;
            foreach (string stale in staleFiles)
            {
                if (model.SyncKeepFiles != null && model.SyncKeepFiles.Contains(stale))
                {
                    continue;
                }
                FileTool.DeleteFile(stale);
                deleted++;
            }
            foreach (string dir in Directory.GetDirectories(model.TargetPath, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                    }
                }
                catch
                {
                }
            }
            Trace.TraceInformation($"严格同步<{model.SoftwareName}>：清理{deleted}个多余文件（保留{staleFiles.Count - deleted}个）");
        }

        /// <summary>
        /// 按模板生成压缩包文件名。占位符：{Name}=软件名、{Company}=公司名，
        /// 其余 {xxx} 按日期时间格式解析（如 {yyyyMMdd}、{HHmm}），无法解析的原样保留。
        /// 单项模板与全局模板均留空时，沿用原有 TargetPath 中 "custom" 替换为日期+后缀的规则（无 "custom" 时回退为 {Name}_{yyyyMMdd}）。
        /// </summary>
        private string BuildArchiveName(SignatureModel item, string extension)
        {
            string template = item?.FileNameTemplate;
            if (string.IsNullOrWhiteSpace(template))
            {
                template = NameTemplate;
            }
            if (string.IsNullOrWhiteSpace(template))
            {
                string folderName = item?.TargetPath.GetFileName() ?? "";
                if (!string.IsNullOrEmpty(folderName) && folderName.Contains("custom"))
                {
                    return folderName.Replace("custom", $"{DateTime.Now.Date.ToString("yyyyMMdd")}{extension}");
                }
                template = "{Name}_{yyyyMMdd}";
            }
            string name = Regex.Replace(template, @"\{([^{}]+)\}", delegate (Match match)
            {
                string token = match.Groups[1].Value;
                if (token.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    return item?.SoftwareName ?? "";
                }
                if (token.Equals("Company", StringComparison.OrdinalIgnoreCase))
                {
                    return item?.Company ?? "";
                }
                try
                {
                    return DateTime.Now.ToString(token);
                }
                catch
                {
                    return match.Value;
                }
            });
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name + extension;
        }

        /// <summary>签名完成后询问并执行 7z/Zip 打包；压缩过程可被“停止”中断。</summary>
        private void AskCompress(List<SignatureModel> li)
        {
            var rl = MessageBox.Show("签名完成！是否打包？[Yes:7z;No:Zip;Cancel:Cancel]", "向导", MessageBoxButton.YesNoCancel);
            string format = rl == MessageBoxResult.Yes ? "7z" : (rl == MessageBoxResult.No ? "zip" : null);
            string extension = rl == MessageBoxResult.Yes ? ".7z" : ".zip";
            foreach (SignatureModel item in li)
            {
                if (_isSkip || RunControl.Token.IsCancellationRequested)
                {
                    Trace.TraceWarning("打包已停止！");
                    return;
                }
                string archivePath = null;
                if (format != null)
                {
                    string targeFileName = BuildArchiveName(item, extension);
                    archivePath = Path.Combine(Path.GetDirectoryName(item.TargetPath), targeFileName);
                    if (archivePath.IsExistsFile())
                    {
                        FileTool.DeleteFile(archivePath);
                    }
                    com7.CompressDirectory(item.TargetPath, archivePath, format);
                    Trace.TraceInformation($"打包<{targeFileName}>完成！");
                    Process.Start("explorer.exe", $"/select,\"{archivePath}\"");
                }
                if (item.PackageMsi)
                {
                    RunMsiPackage(item);
                }
                RunPostActions(item, archivePath);
            }
        }

        private static readonly Com7zHelper com7 = new Com7zHelper();

        /// <summary>微软商店(MSI)打包：目标目录完全同步到Msi源目录（保留track.txt）→ AIP设版本 → 默认构建。</summary>
        private void RunMsiPackage(SignatureModel item)
        {
            Trace.TraceInformation("开始微软商店(MSI)打包流程...");
            if (!SyncToMsiSource(item))
            {
                Trace.TraceError("<" + item.SoftwareName + ">Msi源目录同步失败，跳过MSI打包！");
                return;
            }
            if (!item.MsiToolPath.IsExistsFile())
            {
                Trace.TraceError("未找到AdvancedInstaller.com <" + item.MsiToolPath + ">，跳过MSI打包！");
                return;
            }
            if (!item.MsiAipPath.IsExistsFile())
            {
                Trace.TraceError("未找到AIP工程 <" + item.MsiAipPath + ">，跳过MSI打包！");
                return;
            }
            if (!item.MsiVersion.IsNullOrEmpty())
            {
                if (!RunAiCommand(item.MsiToolPath, "/edit \"" + item.MsiAipPath + "\" /SetVersion " + item.MsiVersion.Trim()))
                {
                    return;
                }
                Trace.TraceInformation("AIP版本已设置为<" + item.MsiVersion.Trim() + ">");
            }
            if (RunAiCommand(item.MsiToolPath, "/build \"" + item.MsiAipPath + "\""))
            {
                Trace.TraceInformation("MSI默认构建(DefaultBuild)完成！");
            }
        }

        /// <summary>把目标目录镜像同步到Msi源目录：全量覆盖，清除多余文件，但保留根目录的track.txt。</summary>
        private bool SyncToMsiSource(SignatureModel item)
        {
            string source = item.TargetPath;
            string dest = item.MsiSourcePath;
            if (dest.IsNullOrEmpty())
            {
                Trace.TraceError("<" + item.SoftwareName + ">未配置Msi源文件夹路径！");
                return false;
            }
            if (!source.IsExistsFolder())
            {
                Trace.TraceError("<" + item.SoftwareName + ">目标路径不存在 <" + source + ">，无法同步Msi源目录！");
                return false;
            }
            try
            {
                Directory.CreateDirectory(dest);
                if (!CopyTree(source, dest))
                {
                    return false;
                }
                // 清除源目录中不存在的多余文件（track.txt 保留）
                HashSet<string> sourceRelative = new HashSet<string>(
                    FolderTool.GetAllFiles(source).Select(p => p.Substring(source.Length).TrimStart('\\')),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string file in FolderTool.GetAllFiles(dest))
                {
                    if (_isSkip || RunControl.Token.IsCancellationRequested)
                    {
                        return false;
                    }
                    string relative = file.Substring(dest.Length).TrimStart('\\');
                    if (relative.Equals("track.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (!sourceRelative.Contains(relative))
                    {
                        FileTool.DeleteFile(file);
                        Trace.TraceInformation("Msi源目录清除多余文件<" + relative + ">");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("同步Msi源目录失败: " + ex.Message);
                return false;
            }
        }

        /// <summary>递归拷贝目录（覆盖已存在文件）。</summary>
        private bool CopyTree(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                if (_isSkip || RunControl.Token.IsCancellationRequested)
                {
                    return false;
                }
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
            }
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                if (_isSkip || RunControl.Token.IsCancellationRequested)
                {
                    return false;
                }
                if (!CopyTree(dir, Path.Combine(destDir, Path.GetFileName(dir))))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>执行 AdvancedInstaller.com 命令；支持通过"停止"终止，返回是否成功。</summary>
        private bool RunAiCommand(string toolPath, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process process = Process.Start(psi))
                {
                    RunControl.Track(process);
                    System.Threading.Tasks.Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    System.Threading.Tasks.Task<string> errorTask = process.StandardError.ReadToEndAsync();
                    while (!process.WaitForExit(200))
                    {
                        if (_isSkip || RunControl.Token.IsCancellationRequested)
                        {
                            RunControl.KillTree(process);
                            Trace.TraceWarning("MSI打包命令已终止: " + arguments);
                            return false;
                        }
                    }
                    RunControl.Untrack(process);
                    string output = (outputTask.Result + errorTask.Result).Trim();
                    if (process.ExitCode != 0)
                    {
                        Trace.TraceError("AdvancedInstaller命令失败(" + arguments + "): " + output);
                        return false;
                    }
                    if (!output.IsNullOrEmpty())
                    {
                        Trace.TraceInformation(output);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("执行AdvancedInstaller命令异常: " + ex.Message);
                return false;
            }
        }

        /// <summary>打包后动作流水线：校验全签名 → 本地拷贝 → FTP 上传。</summary>
        private void RunPostActions(SignatureModel item, string archivePath)
        {
            if (item == null)
            {
                return;
            }
            if (item.VerifySign)
            {
                VerifyAllSigned(item);
            }
            if (archivePath == null || !archivePath.IsExistsFile())
            {
                if (item.CopyTargets != null && item.SplitTargets(item.CopyTargets).Count > 0
                    || item.FtpTargets != null && item.SplitTargets(item.FtpTargets).Count > 0)
                {
                    Trace.TraceWarning("<" + item.SoftwareName + ">未生成压缩包，跳过拷贝与上传！");
                }
                return;
            }
            // 本地目录拷贝
            foreach (string dir in item.SplitTargets(item.CopyTargets))
            {
                if (_isSkip || RunControl.Token.IsCancellationRequested)
                {
                    return;
                }
                try
                {
                    Directory.CreateDirectory(dir);
                    string targetFile = Path.Combine(dir, Path.GetFileName(archivePath));
                    File.Copy(archivePath, targetFile, true);
                    Trace.TraceInformation($"已拷贝<{Path.GetFileName(archivePath)}>到<{dir}>");
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"拷贝到<{dir}>失败: {ex.Message}");
                }
            }
            // FTP 上传
            foreach (string ftpUrl in item.SplitTargets(item.FtpTargets))
            {
                if (_isSkip || RunControl.Token.IsCancellationRequested)
                {
                    return;
                }
                UploadToFtp(archivePath, ftpUrl);
            }
        }

        /// <summary>校验目标目录内所有可签名文件是否均已签名。</summary>
        private bool VerifyAllSigned(SignatureModel item)
        {
            Trace.TraceInformation("校验<" + item.SoftwareName + ">目标目录签名状态...");
            List<string> unsigned = new List<string>();
            foreach (string file in FolderTool.GetAllFiles(item.TargetPath))
            {
                if (!SignatureTool.SupportSignFile.Contains(file.GetExtension().ToLower()))
                {
                    continue;
                }
                if (!SignatureTool.GetIsSigned(file))
                {
                    unsigned.Add(file);
                }
            }
            if (unsigned.Count == 0)
            {
                Trace.TraceInformation("校验<" + item.SoftwareName + ">通过：全部已签名！");
                return true;
            }
            Trace.TraceError($"校验<{item.SoftwareName}>发现{unsigned.Count}个未签名文件：\r\n" + string.Join("\r\n", unsigned));
            return false;
        }

        /// <summary>上传文件到 FTP；地址支持 ftp://user:pass@host/path 形式，默认匿名。</summary>
        private bool UploadToFtp(string filePath, string ftpUrl)
        {
            try
            {
                Uri uri = new Uri(ftpUrl);
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.UseBinary = true;
                request.UsePassive = true;
                request.Timeout = 60000;
                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    string[] parts = uri.UserInfo.Split(':');
                    request.Credentials = new NetworkCredential(Uri.UnescapeDataString(parts[0]), parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "");
                }
                byte[] fileData = File.ReadAllBytes(filePath);
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileData, 0, fileData.Length);
                }
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Trace.TraceInformation($"FTP上传<{Path.GetFileName(filePath)}>到<{ftpUrl}>完成: {response.StatusDescription.Trim()}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"FTP上传到<{ftpUrl}>失败: {ex.Message}");
                return false;
            }
        }

        private bool CopyFile(SignatureModel model)
        {
            bool result = false;
            if (_isSkip || RunControl.Token.IsCancellationRequested)
            {
                return false;
            }
            if (model.checkNeedBuild())
            {

                var compiler = CompilerTool.Instance.GetCompilerByID(null);
                if (compiler == null || compiler.vsBuilderPath.IsNullOrEmpty())
                {
                    Trace.TraceError("<" + model.SoftwareName + ">未配置编译器，无法构建，已跳过拷贝与签名！");
                    model.StatusText = "未配置编译器";
                    return false;
                }
                bool buildSuccess = SlnBuild.Build(model.BiuldArgument, Path.GetDirectoryName(compiler.vsBuilderPath.Trim('\"')));
                if (!buildSuccess)
                {
                    Trace.TraceError("构建<" + model.SoftwareName + ">失败，已跳过拷贝与签名！");
                    model.StatusText = "构建失败";
                    return false;
                }
                model.LastBiuldTime = DateTime.Now;
                if (!string.IsNullOrEmpty(model.ProtecterID) && !string.IsNullOrEmpty(model.ProtectProject))
                {
                    if (!ProtectProjectFiles(model))
                    {
                        model.StatusText = "保护失败";
                        return false;
                    }
                }
                else if (!string.IsNullOrEmpty(model.ProtecterID) || !string.IsNullOrEmpty(model.ProtectProject))
                {
                    Trace.TraceWarning($"项目 <{model.SoftwareName}> 代码保护配置不完整（ProtecterID=\"{model.ProtecterID}\", ProtectProject=\"{model.ProtectProject}\"），已跳过代码保护。");
                }
            }
            FindNeedCopyFile(model);
            if (model.IsStrictSync)
            {
                DeleteStaleFiles(model);
            }
            if (CopyFileToTarget(model))
            {

                if (SignatureFile(model))
                {
                    result = true;
                    Trace.TraceInformation("签名<" + model.SoftwareName + ">成功！");
                }
                else
                {
                    // 自动重试失败的文件（不再弹窗，避免阻塞停止操作）
                    const int maxRetry = 3;
                    for (int attempt = 1; attempt <= maxRetry && !result; attempt++)
                    {
                        if (_isSkip || RunControl.Token.IsCancellationRequested)
                        {
                            break;
                        }
                        Trace.TraceWarning($"签名<{model.SoftwareName}>出现失败，自动重试({attempt}/{maxRetry})！");
                        Thread.Sleep(1000);
                        result = Retry(model);
                    }
                    if (!result && !_isSkip && !RunControl.Token.IsCancellationRequested)
                    {
                        Trace.TraceWarning("签名<" + model.SoftwareName + ">重试后仍未完成签名！");
                        model.StatusText = "签名失败";
                    }
                }
            }
            else
            {
                Trace.TraceWarning("拷贝<" + model.SoftwareName + ">出现失败，未能完成签名！");
                model.StatusText = "拷贝失败";
            }
            if (result)
            {
                model.StatusText = "签名成功";
            }
            else if (_isSkip || RunControl.Token.IsCancellationRequested)
            {
                model.StatusText = "已停止";
            }
            return result;
        }

        private bool ProtectProjectFiles(SignatureModel model)
        {
            if (string.IsNullOrWhiteSpace(model.SourcePath) || !Directory.Exists(model.SourcePath))
            {
                Trace.TraceError($"项目 <{model.SoftwareName}> 源码输出目录不存在：{model.SourcePath}");
                return ShowWarningConfirm(
                    $"源码输出目录不存在：\r\n{model.SourcePath}\r\n\r\n是否忽略代码保护并继续后续流程？\r\n【是】：忽略并继续流程\r\n【否】：终止流程",
                    "提示 - 目录不存在");
            }

            // 保护前先清理可能存在的历史 *_Secure 临时目录，避免假阳性
            try
            {
                foreach (string oldDir in Directory.GetDirectories(model.SourcePath, "*_Secure"))
                {
                    FolderTool.DeleteFolder(oldDir);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"清理历史保护目录异常：{ex.Message}");
            }

            ProtecterSettingModel protecter = ProtecterTool.Instance.GetProtecterByID(model.ProtecterID);
            if (protecter == null)
            {
                Trace.TraceError($"未找到 ID 为 [{model.ProtecterID}] 的保护器配置！");
                return ShowWarningConfirm(
                    $"未找到 ID 为 [{model.ProtecterID}] 的代码保护配置。\r\n\r\n是否忽略该配置并继续后续流程？\r\n【是】：忽略并继续流程\r\n【否】：终止流程",
                    "提示 - 保护器未配置");
            }

            if (!protecter.IsAvailable)
            {
                Trace.TraceError($"保护器 <{protecter.name}> 可执行文件不存在：{protecter.protecterPath}");
                return ShowWarningConfirm(
                    $"保护器 <{protecter.name}> 可执行文件不存在：\r\n{protecter.protecterPath}\r\n\r\n是否忽略代码保护并继续后续流程？\r\n【是】：忽略并继续流程\r\n【否】：终止流程",
                    "提示 - 保护器路径无效");
            }

            if (!File.Exists(model.ProtectProject))
            {
                Trace.TraceError($"项目 <{model.SoftwareName}> 混淆工程文件不存在：{model.ProtectProject}");
                return ShowWarningConfirm(
                    $"混淆工程文件不存在：\r\n{model.ProtectProject}\r\n\r\n是否忽略代码保护并继续后续流程？\r\n【是】：忽略并继续流程\r\n【否】：终止流程",
                    "提示 - 工程文件不存在");
            }

            if (!protecter.ProtecterFile(model.ProtectProject))
            {
                Trace.TraceWarning($"项目 <{model.SoftwareName}> 代码保护执行返回失败！");
                if (!ShowWarningConfirm(
                    $"项目 <{model.SoftwareName}> 代码保护执行返回失败！\r\n\r\n是否继续执行后续流程？\r\n【是】：继续流程\r\n【否】：终止流程",
                    "提示 - 代码保护执行失败"))
                {
                    Trace.TraceError($"用户选择终止，项目 <{model.SoftwareName}> 因保护执行失败已退出。");
                    return false;
                }
            }

            string[] secureDirs;
            var secureFiles = new List<(string FilePath, string RelativePath)>();
            try
            {
                secureDirs = Directory.GetDirectories(model.SourcePath, "*_Secure");
                foreach (string dir in secureDirs)
                {
                    string normalizedDir = dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    int prefixLen = normalizedDir.Length + 1;
                    foreach (string filePath in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        secureFiles.Add((filePath, filePath.Substring(prefixLen)));
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"扫描保护产物目录异常：{ex.Message}");
                return ShowWarningConfirm(
                    $"扫描保护产物目录失败：{ex.Message}\r\n\r\n是否继续执行后续流程？\r\n【是】：继续流程\r\n【否】：终止流程",
                    "提示 - 扫描保护产物异常");
            }

            if (secureFiles.Count == 0)
            {
                Trace.TraceWarning($"路径 <{model.SourcePath}> 下未检测到保护后的文件夹 \"*_Secure\"！");
                if (!ShowWarningConfirm(
                    $"当前路径下未检测到保护后的文件夹 \"*_Secure\"（或文件夹内无文件）！\r\n\r\n路径：{model.SourcePath}\r\n\r\n是否继续执行后续流程？\r\n【是】：继续流程\r\n【否】：终止流程",
                    "提示 - 未检测到保护产物"))
                {
                    Trace.TraceError($"用户选择终止，项目 <{model.SoftwareName}> 因未找到保护产物已退出。");
                    return false;
                }
                return true;
            }

            StringBuilder fileInfoSb = new StringBuilder();
            fileInfoSb.AppendLine($"检测到受保护文件共 {secureFiles.Count} 个：");
            int index = 1;
            foreach (var item in secureFiles)
            {
                FileInfo sfi = new FileInfo(item.FilePath);
                string origPath = Path.Combine(model.SourcePath, item.RelativePath);
                if (File.Exists(origPath))
                {
                    FileInfo origFi = new FileInfo(origPath);
                    fileInfoSb.AppendLine($"{index++}. {item.RelativePath} (原大小: {origFi.Length} 字节 -> 保护后: {sfi.Length} 字节)");
                }
                else
                {
                    fileInfoSb.AppendLine($"{index++}. {item.RelativePath} (保护后: {sfi.Length} 字节, 源目录无同名文件)");
                }
            }

            fileInfoSb.AppendLine("\r\n请核对上述受保护的文件清单是否完整：");
            fileInfoSb.AppendLine("【是】：清单完整无误，执行覆盖并继续流程");
            fileInfoSb.AppendLine("【否】：清单不完整或有异常，终止流程");

            var confirmResult = MessageDialog.Show(fileInfoSb.ToString(), $"保护产物确认 - <{model.SoftwareName}>", MessageBoxButton.YesNo);
            if (confirmResult != MessageBoxResult.Yes)
            {
                Trace.TraceError($"用户确认保护产物不完整或选择终止，项目 <{model.SoftwareName}> 已退出。");
                return false;
            }

            try
            {
                foreach (var item in secureFiles)
                {
                    string targetPath = Path.Combine(model.SourcePath, item.RelativePath);
                    string targetDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    File.Copy(item.FilePath, targetPath, true);
                }

                foreach (string dir in secureDirs)
                {
                    FolderTool.DeleteFolder(dir);
                }

                Trace.TraceInformation($"项目 <{model.SoftwareName}> 保护文件已成功覆盖至目标源码目录。");
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"覆盖受保护文件失败：{ex.Message}");
                return ShowWarningConfirm(
                    $"覆盖受保护文件时发生异常：\r\n{ex.Message}\r\n\r\n已保留保护产物目录以便排查。\r\n【是】：仍继续后续流程\r\n【否】：终止流程",
                    "提示 - 覆盖保护产物失败");
            }
        }

        private static bool ShowWarningConfirm(string message, string title)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return (bool)Application.Current.Dispatcher.Invoke(new Func<bool>(() => ShowWarningConfirm(message, title)));
            }

            Window owner = Application.Current?.MainWindow;
            MessageBoxResult result = (owner != null && owner.IsVisible)
                ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
                : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return result == MessageBoxResult.Yes;
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
            string sourcePath = model.SourcePath;
            string targetPath = model.TargetPath;
            List<string> allFiles = FolderTool.GetAllFiles(sourcePath);
            List<CopyFileModel> list = new List<CopyFileModel>();
            DateTime dateTime = DateTime.Now.Date;
            List<string> supportSignFile = SignatureTool.SupportSignFile;
            foreach (string item in allFiles)
            {
                DateTime lastWriteTime = new FileInfo(item).LastWriteTime;
                if (model.LastSignTime == DateTime.MinValue)
                    model.LastSignTime = DateTime.Now.Date;
                // 严格同步：全量拷贝；增量：只拷贝比上次签名新的文件
                bool needCopy = model.IsStrictSync || !(lastWriteTime < model.LastSignTime);
                if (needCopy && supportSignFile.Contains(item.GetExtension().ToLower()))
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
                string filePath = list[i].SourcePath;
                CopyFileModel copyFileModel2 = new CopyFileModel();
                copyFileModel2.SourcePath = filePath;
                copyFileModel2.RelativePath = filePath.Replace(sourcePath + "\\", "");
                copyFileModel2.TargetPath = targetPath.Combine(copyFileModel2.RelativePath);
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
