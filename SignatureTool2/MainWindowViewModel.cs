// SignTool.MainWindowViewModel
using SignatureTool2.View;
using SignatureTool2.View.Setting;
using SignatureTool2.View.Signature;
using SignatureTool2.ViewModel;
using SignatureTool2.ViewModel.Setting;
using SignatureTool2.ViewModel.Signature;
using SnappyWinscard;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SignatureTool2
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<TabModel> Tables { get; }

        public MainWindowViewModel()
        {
            Tables = new ObservableCollection<TabModel>();
            Init();
        }

        private void Init()
        {
            TabModel tabModel = new TabModel
            {
                IsSelected = true,
                TabName = "批量签名"
            };
            SignatureView signatureView = new SignatureView();
            signatureView.DataContext = new SignatureViewModel();
            tabModel.TabContent = signatureView;
            Tables.Add(tabModel);
            tabModel = new TabModel
            {
                TabName = "自定义签名"
            };
            SelectFileSignatureView selectFileSignatureView = new SelectFileSignatureView();
            selectFileSignatureView.DataContext = new SelectFileSignatureViewModel();
            tabModel.TabContent = selectFileSignatureView;
            Tables.Add(tabModel);
            tabModel = new TabModel
            {
                TabName = "编译安装包"
            };
            SetupSignatureView setupSignatureView = new SetupSignatureView();
            setupSignatureView.DataContext = new SetupSignatureViewModel();
            tabModel.TabContent = setupSignatureView;
            Tables.Add(tabModel);
            tabModel = new TabModel
            {
                TabName = "编译器设置"
            };
            CompilerSettingView compilerSettingView = new CompilerSettingView();
            compilerSettingView.DataContext = new CompilerSettingViewModel();
            tabModel.TabContent = compilerSettingView;
            Tables.Add(tabModel); 
            tabModel = new TabModel
            {
                TabName = "混淆器设置"
            };
            ProtecterSettingView protecterSettingView = new ProtecterSettingView();
            protecterSettingView.DataContext = new ProtecterSettingViewModel();
            tabModel.TabContent = protecterSettingView;
            Tables.Add(tabModel);
            tabModel = new TabModel
            {
                TabName = "公司设置"
            };
            CompanySettingView companySettingView = new CompanySettingView();
            companySettingView.DataContext = new CompanySettingViewModel();
            tabModel.TabContent = companySettingView;
            Tables.Add(tabModel);
        }
    }

}
