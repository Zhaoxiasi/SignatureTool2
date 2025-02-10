// SignTool.ViewModel.SelectFileSignatureViewModel
#define TRACE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using SignatureTool2.Utilites.Log;
using SignatureTool2.Utilites.Sign;
using SignatureTool2.ViewModel;
using SignatureTool2.ViewModel.Setting;

namespace SignatureTool2.ViewModel
{
    public class SelectFileSignatureViewModel : SignBaseViewModel
    {
        private bool _isSkip;

        private List<string> _errorList = new List<string>();

        private ConcurrentQueue<SignatureTool> _signList;

        private string errorText;

        private bool isEnabled = true;
        private bool isSignGemoo = true;
        private bool isSigniMobie = false;

        private string signTip;

        private string totalCount = "0";

        private Visibility loadingVisibility = Visibility.Collapsed;

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
        public bool IsSigniMobie
        {
            get
            {
                return isSigniMobie;
            }
            set
            {
                SetProperty(ref isSigniMobie, value, "IsSigniMobie");
            }
        }
        public bool IsSignGemoo
        {
            get
            {
                return isSignGemoo;
            }
            set
            {
                SetProperty(ref isSignGemoo, value, "IsSignGemoo");
            }
        }

        public string SignTip
        {
            get
            {
                return signTip;
            }
            set
            {
                SetProperty(ref signTip, value, "SignTip");
            }
        }

