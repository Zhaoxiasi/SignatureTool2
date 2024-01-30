// SignTool.Utilites.ConfigTool
using System;
using System.Collections.Generic;
using System.IO;
using SignatureTool2.Utilites;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Files;
using SignatureTool2.Utilites.Plist;

namespace SignatureTool2.Utilites
{
    public class ConfigTool
    {
        protected readonly string _configName = "config.plist";

        protected readonly string _cacheConfigName = "config-cache.plist";

        protected readonly int _configVersion = 1;

        private string _configSaveFolderPath;

        private string _configPath;

        private string _configCachePath;

        private Dictionary<string, object> _settings;

        private static ConfigTool _configTool;

        private static object _lockInit = new object();

        private static object _lockWrite = new object();

        public static ConfigTool Instance
        {
            get
            {
                lock (_lockInit)
                {
                    if (_configTool == null)
                    {
                        _configTool = new ConfigTool();
                    }
                    return _configTool;
                }
            }
        }

        private ConfigTool()
        {
            InitPath();
            LoadConfig();
        }

        private void InitPath()
        {
            _configSaveFolderPath = AppDomain.CurrentDomain.BaseDirectory.Combine("Config");
            Directory.CreateDirectory(_configSaveFolderPath);
            _configPath = Path.Combine(_configSaveFolderPath, _configName);
            _configCachePath = Path.Combine(_configSaveFolderPath, _cacheConfigName);
        }

        private void LoadConfig()
        {
            if (_configPath.IsExistsFile())
            {
                string readPath = _configPath;
                if (FileTool.CopyFile(_configPath, _configCachePath) == 0)
                {
                    readPath = _configCachePath;
                }
                _settings = PlistTool.ReadPlist(readPath);
                if (_settings == null)
                {
                    CreateDefaultConfig();
                    return;
                }
                if (CheckSetting(GetDefaultConfig(), _settings))
                {
                    SaveConfig();
                }
                FileTool.DeleteFile(_configCachePath);
            }
            else
            {
                CreateDefaultConfig();
            }
        }

        private bool CheckSetting(Dictionary<string, object> source, Dictionary<string, object> target)
        {
            bool flag = false;
            foreach (KeyValuePair<string, object> item in source)
            {
                if (!target.ContainsKey(item.Key))
                {
                    target.Add(item.Key, item.Value);
                    flag = true;
                    continue;
                }
                Dictionary<string, object> dictionary = item.Value as Dictionary<string, object>;
                if (dictionary == null)
                {
                    continue;
                }
                Dictionary<string, object> dictionary2 = target[item.Key] as Dictionary<string, object>;
                if (dictionary2 != null)
                {
                    bool flag2 = CheckSetting(dictionary, dictionary2);
                    if (!flag)
                    {
                        flag = flag2;
                    }
                }
                else
                {
                    target[item.Key] = item.Value;
                    flag = true;
                }
            }
            return flag;
        }

        private void CreateDefaultConfig()
        {
            _settings = GetDefaultConfig();
            SaveConfig();
        }

        private Dictionary<string, object> GetDefaultConfig()
        {
            return new Dictionary<string, object>();
        }

        private Dictionary<string, object> GetDefaultSetting()
        {
            return new Dictionary<string, object>();
        }

        public T GetValveOfRoot<T>(CRootKey key)
        {
            return _settings.GetValue<T>(MatchCRootKey(key));
        }

        public T GetValueOfSecondNode<T>(CRootKey rootKey, CSecondNodeKey sceondKey)
        {
            Dictionary<string, object> value = _settings.GetValue<Dictionary<string, object>>(MatchCRootKey(rootKey));
            if (value == null)
            {
                return default(T);
            }
            return value.GetValue<T>(MatchSecondNodeKey(sceondKey));
        }

        public void CreateSecondNode(Dictionary<string, object> root, CRootKey rootKey, CSecondNodeKey key, object value)
        {
            if (root == null)
            {
                root = _settings.GetValue<Dictionary<string, object>>(MatchCRootKey(rootKey));
            }
            if (root == null)
            {
                root = new Dictionary<string, object>();
            }
            root[MatchSecondNodeKey(key)] = value;
        }

        public void CreateSecondNode(Dictionary<string, object> root, CSecondNodeKey key, object value)
        {
            if (root == null)
            {
                root = new Dictionary<string, object>();
            }
            root[MatchSecondNodeKey(key)] = value;
        }

        public void SetValueOfRoot(CRootKey key, object value)
        {
            _settings[MatchCRootKey(key)] = value;
            SaveConfig();
        }

        public void SetValueOfSecondNode(CRootKey rootKey, CSecondNodeKey nodeKey, object value)
        {
            Dictionary<string, object> dictionary = null;
            if (_settings.ContainsKey(MatchCRootKey(rootKey)))
            {
                dictionary = _settings[MatchCRootKey(rootKey)] as Dictionary<string, object>;
            }
            if (dictionary == null)
            {
                dictionary = new Dictionary<string, object>();
                _settings[MatchCRootKey(rootKey)] = dictionary;
            }
            dictionary[MatchSecondNodeKey(nodeKey)] = value;
            SaveConfig();
        }

        public void RemoveValueOfRoot(CRootKey key)
        {
            if (_settings.ContainsKey(MatchCRootKey(key)))
            {
                _settings.Remove(MatchCRootKey(key));
                SaveConfig();
            }
        }

        public void RemoveValueOfSecondNode(CRootKey rootKey, CSecondNodeKey secondKey)
        {
            Dictionary<string, object> value = _settings.GetValue<Dictionary<string, object>>(MatchCRootKey(rootKey));
            if (value != null && value.ContainsKey(MatchSecondNodeKey(secondKey)))
            {
                value.Remove(MatchSecondNodeKey(secondKey));
                SaveConfig();
            }
        }

        private string MatchCRootKey(CRootKey key)
        {
            return key.ToString();
        }

        private string MatchSecondNodeKey(CSecondNodeKey key)
        {
            return key.ToString();
        }

        public void CreateDictionaryOfSecondRode(Dictionary<string, object> dic, CSecondNodeKey key, string value)
        {
            if (dic != null)
            {
                if (value == null)
                {
                    value = "";
                }
                dic[key.ToString()] = value;
            }
        }

        public T GetDictionaryValueOfSecondRode<T>(Dictionary<string, object> dic, CSecondNodeKey key)
        {
            if (dic == null)
            {
                return default(T);
            }
            if (dic.ContainsKey(key.ToString()))
            {
                return (T)dic[key.ToString()];
            }
            return default(T);
        }

        public void BackupConfig()
        {
            string text = _configSaveFolderPath.Combine("Backup");
            Directory.CreateDirectory(text);
            text = FileTool.GetAvailableFilePath(text.Combine($"{_configName.GetFileNameWithoutExtension()}-{DateTime.Now:yyyy-MM-dd}{_configName.GetExtension()}"));
            FileTool.CopyFile(_configPath, text);
        }

        private void SaveConfig()
        {
            lock (_lockWrite)
            {
                FileTool.DeleteFile(_configCachePath);
                PlistTool.WritePlist(_settings, _configCachePath);
                FileTool.CopyFile(_configCachePath, _configPath);
                FileTool.DeleteFile(_configCachePath);
            }
        }
    } 
}
