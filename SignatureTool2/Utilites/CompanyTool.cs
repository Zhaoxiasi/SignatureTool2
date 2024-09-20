// SignTool.Utilites.ProtecterTool
#define TRACE
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using SignatureTool2.Utilites;
using SignatureTool2.ViewModel.Setting;

namespace SignatureTool2.Utilites
{
    public class CompanyTool
    {
        private static CompanyTool protecterTool;

        private List<CompanyModel> settingList;

        public List<CompanyModel> CompanySettingList => settingList;

        public static CompanyTool Instance
        {
            get
            {
                if (protecterTool == null)
                {
                    protecterTool = new CompanyTool();
                }
                return protecterTool;
            }
        }

        private CompanyTool()
        {
            settingList = new List<CompanyModel>();
            ReadConfig();
        }

        private void ReadConfig()
        {
            List<object> valveOfRoot = ConfigTool.Instance.GetValveOfRoot<List<object>>(CRootKey.Company);
            if (valveOfRoot == null)
            {
                return;
            }
            settingList.Clear();
            ConfigTool instance = ConfigTool.Instance;
            foreach (object item2 in valveOfRoot)
            {
                Dictionary<string, object> dictionary = item2 as Dictionary<string, object>;
                if (dictionary != null)
                {
                    CompanyModel item = new CompanyModel
                    {
                        Password = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_CompanyPassword),
                        CompanyID = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_CompanyID),
                        Sha1 = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_CompanySha1),
                        Name = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_Name),
                        IsSaved = true,
                    };
                    settingList.Add(item);
                }
            }
        }

        public void Save(List<CompanyModel> li)
        {
            if (li == null)
            {
                return;
            }
            List<object> list = new List<object>();
            foreach (CompanyModel item in li)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_Name, item.Name);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_CompanyID, item.CompanyID);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_CompanySha1, item.Sha1);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_CompanyPassword, item.Password);
                list.Add(dictionary);
            }
            ConfigTool.Instance.SetValueOfRoot(CRootKey.Company, list);
            Trace.TraceInformation("保存配置成功！");
            ReadConfig();
        }

        internal CompanyModel GetCompanyByID(string companyID)
        {
            if (string.IsNullOrEmpty(companyID))
            {
                return settingList.FirstOrDefault();
            }
            return settingList.FirstOrDefault((CompanyModel p) => p.CompanyID == companyID);
        }

        internal CompanyModel GetCompanyByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return settingList.FirstOrDefault();
            }
            return settingList.FirstOrDefault((CompanyModel p) => p.Name == name);
        }

        internal CompanyModel GetCompanyBySha1(string sha1)
        {
            if (string.IsNullOrEmpty(sha1))
            {
                return settingList.FirstOrDefault();
            }
            return settingList.FirstOrDefault((CompanyModel p) => p.Sha1 == sha1);
        }
    } 
}
