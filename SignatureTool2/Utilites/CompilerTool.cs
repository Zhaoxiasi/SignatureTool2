// SignTool.Utilites.CompilerTool
#define TRACE
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SignatureTool2.Utilites;

namespace SignatureTool2.Utilites
{
    public class CompilerTool
    {
        private static CompilerTool compilerTool;

        private List<CompilerSettingModel> settingList;

        public List<CompilerSettingModel> CompilerSettingList => settingList;

        public static CompilerTool Instance
        {
            get
            {
                if (compilerTool == null)
                {
                    compilerTool = new CompilerTool();
                }
                return compilerTool;
            }
        }

        private CompilerTool()
        {
            settingList = new List<CompilerSettingModel>();
            ReadConfig();
        }

        private void ReadConfig()
        {
            List<object> valveOfRoot = ConfigTool.Instance.GetValveOfRoot<List<object>>(CRootKey.Compiler);
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
                    CompilerSettingModel item = new CompilerSettingModel
                    {
                        commandParameter = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_CommandParameter),
                        vsBuilderPath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_VsBuilderPath),
                        wpfResourcePath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_WpfResourcePath),
                        compilerIconSavePath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_CompilerIconSavePath),
                        compilerID = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_CompilerID),
                        compilerPath = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_CompilerPath),
                        name = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_Name),
                        replaceIconName = instance.GetDictionaryValueOfSecondRode<string>(dictionary, CSecondNodeKey.C_ReplaceIconName)
                    };
                    settingList.Add(item);
                }
            }
        }

        public void Save(List<CompilerSettingModel> li)
        {
            if (li == null)
            {
                return;
            }
            List<object> list = new List<object>();
            foreach (CompilerSettingModel item in li)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_Name, item.name);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_CompilerID, item.compilerID);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_CompilerPath, item.compilerPath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_ReplaceIconName, item.replaceIconName);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_CompilerIconSavePath, item.compilerIconSavePath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_CommandParameter, item.commandParameter);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_VsBuilderPath, item.vsBuilderPath);
                ConfigTool.Instance.CreateDictionaryOfSecondRode(dictionary, CSecondNodeKey.C_WpfResourcePath, item.wpfResourcePath);
                list.Add(dictionary);
            }
            ConfigTool.Instance.SetValueOfRoot(CRootKey.Compiler, list);
            Trace.TraceInformation("保存配置成功！");
            ReadConfig();
        }

        internal CompilerSettingModel GetCompilerByID(string compilerID)
        {
            if (string.IsNullOrEmpty(compilerID))
            {
                return settingList.FirstOrDefault();
            }
            return settingList.FirstOrDefault((CompilerSettingModel p) => p.compilerID == compilerID);
        }

    } 
}
