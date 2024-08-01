// SignTool.ViewModel.Signature.SetupSignatureModel
#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Management.Instrumentation;
using System.Windows;
using System.Windows.Forms;
using Prism.Commands;
using SignatureTool2;
using SignatureTool2.Utilites;
using SignatureTool2.Utilites.DialogWindow;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Files;
using SignatureTool2.ViewModel.Signature;
namespace SignatureTool2.ViewModel.Signature
{

    public class SetupSignatureModel : ViewModelBase
    {
        private string name;

        private string saveName;

        private string uninstallName = "uninstall";

        private string setupNSISPath;

        private string setupIconPath;

        private string uninstallNSISPath;

        private string uninstallIconPath;

        private string uninstallEXESavePath;
        private string wpfReouscePath;

        private string compilerName;

        private string wpfOutPutPath;

        private string wpfSlnPath;

        private bool isSaved;

        private bool isWpf;

        private bool isSelected;

        private string createResult;

        public string CompilerID { get; set; }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                SetProperty(ref name, value, "Name");
            }
        }

        public string SaveName
        {
            get
            {
                return saveName;
            }
            set
            {
                SetProperty(ref saveName, value, "SaveName");
            }
        }

        public string UninstallName
        {
            get
            {
                return uninstallName;
            }
            set
            {
                SetProperty(ref uninstallName, value, "UninstallName");
            }
        }

        public string SetupNSISPath
        {
            get
            {
                return setupNSISPath;
            }
            set
            {
                SetProperty(ref setupNSISPath, value, "SetupNSISPath");
            }
        }

        public string SetupIconPath
        {
            get
            {
                return setupIconPath;
            }
            set
            {
                SetProperty(ref setupIconPath, value, "SetupIconPath");
            }
        }

        public string UninstallNSISPath
        {
            get
            {
                return uninstallNSISPath;
            }
            set
            {
                SetProperty(ref uninstallNSISPath, value, "UninstallNSISPath");
            }
        }

        public string UninstallIconPath
        {
            get
            {
                return uninstallIconPath;
            }
            set
            {
                SetProperty(ref uninstallIconPath, value, "UninstallIconPath");
            }
        }

        public string UninstallEXESavePath
        {
            get
            {
                return uninstallEXESavePath;
            }
            set
            {
                SetProperty(ref uninstallEXESavePath, value, "UninstallEXESavePath");
            }
        }

        public string WpfResourcePath
        {
            get
            {
                return wpfReouscePath;
            }
            set
            {
                SetProperty(ref wpfReouscePath, value, "WpfResourcePath");
            }
        }

        public string CompilerName
        {
            get
            {
                return compilerName;
            }
            set
            {
                SetProperty(ref compilerName, value, "CompilerName");
            }
        }
        public string WpfOutPutPath
        {
            get
            {
                return wpfOutPutPath;
            }
            set
            {
                SetProperty(ref wpfOutPutPath, value, "WpfOutPutPath");
            }
        }
        public string WpfSlnPath
        {
            get
            {
                return wpfSlnPath;
            }
            set
            {
                SetProperty(ref wpfSlnPath, value, "WpfSlnPath");
            }
        }
        public bool IsWpf
        {
            get
            {
                return isWpf;
            }
            set
            {
                SetProperty(ref isWpf, value, "IsWpf");
            }
        }

        public bool IsSaved
        {
            get
            {
                return isSaved;
            }
            set
            {
                SetProperty(ref isSaved, value, "IsSaved");
            }
        }

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                SetProperty(ref isSelected, value, "IsSelected");
            }
        }

        public string CreateResult
        {
            get
            {
                return createResult;
            }
            set
            {
                SetProperty(ref createResult, value, "CreateResult");
            }
        }
        public bool IsGemoo
        {
            get
            {
                return isGemoo;
            }
            set
            {
                SetProperty(ref isGemoo, value, "IsGemoo");
            }
        }
        private bool isGemoo = true;

        public bool IsAny
        {
            get
            {
                return isAny;
            }
            set
            {
                SetProperty(ref isAny, value, "IsAny");
            }
        }
        private bool isAny;

        public DelegateCommand<object> SelectFolderCommand { get; }

        public DelegateCommand<object> SelectFileCommand { get; }

        public DelegateCommand<SetupSignatureModel> SelectCompilerCommand { get; set; }

        public SetupSignatureModel()
        {
            SelectFolderCommand = new DelegateCommand<object>(OnSelectFolder);
            SelectFileCommand = new DelegateCommand<object>(OnSelectFile);
        }

        private void OnSelectFile(object obj)
        {
            string text = obj?.ToString();
            (string, string) filter = default((string, string));
            filter.Item1 = "";
            filter.Item2 = "";
            switch (text)
            {
                case "install":
                    filter.Item1 = "安装包源文件";
                    filter.Item2 = "*.nsi";
                    break;
                case "uninstall":
                    filter.Item1 = "卸载包源文件";
                    filter.Item2 = "*.nsi";
                    break;
                case "installicon":
                    filter.Item1 = "安装包图标";
                    filter.Item2 = "*.ico";
                    break;
                case "uninstallicon":
                    filter.Item1 = "卸载包图标";
                    filter.Item2 = "*.ico";
                    break;
                case "WPFsln":
                    filter.Item1 = "Sln工程文件";
                    filter.Item2 = "*.Sln";
                    break;
            }
            string value = ChooseDialogTool.OpenSelectFileDialog(filter, multiSelect: false);
            if (!value.IsNullOrEmpty())
            {
                switch (text)
                {
                    case "install":
                        SetupNSISPath = value;
                        break;
                    case "uninstall":
                        UninstallNSISPath = value;
                        break;
                    case "installicon":
                        SetupIconPath = value;
                        break;
                    case "uninstallicon":
                        UninstallIconPath = value;
                        break;
                    case "WPFsln":
                        WpfSlnPath = value;
                        break;
                }
                IsSaved = false;
            }
        }

        private void OnSelectFolder(object obj)
        {
            string param = obj?.ToString();
            string startPath = "";
            switch (param)
            {
                case "WPFOutput":
                    startPath = WpfOutPutPath;

                    break;
                case "WPFResource":
                    startPath = WpfResourcePath;

                    break;
                default:
                    startPath = UninstallEXESavePath;
                    break;
            }
            string text = ChooseDialogTool.OpenSelectFolderDialog(startPath);
            if (!text.IsNullOrEmpty())
            {
                switch (param)
                {
                    case "WPFOutput":
                        WpfOutPutPath = text;
                        break;
                    case "WPFResource":
                        WpfResourcePath = text;

                        break;
                    default:
                        if (text.Contains(" "))
                        {
                            TipsTool.OpenTipsWindow("卸载包的保存路径不可以包含空格", "警告", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                        UninstallEXESavePath = text;
                        break;
                }
                IsSaved = false;
            }
        }

        internal bool VerifyWhileCreateUninstall()
        {
            CompilerSettingModel compilerByID = CompilerTool.Instance.GetCompilerByID(CompilerID);
          
            if (compilerByID == null)
            {
                WriteLog("未找到编译器<" + CompilerName + ">");
                return false;
            }
            if (!compilerByID.IsAvailable)
            {
                return false;
            }
            if (isWpf)
            {
                WriteLog("拷贝Wpf EXE");
                if (!WpfOutPutPath.IsExistsFolder())
                {
                    WriteLog("Exe<" + WpfOutPutPath + ">不存在");
                    return false;
                }
                string installexePath = Path.Combine(WpfOutPutPath, "setup.exe");
                string uninstallexePath = Path.Combine(WpfOutPutPath, "uninstall.exe");
                string targetResPath = Path.Combine(WpfResourcePath, "setup.exe");
                if (FileTool.CopyFile(installexePath, targetResPath, delegate (Exception error)
                {
                    WriteLog("拷贝失败：" + error.Message);
                }) != 0)
                {

                }
                targetResPath = Path.Combine(WpfResourcePath, "uninstall.exe");
                if (FileTool.CopyFile(uninstallexePath, targetResPath, delegate (Exception error)
                {
                    WriteLog("拷贝失败：" + error.Message);
                }) != 0)
                {

                }

            }

            WriteLog("拷贝卸载包图标");
            if (!UninstallIconPath.IsExistsFile())
            {
                WriteLog("图标<" + UninstallIconPath + ">不存在");
                return false;
            }
            string[] array = compilerByID.replaceIconName.Split(new string[1] { ";" }, StringSplitOptions.None);
            foreach (string p in array)
            {
                string targetPath = compilerByID.compilerIconSavePath.Combine(p);
                if (FileTool.CopyFile(uninstallIconPath, targetPath, delegate (Exception error)
                {
                    WriteLog("拷贝失败：" + error.Message);
                }) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        internal bool VerifyWhileCreateSetup()
        {
            CompilerSettingModel compilerByID = CompilerTool.Instance.GetCompilerByID(CompilerID);
            WriteLog("拷贝安装包图标");
            if (!SetupIconPath.IsExistsFile())
            {
                WriteLog("图标<" + SetupIconPath + ">不存在");
                return false;
            }
            string[] array = compilerByID.replaceIconName.Split(new string[1] { ";" }, StringSplitOptions.None);
            foreach (string p in array)
            {
                string targetPath = compilerByID.compilerIconSavePath.Combine(p);
                if (FileTool.CopyFile(SetupIconPath, targetPath, delegate (Exception error)
                {
                    WriteLog("拷贝失败：" + error.Message);
                }) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        private void WriteLog(string msg)
        {
            Trace.TraceWarning("<" + Name + "> -> " + msg);
        }

        internal string CreateBuildArg()
        {
            return $"/c devenv \"{WpfSlnPath}\" /build \"Release|AnyCPU\"";
        }
    } 
}
