// SignTool.ViewModel.Signature.SetupSignatureViewModel
#define TRACE
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using SignatureTool2;
using SignatureTool2.Utilites;
using SignatureTool2.Utilites.DialogWindow;
using SignatureTool2.Utilites.Directorys;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Files;
using SignatureTool2.Utilites.Log;
using SignatureTool2.Utilites.Sign;
using SignatureTool2.ViewModel.Setting;
using SignatureTool2.ViewModel.Signature;

namespace SignatureTool2.ViewModel.Signature
{

    public class SetupSignatureViewModel : ViewModelBase
    {
        private string setupSaveFolder;

        private List<string> _errorList = new List<string>();

        private SetupSignatureModel selectedSetup;

        private string errorText;

        private bool isOpenComplierPanel;

        private bool isWaitting;

        public SetupSignatureModel SelectedSetup
        {
            get
            {
                return selectedSetup;
            }
            set
            {
                SetProperty(ref selectedSetup, value, "SelectedSetup");
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

        public bool IsOpenComplierPanel
        {
            get
            {
                return isOpenComplierPanel;
            }
            set
            {
                SetProperty(ref isOpenComplierPanel, value, "IsOpenComplierPanel");
            }
        }

        public bool IsWaitting
        {
            get
            {
                return isWaitting;
            }
            set
            {
                SetProperty(ref isWaitting, value, "IsWaitting");
            }
        }

        public ObservableCollection<SetupSignatureModel> DataList { get; }

        public ObservableCollection<CompilerModel> CompilerList { get; }

        public DelegateCommand AddCommand { get; }

        public DelegateCommand DeleteCommand { get; }

        public DelegateCommand SignCommand { get; }

        public DelegateCommand SaveCommand { get; }

        public SetupSignatureViewModel()
        {
            AddCommand = new DelegateCommand(OnAdd);
            DeleteCommand = new DelegateCommand(OnDelete);
            SignCommand = new DelegateCommand(OnSign);
            SaveCommand = new DelegateCommand(OnSave);
            DataList = new ObservableCollection<SetupSignatureModel>();
            CompilerList = new ObservableCollection<CompilerModel>();
            ReadConfig();
            LogTool.Instance.WriteLogEvent += Instance_WriteLogEvent;
            setupSaveFolder = Directory.GetDirectoryRoot(AppDomain.CurrentDomain.BaseDirectory).Combine("Output");
        }

        private void Instance_WriteLogEvent(string msg)
        {
            _errorList.Insert(0, msg);
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string error in _errorList)
            {
                stringBuilder.AppendLine(error);
            }
            ErrorText = stringBuilder.ToString();
        }

        private void OnSave()
        {
            List<object> list = new List<object>();
            foreach (SetupSignatureModel data in DataList)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_CompilerID, data.CompilerID);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_CompilerName, data.CompilerName);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_Name, data.Name);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_SaveName, data.SaveName);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_SetupIconPath, data.SetupIconPath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_SetupNSISPath, data.SetupNSISPath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_UninstallExeSaveFolderPath, data.UninstallEXESavePath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_WpfResourcePath, data.WpfResourcePath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_UninstallIconPath, data.UninstallIconPath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_UninstallNSISPath, data.UninstallNSISPath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_IsWpf, data.IsWpf.ToString());
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_CompanyIdx, data.IsGemoo ? "0" : "1");
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_IsWpf, data.IsWpf.ToString());
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_WPFSlnPath, data.WpfSlnPath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.CS_WPFOutputPath, data.WpfOutPutPath);
                list.Add(dictionary);
                data.IsSaved = true;
            }
            ConfigTool.Instance.SetValueOfRoot(CRootKey.Compiler_Setup, list);
            if (list.Count > 0)
                ConfigTool.Instance.BackupConfig();
            Trace.TraceInformation("保存配置成功！");
        }

        private void OnSign()
        {
            _errorList.Clear();
            ErrorText = "";
            CreateSetup();
        }

        private void OnDelete()
        {
            if (DataList.FirstOrDefault((SetupSignatureModel p) => p.IsSelected) == null || TipsTool.OpenTipsWindow("确认删除选中项？", "删除", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
            {
                return;
            }
            for (int i = 0; i < DataList.Count; i++)
            {
                if (DataList[i].IsSelected)
                {
                    DataList.RemoveAt(i);
                    i--;
                }
            }
            OnSave();
        }

        private void OnAdd()
        {
            foreach (SetupSignatureModel data in DataList)
            {
                data.IsSelected = false;
            }
            SetupSignatureModel setupSignatureModel = new SetupSignatureModel
            {
                Name = "新增项",
                IsSelected = true
            };
            setupSignatureModel.SelectCompilerCommand = new DelegateCommand<SetupSignatureModel>(OnSelectCompiler);
            DataList.Add(setupSignatureModel);
        }

        private void OnSelectCompiler(SetupSignatureModel selected)
        {
            IsOpenComplierPanel = false;
            IsOpenComplierPanel = true;
            CompilerList.ToList().ForEach(delegate (CompilerModel p)
            {
                p.SelectionChanged -= Model_SelectionChanged;
            });
            CompilerList.Clear();
            foreach (CompilerSettingModel compilerSetting in CompilerTool.Instance.CompilerSettingList)
            {
                CompilerModel compilerModel = new CompilerModel
                {
                    CommandParameter = compilerSetting.commandParameter,
                    VsBuilderPath = compilerSetting.vsBuilderPath,
                    WpfResourcePath = compilerSetting.wpfResourcePath,
                    CompilerIconSavePath = compilerSetting.compilerIconSavePath,
                    CompilerID = compilerSetting.compilerID,
                    CompilerPath = compilerSetting.compilerPath,
                    ReplaceIconName = compilerSetting.replaceIconName,
                    Name = compilerSetting.name
                };
                compilerModel.SelectionChanged += Model_SelectionChanged;
                if (selected.CompilerID == compilerSetting.compilerID)
                {
                    compilerModel.SelectionChanged -= Model_SelectionChanged;
                    compilerModel.IsSelected = true;
                    compilerModel.SelectionChanged += Model_SelectionChanged;
                }
                CompilerList.Add(compilerModel);
            }
            if (CompilerList.FirstOrDefault((CompilerModel p) => p.IsSelected) == null)
            {
                selected.CompilerID = "";
                selected.CompilerName = "";
            }
        }

        private void Model_SelectionChanged(CompilerModel obj)
        {
            IsOpenComplierPanel = false;
            if (SelectedSetup != null && obj.IsSelected)
            {
                SelectedSetup.CompilerID = obj.CompilerID;
                SelectedSetup.CompilerName = obj.Name;
            }
        }

        private void CreateSetup()
        {
            int total = 0;
            foreach (SetupSignatureModel data in DataList)
            {
                data.CreateResult = "";
                if (data.IsSelected)
                {
                    total++;
                    data.CreateResult = "等待...";
                }
            }
            if (total == 0)
            {
                return;
            }
            List<SetupSignatureModel> li = DataList.ToList().FindAll((SetupSignatureModel p) => p.IsSelected);
            IsWaitting = true;
            string outputDir = li.FirstOrDefault().SetupNSISPath;
            Task.Factory.StartNew(delegate
            {
                int num = 0;
                foreach (SetupSignatureModel item in li)
                {
                    if (item.IsSelected)
                    {
                        var company = item.IsGemoo ? "Gemoo" : "iMobie";
                        if (!SignatureTool.CheckKey(company))
                        {
                            Trace.TraceWarning($"无法完成{item.Name}项，因为{company}的Key没有插入！");
                            continue;
                        }
                        Trace.TraceWarning($"{item.Name}项Key密码已复制！");

                        item.CreateResult = "生成中...";
                        if (CreateUninstall(item))
                        {
                            num++;
                            item.CreateResult = "成功！";
                        }
                        else
                        {
                            item.CreateResult = "失败！！！";
                        }
                    }
                }
                if (num == total)
                {
                    Trace.TraceInformation($"全部成功(总共{total}项)");
                }
                else
                {
                    Trace.TraceInformation($"部分(总共{total}项，成功{num}项，失败{total - num}项)");
                }
                IsWaitting = false;
                if (num > 0)
                {
                    Process.Start("explorer.exe", setupSaveFolder);
                }
            });
        }

        private bool CreateUninstall(SetupSignatureModel model)
        {
            bool flag = false;
            Trace.TraceInformation("---------------生成<" + model.Name + ">---------------");
            if (model.IsWpf)
            {
                flag = CreateWPFInstallerAndUninstaller(model);
            }
            if (!flag)
            {
                return false;
            }
            if (model.UninstallNSISPath.IsNullOrEmpty())
            {
                Trace.TraceInformation("<" + model.Name + ">的卸载包NSIS路径为空，已跳过生成");
                flag = CreateInstall(model);
            }
            else
            {
                Trace.TraceInformation("生成<" + model.Name + ">的卸载包");
                if (model.VerifyWhileCreateUninstall())
                {
                    string text = model.UninstallEXESavePath.Combine(model.UninstallName + ".exe");
                    flag = CompilerTool.Instance.GetCompilerByID(model.CompilerID).CompilerFile(model.UninstallNSISPath, text);
                    if (flag)
                    {
                        Trace.TraceInformation("签名<" + model.Name + ">的卸载包");
                        SignModel model2 = SignatureTool.CreateSignModel(text);
                        flag = new SignatureTool().SignatureFile(model2);
                        Trace.TraceInformation("签名<" + model.Name + ">的卸载包" + (flag ? "成功" : "失败"));
                        if (flag)
                        {
                            flag = CreateInstall(model);
                        }
                    }
                    else
                    {
                        Trace.TraceInformation("<" + model.Name + ">的卸载包生成失败");
                    }
                }
            }
            Trace.TraceInformation("---------------生成<" + model.Name + ">结束---------------");
            return flag;
        }

        private bool CreateWPFInstallerAndUninstaller(SetupSignatureModel model)
        {
            FolderTool.DeleteFolder(model.WpfOutPutPath);

            var compiler = CompilerTool.Instance.GetCompilerByID(model.CompilerID);
            SlnBuild.Build(model.CreateBuildArg(), Path.GetDirectoryName(compiler.vsBuilderPath.Trim('\"')));
            string installexePath = Path.Combine(model.WpfOutPutPath, "setup.exe");
            string uninstallexePath = Path.Combine(model.WpfOutPutPath, "uninstall.exe");
            if (installexePath.IsExistsFile() && uninstallexePath.IsExistsFile())
            {
                List<string> list = new List<string>();
                foreach (var file in Directory.GetFiles(model.WpfOutPutPath))
                {
                    if (file.EndsWith(".dll") || file.EndsWith(".exe"))
                    {
                        list.Add(file);
                    }
                }
                var smodel = SignatureTool.CreateSignModel(list);

                foreach (var item in smodel)
                {
                    string name = Path.GetFileName(item.FilePath);
                    string Targetname = Path.Combine(model.WpfResourcePath, name);
                    FileTool.CopyFile(item.FilePath, Targetname);

                }
                var flag = new SignatureTool().SignatureFiles("wpf Installers", smodel);
                return flag;
            }
            else
            {
                if (!installexePath.IsExistsFile())
                    Trace.TraceInformation("<" + model.Name + ">的WPF安装包路径为空，已跳过生成");
                else if (!uninstallexePath.IsExistsFile())
                    Trace.TraceInformation("<" + model.Name + ">的WPF写在包路径为空，已跳过生成");
            }
            return false;
        }

        private bool CreateInstall(SetupSignatureModel model)
        {
            bool flag = false;
            Trace.TraceInformation("生成<" + model.Name + ">的安装包--->");
            if (model.VerifyWhileCreateSetup())
            {
                Directory.CreateDirectory(setupSaveFolder);
                string text = model.SaveName;
                string parent = Directory.GetParent(model.SetupNSISPath).FullName;
                setupSaveFolder = parent;
                if (!text.EndsWith(".exe"))
                {
                    text = Path.Combine(setupSaveFolder, model.SaveName + ".exe");
                }
                else
                {
                    text = Path.Combine(setupSaveFolder, model.SaveName);
                }
                if (File.Exists(text))
                    File.Delete(text);
                flag = CompilerTool.Instance.GetCompilerByID(model.CompilerID).CompilerFile(model.SetupNSISPath, text);
                if (flag)
                {
                    Trace.TraceInformation("签名<" + model.Name + ">的安装包");
                    SignModel model2 = SignatureTool.CreateSignModel(text);
                    flag = new SignatureTool().SignatureFile(model2);
                    Trace.TraceInformation("签名<" + model.Name + ">的安装包" + (flag ? "成功" : "失败"));
                }
                else
                {
                    Trace.TraceInformation("<" + model.Name + ">的安装包生成失败");
                }
            }
            return flag;
        }

        private void ReadConfig()
        {
            List<object> valveOfRoot = ConfigTool.Instance.GetValveOfRoot<List<object>>(CRootKey.Compiler_Setup);
            if (valveOfRoot == null)
            {
                return;
            }
            ConfigTool instance = ConfigTool.Instance;
            foreach (object item in valveOfRoot)
            {
                Dictionary<string, object> dictionary = item as Dictionary<string, object>;
                if (dictionary != null)
                {
                    SetupSignatureModel setupSignatureModel = new SetupSignatureModel
                    {
                        CompilerID = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_CompilerID),
                        CompilerName = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_CompilerName),
                        Name = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_Name),
                        SaveName = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_SaveName),
                        SetupIconPath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_SetupIconPath),
                        SetupNSISPath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_SetupNSISPath),
                        UninstallEXESavePath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_UninstallExeSaveFolderPath),
                        WpfResourcePath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_WpfResourcePath),
                        UninstallIconPath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_UninstallIconPath),
                        UninstallNSISPath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_UninstallNSISPath),
                        WpfSlnPath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_WPFSlnPath),
                        WpfOutPutPath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_WPFOutputPath),
                        IsSaved = true
                    };
                    var IsWpf = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_IsWpf);
                    if (bool.TryParse(IsWpf, out bool isWPf))
                    {
                        setupSignatureModel.IsWpf = isWPf;
                    }
                    var CompanyIdx = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.CS_CompanyIdx);
                    if (CompanyIdx == "0")
                    {
                        setupSignatureModel.IsGemoo = true;
                        setupSignatureModel.IsAny = false;
                    }
                    else if (CompanyIdx == "1")
                    {
                        setupSignatureModel.IsAny = true;
                        setupSignatureModel.IsGemoo = false;
                    }
                    setupSignatureModel.SelectCompilerCommand = new DelegateCommand<SetupSignatureModel>(OnSelectCompiler);
                    DataList.Add(setupSignatureModel);
                }
            }
            using (IEnumerator<SetupSignatureModel> enumerator2 = DataList.GetEnumerator())
            {
                if (enumerator2.MoveNext())
                {
                    enumerator2.Current.IsSelected = true;
                }
            }
            DataList.OrderBy((SetupSignatureModel p) => p.Name);
        }
    }
}