        public string TotalCount
        {
            get
            {
                return totalCount;
            }
            set
            {
                SetProperty(ref totalCount, value, "TotalCount");
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

        public ObservableCollection<SignFileModel> FileList { get; }

        public ObservableCollection<CompanyModel> Companies { get; }
        private CompanyModel _selectCompany;

        public CompanyModel SelectCompany
        {
            get => _selectCompany;
            set => SetProperty(ref _selectCompany, value);
        }


        public ICommand ChoseFolderCommand { get; }

        public ICommand ChoseFileCommand { get; }

        public DelegateCommand<string> RemoveItemCommand { get; }


        public SelectFileSignatureViewModel()
        {
            ChoseFolderCommand = new DelegateCommand(OnChoseFolderCommand);
            ChoseFileCommand = new DelegateCommand(OnChoseFileCommand);
            RemoveItemCommand = new DelegateCommand<string>(OnRemoveItemCommand);
            SignCommand = new DelegateCommand<string>(OnSignCommand);
            StopCommand = new DelegateCommand(OnStopCommand);
            FileList = new ObservableCollection<SignFileModel>();
            _signList = new ConcurrentQueue<SignatureTool>();
            Companies = new ObservableCollection<CompanyModel>();
            Companies.AddRange(CompanyTool.Instance.CompanySettingList);
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

        private void OnSignCommand(string pa)
        {
            if (FileList.Count != 0)
            {
                //base.OnSign("");
                _errorList.Clear();
                List<SignFileModel> li = (!(pa == "all")) ? FileList.ToList().FindAll((SignFileModel p) => p.IsSelected) : FileList.ToList();
                Package(li);
            }
        }

        private void Package(List<SignFileModel> li)
        {
            if (li.Count == 0)
            {
                return;
            }
            SignTip = "0/" + totalCount;
            _isSkip = false;
            IsEnabled = false;
            li.ForEach(delegate (SignFileModel p)
            {
                p.SignStatus = "";
            });
            string SignSha1 = "";
            if (SelectCompany != null)
            {
                SignSha1 = SelectCompany.Sha1;
                Clipboard.SetDataObject(SelectCompany.Password);

                Trace.TraceInformation($"Signature Password Copied!Company:<{SelectCompany.Name}>");
            }
            //if (IsSignGemoo)
            //{
            //    var company = CompanyTool.Instance.GetCompanyByName("Gemoo");
            //    if (company != null)
            //    {
            //        SignSha1 = company.Sha1;
            //        Clipboard.SetDataObject(company.Password);
            //        Trace.TraceInformation($"Signature Password Copied!Company:<{company.Name}>");
            //    }
            //}
            //else if (IsSigniMobie)
            //{
            //    var company = CompanyTool.Instance.GetCompanyByName("iMobie2024");
            //    if (company != null)
            //    {
            //        SignSha1 = company.Sha1;
            //        Clipboard.SetDataObject(company.Password);
            //        Trace.TraceInformation($"Signature Password Copied!Company:<{company.Name}>");
            //    }
            //}
            LoadingVisibility = Visibility.Visible;
            Trace.TraceInformation($"signature total {li.Count} file(s)");
            new Thread((ThreadStart)delegate
            {
                int successCount = 0;
                int currentCount = 0;
                int totalCount = li.Count;
                SemaphoreTool semaphoreTool = new SemaphoreTool(li.Count, 5);
                foreach (SignFileModel item in li)
                {
                    if (_isSkip)
                    {
                        semaphoreTool.SkipThread();
                        break;
                    }
                    semaphoreTool.InvokeByTask(delegate (object par)
                    {
                        SignFileModel signFileModel = (SignFileModel)par;
                        SignatureTool signatureTool = new SignatureTool();
                        _signList.Enqueue(signatureTool);
                        SignModel model = new SignModel
                        {
                            FilePath = signFileModel.FilePath,
                            sha1 = SignSha1
                        };
                        int num = signatureTool.SignatureFile(model);
                        Interlocked.Increment(ref currentCount);
                        SignTip = $"{currentCount}/{totalCount}";
                        if (num == 1)
                        {
                            Interlocked.Increment(ref successCount);
                            signFileModel.SignStatus = "成功！";
                        }
                        else
                        {
                            signFileModel.SignStatus = "签名失败！";
                        }
                    }, item);
                }
                semaphoreTool.WaitAllFinish();
                IsEnabled = true;
                LoadingVisibility = Visibility.Collapsed;
                Trace.TraceInformation($"所有签名完成，总共{li.Count}项，成功{successCount}项，失败{li.Count - successCount}项！");
            }).Start();
        }

        public void OnRemoveItemCommand(string pa)
        {
            if (pa == "all")
            {
                FileList.Clear();
            }
            else
            {
                for (int i = 0; i < FileList.Count; i++)
                {
                    if (FileList[i].IsSelected)
                    {
                        FileList.RemoveAt(i);
                        i--;
                    }
                }
            }
            TotalCount = FileList.Count.ToString();
        }

        private void OnChoseFileCommand()
        {
            List<string> supportSignFile = SignatureTool.SupportSignFile;
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < supportSignFile.Count; i++)
            {
                stringBuilder.Append("*" + supportSignFile[i]);
                if (i < supportSignFile.Count - 1)
                {
                    stringBuilder.Append(";");
                }
            }
            List<string> list = ChooseDialogTool.OpenSelectFileDialog(new List<(string, string)> { ("支持类型", stringBuilder.ToString()) }, multiSelect: true);
            if (list == null)
            {
                return;
            }
            new List<SignFileModel>();
            foreach (string item in list)
            {
                SignFileModel signFileModel = new SignFileModel();
                signFileModel.FilePath = item;
                signFileModel.FileName = item.GetFileName();
                FileList.Add(signFileModel);
            }
            TotalCount = FileList.Count.ToString();
        }

        private void OnChoseFolderCommand()
        {
            string text = ChooseDialogTool.OpenSelectFolderDialog();
            if (!text.IsExistsFolder())
            {
                return;
            }
            List<string> allFiles = FolderTool.GetAllFiles(text);
            List<SignFileModel> list = new List<SignFileModel>();
            DateTime dateTime = DateTime.Now.Date;
            List<string> supportSignFile = SignatureTool.SupportSignFile;
            foreach (string item in allFiles)
            {
                DateTime lastWriteTime = new FileInfo(item).LastWriteTime;
                if (!(lastWriteTime < DateTime.Now.Date) && supportSignFile.Contains(item.GetExtension().ToLower()))
                {
                    if (dateTime < lastWriteTime)
                    {
                        dateTime = lastWriteTime;
                    }
                    SignFileModel signFileModel = new SignFileModel();
                    signFileModel.FilePath = item;
                    signFileModel.FileName = item.GetFileName();
                    list.Add(signFileModel);
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].FilePath.GetExtension().ToLower() == ".pdb")
                {
                    list.RemoveAt(i);
                    i--;
                }
                else
                {
                    FileList.Add(list[i]);
                }
            }
            TotalCount = FileList.Count.ToString();
        }

        internal void FileDroped(string[] li)
        {
            List<string> supportSignFile = SignatureTool.SupportSignFile;
            foreach (string item in li)
            {
                if (supportSignFile.Contains(item.GetExtension().ToLower()) && FileList.FirstOrDefault((SignFileModel p) => p.FilePath == item) == null)
                {
                    SignFileModel signFileModel = new SignFileModel();
                    signFileModel.FilePath = item;
                    signFileModel.FileName = item.GetFileName();
                    FileList.Add(signFileModel);
                }
            }
            TotalCount = FileList.Count.ToString();
        }
    } 
}
