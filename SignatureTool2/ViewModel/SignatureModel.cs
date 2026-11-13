// SignTool.ViewModel.SignatureViewModel
using Prism.Commands;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Sign;
using SignatureTool2.ViewModel.Signature;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace SignatureTool2.ViewModel
{
	public class SignatureModel : ViewModelBase
	{
		private string softwareName;

		private string excutableName;

		private bool isSelected;

		private string sourcePath;

		private string targetPath;

		private string company;

        private string _buildArguments = "devenv \"{输入你的sln地址}\" /build \"Release|Mixed Platforms\"";
        private string _productInfoPath = "输入你的ProductInfo地址";

		public DateTime LastSignTime { get; set; }
        public DateTime LastBiuldTime { get; set; }

		public List<CopyFileModel> CopyList { get; }
		public List<SignModel> SignModelList { get; set; }

		public bool IsNew { get; set; } = true;

		private string statusText;

		/// <summary>本次运行状态（构建失败/签名成功等），不持久化。</summary>
		public string StatusText
		{
			get
			{
				return statusText;
			}
			set
			{
				if (SetProperty(ref statusText, value, "StatusText"))
				{
					RaisePropertyChanged("DisplayName");
				}
			}
		}

		/// <summary>列表项显示名：名称 + 状态后缀。</summary>
		public string DisplayName => string.IsNullOrEmpty(StatusText) ? SoftwareName : $"{SoftwareName}（{StatusText}）";

		private string syncMode = "增量";

		/// <summary>同步模式：“增量”=只拷贝新文件；“严格同步”=全量拷贝并清理目标目录多余文件。</summary>
		public string SyncMode
		{
			get
			{
				return syncMode;
			}
			set
			{
				SetProperty(ref syncMode, value, "SyncMode");
			}
		}

		/// <summary>严格同步时用户勾选保留的文件（不持久化）。</summary>
		public HashSet<string> SyncKeepFiles { get; set; }

		/// <summary>是否严格同步模式。</summary>
		public bool IsStrictSync => "严格同步".Equals(SyncMode, StringComparison.Ordinal);

		private string fileNameTemplate = "";

		/// <summary>压缩包文件名模板；留空使用全局默认模板。</summary>
		public string FileNameTemplate
		{
			get
			{
				return fileNameTemplate;
			}
			set
			{
				SetProperty(ref fileNameTemplate, value, "FileNameTemplate");
			}
		}

		private bool verifySign;

		/// <summary>打包后是否校验目标目录全部已签名。</summary>
		public bool VerifySign
		{
			get
			{
				return verifySign;
			}
			set
			{
				SetProperty(ref verifySign, value, "VerifySign");
			}
		}

		private bool packageMsi;

		/// <summary>是否在压缩后执行微软商店(MSI)打包：同步目标目录到Msi源目录 → AIP设版本 → 默认构建。</summary>
		public bool PackageMsi
		{
			get
			{
				return packageMsi;
			}
			set
			{
				SetProperty(ref packageMsi, value, "PackageMsi");
			}
		}

		private string msiVersion = "";

		/// <summary>写入 AIP 工程的版本号，如 1.2.3；留空则跳过设置版本。</summary>
		public string MsiVersion
		{
			get
			{
				return msiVersion;
			}
			set
			{
				SetProperty(ref msiVersion, value, "MsiVersion");
			}
		}

		private string msiSourcePath = "";

		/// <summary>Msi源文件夹路径：目标路径内容完全同步到此（保留track.txt），AIP基于此目录打包。</summary>
		public string MsiSourcePath
		{
			get
			{
				return msiSourcePath;
			}
			set
			{
				SetProperty(ref msiSourcePath, value, "MsiSourcePath");
			}
		}

		private string msiAipPath = "";

		/// <summary>Advanced Installer 工程文件(.aip)路径。</summary>
		public string MsiAipPath
		{
			get
			{
				return msiAipPath;
			}
			set
			{
				SetProperty(ref msiAipPath, value, "MsiAipPath");
			}
		}

		private string msiToolPath = "";

		/// <summary>AdvancedInstaller.com 完整路径。</summary>
		public string MsiToolPath
		{
			get
			{
				return msiToolPath;
			}
			set
			{
				SetProperty(ref msiToolPath, value, "MsiToolPath");
			}
		}

		private string copyTargets = "";

		/// <summary>打包后拷贝压缩包到本地目录，每行一个。</summary>
		public string CopyTargets
		{
			get
			{
				return copyTargets;
			}
			set
			{
				SetProperty(ref copyTargets, value, "CopyTargets");
			}
		}

		private string ftpTargets = "";

		/// <summary>打包后上传压缩包的 FTP 地址，每行一个，支持 ftp://user:pass@host/path。</summary>
		public string FtpTargets
		{
			get
			{
				return ftpTargets;
			}
			set
			{
				SetProperty(ref ftpTargets, value, "FtpTargets");
			}
		}

		/// <summary>拆分多行配置为非空行列表。</summary>
		public List<string> SplitTargets(string multiline)
		{
			List<string> result = new List<string>();
			if (string.IsNullOrWhiteSpace(multiline))
			{
				return result;
			}
			foreach (string line in multiline.Replace("\r\n", "\n").Split('\n'))
			{
				string trimmed = line.Trim();
				if (trimmed.Length > 0)
				{
					result.Add(trimmed);
				}
			}
			return result;
		}


		public string SoftwareName
		{
			get
			{
				return softwareName;
			}
			set
			{
				if (SetProperty(ref softwareName, value, "SoftwareName"))
				{
					RaisePropertyChanged("DisplayName");
				}
			}
		}

		public string Company
		{
			get
			{
				return company;
			}
			set
			{
				SetProperty(ref company, value, "Company");
			}
		}

		public string ExcutableName
		{
			get
			{
				return excutableName;
			}
			set
			{
				SetProperty(ref excutableName, value, "ExcutableName");
			}
		}

		public string ProductInfoPath
		{
			get
			{
				return _productInfoPath;
			}
			set
			{
				SetProperty(ref _productInfoPath, value, "ProductInfoPath");
			}
		}
		public string BiuldArgument
		{
			get
			{
				return _buildArguments;
			}
			set
			{
				SetProperty(ref _buildArguments, value, "BiuldArgument");
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

		public string SourcePath
		{
			get
			{
				return sourcePath;
			}
			set
			{
				SetProperty(ref sourcePath, value, "SourcePath");
			}
		}

		public string TargetPath
		{
			get
			{
				return targetPath;
			}
			set
			{
				SetProperty(ref targetPath, value, "TargetPath");
			}
        }
        public string ProtecterName
        {
            get
            {
                return protecterName;
            }
            set
            {
                SetProperty(ref protecterName, value, "ProtecterName");
            }
        }
        public string ProtectProject
        {
            get
            {
                return protecterProject;
            }
            set
            {
                SetProperty(ref protecterProject, value, "ProtectProject");
            }
        }
        public string ProtecterID { get; internal set; }
        public string CompanyID { get; internal set; }
		public string protecterName;

        public bool IsAvailable
		{
			get
			{
				if (!IsNew)
				{
					return true;
				}
				if (sourcePath.IsExistsFolder())
				{
					return targetPath.IsExistsFolder();
				}
				return false;
            }
        }
        public DelegateCommand<SignatureModel> SelectProtecterCommand { get; set; }
        public DelegateCommand<SignatureModel> SelectCompanyCommand { get; set; }
		public string protecterProject;

        public SignatureModel()
        {
            CopyList = new List<CopyFileModel>();
        }

        public bool checkNeedBuild() {

			string exePath = Path.Combine(sourcePath, ExcutableName);
            if (File.Exists(exePath))
            {
                FileInfo info = new FileInfo(exePath);
                if ((LastBiuldTime - info.LastWriteTime).TotalSeconds > 60)
                {
                    return true;
                }
                else
                {
                    if (LastBiuldTime == DateTime.MinValue)
                    {
                        if ((DateTime.Now - info.LastWriteTime).TotalSeconds < 120)
                        {
                            return false;
                        }

                    }
                }
                info = null;
			}
			return true;
		}

        

        internal List<SignModel> CreateModels(List<string> allFiles)
        {
			if(SignModelList==null)
            SignModelList = SignatureTool.CreateSignModel(allFiles, CompanyID);
			return SignModelList;
        }
    }

}