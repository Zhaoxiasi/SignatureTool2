// SignTool.Utilites.ProtecterTool
#define TRACE
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SignatureTool2.Utilites;

namespace SignatureTool2.Utilites
{
    public class ProtecterTool
    {
        private static ProtecterTool protecterTool;

        private List<ProtecterSettingModel> settingList;

        public List<ProtecterSettingModel> ProtecterSettingList => settingList;

        public static ProtecterTool Instance
        {
            get
            {
                if (protecterTool == null)
                {
                    protecterTool = new ProtecterTool();
                }
                return protecterTool;
            }
        }

        private ProtecterTool()
        {
            settingList = new List<ProtecterSettingModel>();
            ReadConfig();
        }

        private void ReadConfig()
        {
            List<object> valveOfRoot = ConfigTool.Instance.GetValveOfRoot<List<object>>(CRootKey.Protecter);
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
                    ProtecterSettingModel item = new ProtecterSettingModel
                    {
                        //commandParameter = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_CommandParameter),
                        protecterID = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_ProtecterID),
                        protecterPath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_ProtecterPath),
                        name = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_Name),
                    };
                    settingList.Add(item);
                }
            }
        }

        public void Save(List<ProtecterSettingModel> li)
        {
            if (li == null)
            {
                return;
            }
            List<object> list = new List<object>();
            foreach (ProtecterSettingModel item in li)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_Name, item.name);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_ProtecterID, item.protecterID);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_ProtecterPath, item.protecterPath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_CommandParameter, item.commandParameter);
                list.Add(dictionary);
            }
            ConfigTool.Instance.SetValueOfRoot(CRootKey.Protecter, list);
            Trace.TraceInformation("保存配置成功！");
            ReadConfig();
        }

        internal ProtecterSettingModel GetProtecterByID(string protecterID)
        {
            if (string.IsNullOrEmpty(protecterID))
            {
                return settingList.FirstOrDefault();
            }
            return settingList.FirstOrDefault((ProtecterSettingModel p) => p.protecterID == protecterID);
        }

    } 
}
