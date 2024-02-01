// SignTool.ViewModel.SignatureViewModel
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Sign;
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


		public string SoftwareName
		{
			get
			{
				return softwareName;
			}
			set
			{
				SetProperty(ref softwareName, value, "SoftwareName");
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
            SignModelList = SignatureTool.CreateSignModel(allFiles);
			return SignModelList;
        }
    }

}