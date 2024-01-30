// SignTool.ViewModel.Setting.CompilerModel
using System;
using System.IO;
using Prism.Commands;
using SignatureTool2;
using SignatureTool2.Utilites.DialogWindow;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.ViewModel.Setting;

namespace SignatureTool2.ViewModel.Setting
{
    public class CompilerModel : ViewModelBase
    {
        private string name;

        private string compilerPath;

        private string compilerIconSavePath;

        private string replaceIconName;

        private string commandParameter;

        private bool isSaved;

        private bool isSelected;

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

        public string CompilerPath
        {
            get
            {
                return compilerPath;
            }
            set
            {
                SetProperty(ref compilerPath, value, "CompilerPath");
            }
        }

        public string CompilerIconSavePath
        {
            get
            {
                return compilerIconSavePath;
            }
            set
            {
                SetProperty(ref compilerIconSavePath, value, "CompilerIconSavePath");
            }
        }

        public string ReplaceIconName
        {
            get
            {
                return replaceIconName;
            }
            set
            {
                SetProperty(ref replaceIconName, value, "ReplaceIconName");
            }
        }

        public string CommandParameter
        {
            get
            {
                return commandParameter;
            }
            set
            {
                SetProperty(ref commandParameter, value, "CommandParameter");
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
                if (SetProperty(ref isSelected, value, "IsSelected"))
                {
                    this.SelectionChanged?.Invoke(this);
                }
            }
        }

        public DelegateCommand SelectFolderCommand { get; }

        public DelegateCommand SelectFileCommand { get; }

        public event Action<CompilerModel> SelectionChanged;

        public CompilerModel()
        {
            CompilerID = Guid.NewGuid().ToString("N");
            SelectFolderCommand = new DelegateCommand(OnSelectFolder);
            SelectFileCommand = new DelegateCommand(OnSelectFile);
            ReplaceIconName = "modern-install.ico;modern-install-full.ico";
            commandParameter = "cmd /c \" \"{0}\" \"{1}\" /X\"OutFile {2} \"";
        }

        private void OnSelectFile()
        {
            string text = ChooseDialogTool.OpenSelectFileDialog(("选择编译器", "*.exe"), multiSelect: false);
            if (!text.IsNullOrEmpty())
            {
                CompilerPath = text;
                string path = Directory.GetParent(text).FullName.Combine("Contrib\\Graphics\\Icons");
                if (Directory.Exists(path))
                {
                    CompilerIconSavePath = path;
                }
                IsSaved = false;
            }
        }

        private void OnSelectFolder()
        {
            string text = CompilerIconSavePath;
            if (text.IsNullOrEmpty() && !CompilerPath.IsNullOrEmpty())
            {
                text = Directory.GetParent(CompilerPath).FullName.Combine("Contrib\\Graphics\\Icons");
                if (!text.IsExistsFolder())
                {
                    text = Directory.GetParent(CompilerPath).FullName;
                }
            }
            string value = ChooseDialogTool.OpenSelectFolderDialog(text);
            if (!value.IsNullOrEmpty())
            {
                CompilerIconSavePath = value;
                IsSaved = false;
            }
        }
    } 
}
