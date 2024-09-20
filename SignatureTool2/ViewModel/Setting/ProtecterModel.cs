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
    public class ProtecterModel : ViewModelBase
    {
        private string name;

        private string protecterPath;

        private bool isSaved;

        private bool isSelected;

        public string ProtecterID { get; set; }

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

        public string ProtecterPath
        {
            get
            {
                return protecterPath;
            }
            set
            {
                SetProperty(ref protecterPath, value, "ProtecterPath");
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

        public event Action<ProtecterModel> SelectionChanged;

        public ProtecterModel()
        {
            ProtecterID = Guid.NewGuid().ToString("N");
            SelectFolderCommand = new DelegateCommand(OnSelectFolder);
            SelectFileCommand = new DelegateCommand(OnSelectFile);
        }

        private void OnSelectFile()
        {
            string text = ChooseDialogTool.OpenSelectFileDialog(("选择混淆器", "*.exe"), multiSelect: false);
            if (!text.IsNullOrEmpty())
            {
                ProtecterPath = text;
                IsSaved = false;
            }
        }

        private void OnSelectFolder()
        {
            
        }
    } 
}
