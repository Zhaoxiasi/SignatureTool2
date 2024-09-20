// SignTool.ViewModel.Setting.ProtecterSettingViewModel
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using Prism.Commands;
using SignatureTool2;
using SignatureTool2.Utilites;
using SignatureTool2.Utilites.DialogWindow;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Log;
using SignatureTool2.ViewModel.Setting;
namespace SignatureTool2.ViewModel.Setting
{

    public class CompanySettingViewModel : ViewModelBase
    {
        private List<string> _errorList = new List<string>();

        private CompanyModel selectedCompany;

        private string errorText;

        public CompanyModel SelectedCompany
        {
            get
            {
                return selectedCompany;
            }
            set
            {
                SetProperty(ref selectedCompany, value, "SelectedCompany");
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

        public ObservableCollection<CompanyModel> DataList { get; }

        public DelegateCommand AddCommand { get; }

        public DelegateCommand DeleteCommand { get; }

        public DelegateCommand SaveCommand { get; }

        public CompanySettingViewModel()
        {
            AddCommand = new DelegateCommand(OnAdd);
            DeleteCommand = new DelegateCommand(OnDelete);
            SaveCommand = new DelegateCommand(OnSave);
            DataList = new ObservableCollection<CompanyModel>();
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
            List<CompanyModel> list = new List<CompanyModel>();
            foreach (CompanyModel data in DataList)
            {
                data.IsSaved = true;
                CompanyModel item = new CompanyModel
                {
                    CompanyID= data.CompanyID,
                    Name = data.Name,
                    Sha1= data.Sha1,
                    Password = data.Password,
                };
                list.Add(item);
            }
            CompanyTool.Instance.Save(list);
        }

        private void OnDelete()
        {
            if (DataList.FirstOrDefault((CompanyModel p) => p.IsSelected) == null || TipsTool.OpenTipsWindow("确认删除选中项？", "删除", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
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
            foreach (CompanyModel data in DataList)
            {
                data.IsSelected = false;
            }
            CompanyModel item = new CompanyModel
            {
                Name = "新增公司",
                IsSelected = true,
                Sha1 = "输入证书指纹",
                Password = "请输入整数Token密码"
            };
            DataList.Add(item);
        }

        private void ReadConfig()
        {
            foreach (CompanyModel compilerSetting in CompanyTool.Instance.CompanySettingList)
            {
                CompanyModel item = new CompanyModel
                {
                    CompanyID = compilerSetting.CompanyID,
                    Name = compilerSetting.Name,
                    Sha1 = compilerSetting.Sha1,
                    Password = compilerSetting.Password,
                    IsSaved = true,
                };
                DataList.Add(item);
            }
            using (IEnumerator<CompanyModel> enumerator2 = DataList.GetEnumerator())
            {
                if (enumerator2.MoveNext())
                {
                    enumerator2.Current.IsSelected = true;
                }
            }
            DataList.OrderBy((CompanyModel p) => p.Name);
        }
    }
    public class CompanyModel : ViewModelBase
    {
        private string name;
        private string _sha1;
        private string _password;

        private bool isSaved;

        private bool isSelected;

        public string CompanyID { get; set; }

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

        public string Sha1
        {
            get
            {
                return _sha1;
            }
            set
            {
                SetProperty(ref _sha1, value, "Sha1");
            }
        }

        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                SetProperty(ref _password, value, "Password");
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


        public event Action<CompanyModel> SelectionChanged;

        public CompanyModel()
        {
            CompanyID = Guid.NewGuid().ToString("N");
        }

        private void OnSelectFolder()
        {

        }
    }

}