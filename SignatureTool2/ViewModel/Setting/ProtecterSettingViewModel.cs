// SignTool.ViewModel.Setting.ProtecterSettingViewModel
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

    public class ProtecterSettingViewModel : ViewModelBase
    {
        private List<string> _errorList = new List<string>();

        private ProtecterModel selectedProtecter;

        private string errorText;

        public ProtecterModel SelectedProtecter
        {
            get
            {
                return selectedProtecter;
            }
            set
            {
                SetProperty(ref selectedProtecter, value, "SelectedProtecter");
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

        public ObservableCollection<ProtecterModel> DataList { get; }

        public DelegateCommand AddCommand { get; }

        public DelegateCommand DeleteCommand { get; }

        public DelegateCommand SaveCommand { get; }

        public ProtecterSettingViewModel()
        {
            AddCommand = new DelegateCommand(OnAdd);
            DeleteCommand = new DelegateCommand(OnDelete);
            SaveCommand = new DelegateCommand(OnSave);
            DataList = new ObservableCollection<ProtecterModel>();
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
            List<ProtecterSettingModel> list = new List<ProtecterSettingModel>();
            foreach (ProtecterModel data in DataList)
            {
                data.IsSaved = true;
                ProtecterSettingModel item = new ProtecterSettingModel
                {
                    protecterID = data.ProtecterID,
                    protecterPath = data.ProtecterPath,
                    name = data.Name,
                };
                list.Add(item);
            }
            ProtecterTool.Instance.Save(list);
        }

        private void OnDelete()
        {
            if (DataList.FirstOrDefault((ProtecterModel p) => p.IsSelected) == null || TipsTool.OpenTipsWindow("确认删除选中项？", "删除", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
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
            foreach (ProtecterModel data in DataList)
            {
                data.IsSelected = false;
            }
            ProtecterModel item = new ProtecterModel
            {
                Name = "新增编译器",
                IsSelected = true
            };
            DataList.Add(item);
        }

        private void ReadConfig()
        {
            foreach (ProtecterSettingModel compilerSetting in ProtecterTool.Instance.ProtecterSettingList)
            {
                ProtecterModel item = new ProtecterModel
                {
                    ProtecterID = compilerSetting.protecterID,
                    ProtecterPath = compilerSetting.protecterPath,
                    Name = compilerSetting.name,
                    IsSaved = true
                };
                DataList.Add(item);
            }
            using (IEnumerator<ProtecterModel> enumerator2 = DataList.GetEnumerator())
            {
                if (enumerator2.MoveNext())
                {
                    enumerator2.Current.IsSelected = true;
                }
            }
            DataList.OrderBy((ProtecterModel p) => p.Name);
        }
    }

}