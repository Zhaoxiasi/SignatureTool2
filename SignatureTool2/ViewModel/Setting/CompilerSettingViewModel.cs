// SignTool.ViewModel.Setting.CompilerSettingViewModel
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using Prism.Commands;
using SignatureTool2;
using SignatureTool2.Utilites;
using SignatureTool2.Utilites.DialogWindow;
using SignatureTool2.Utilites.Log;
using SignatureTool2.ViewModel.Setting;
namespace SignatureTool2.ViewModel.Setting
{

    public class CompilerSettingViewModel : ViewModelBase
    {
        private List<string> _errorList = new List<string>();

        private CompilerModel selectedCompiler;

        private string errorText;

        public CompilerModel SelectedCompiler
        {
            get
            {
                return selectedCompiler;
            }
            set
            {
                SetProperty(ref selectedCompiler, value, "SelectedCompiler");
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

        public ObservableCollection<CompilerModel> DataList { get; }

        public DelegateCommand AddCommand { get; }

        public DelegateCommand DeleteCommand { get; }

        public DelegateCommand SaveCommand { get; }

        public CompilerSettingViewModel()
        {
            AddCommand = new DelegateCommand(OnAdd);
            DeleteCommand = new DelegateCommand(OnDelete);
            SaveCommand = new DelegateCommand(OnSave);
            DataList = new ObservableCollection<CompilerModel>();
            ReadConfig();
            LogTool.Instance.WriteLogEvent += Instance_WriteLogEvent;
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
            List<CompilerSettingModel> list = new List<CompilerSettingModel>();
            foreach (CompilerModel data in DataList)
            {
                data.IsSaved = true;
                CompilerSettingModel item = new CompilerSettingModel
                {
                    commandParameter = data.CommandParameter,
                    compilerIconSavePath = data.CompilerIconSavePath,
                    compilerID = data.CompilerID,
                    compilerPath = data.CompilerPath,
                    name = data.Name,
                    replaceIconName = data.ReplaceIconName
                };
                list.Add(item);
            }
            CompilerTool.Instance.Save(list);
        }

        private void OnDelete()
        {
            if (DataList.FirstOrDefault((CompilerModel p) => p.IsSelected) == null || TipsTool.OpenTipsWindow("确认删除选中项？", "删除", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
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
            foreach (CompilerModel data in DataList)
            {
                data.IsSelected = false;
            }
            CompilerModel item = new CompilerModel
            {
                Name = "新增编译器",
                IsSelected = true
            };
            DataList.Add(item);
        }

        private void ReadConfig()
        {
            foreach (CompilerSettingModel compilerSetting in CompilerTool.Instance.CompilerSettingList)
            {
                CompilerModel item = new CompilerModel
                {
                    CommandParameter = compilerSetting.commandParameter,
                    CompilerIconSavePath = compilerSetting.compilerIconSavePath,
                    CompilerID = compilerSetting.compilerID,
                    CompilerPath = compilerSetting.compilerPath,
                    ReplaceIconName = compilerSetting.replaceIconName,
                    Name = compilerSetting.name,
                    IsSaved = true
                };
                DataList.Add(item);
            }
            using (IEnumerator<CompilerModel> enumerator2 = DataList.GetEnumerator())
            {
                if (enumerator2.MoveNext())
                {
                    enumerator2.Current.IsSelected = true;
                }
            }
            DataList.OrderBy((CompilerModel p) => p.Name);
        }
    }

}