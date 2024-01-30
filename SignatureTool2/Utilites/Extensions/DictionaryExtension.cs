// SignTool.Utilites.Extensions.DictionaryExtension
using System;
using System.Collections.Generic;

namespace SignatureTool2.Utilites.Extensions
{
    public static class DictionaryExtension
    {
        public static T GetValue<T>(this Dictionary<string, object> dic, string key)
        {
            if (dic == null || key.IsNullOrEmpty())
            {
                return default(T);
            }
            if (dic.ContainsKey(key))
            {
                return (T)Convert.ChangeType(dic[key], typeof(T));
            }
            return default(T);
        }

        public static T GetValue<T>(this Dictionary<string, object> dic, string key, T defaultValue)
        {
            if (dic == null)
            {
                return defaultValue;
            }
            if (dic.ContainsKey(key))
            {
                return (T)Convert.ChangeType(dic[key], typeof(T));
            }
            return defaultValue;
        }

        public static void AddValue<T>(this Dictionary<string, object> dic, string key, object data)
        {
            if (dic != null)
            {
                if (dic.ContainsKey(key))
                {
                    dic[key] = (T)data;
                }
                else
                {
                    dic.Add(key, (T)data);
                }
            }
        }

        public static void AddValue(this Dictionary<string, object> dic, Dictionary<string, object> value)
        {
            if (dic == null || value == null)
            {
                return;
            }
            foreach (KeyValuePair<string, object> item in value)
            {
                dic.AddValue<object>(item.Key, item.Value);
            }
        }

        public static void RemoveKey(this Dictionary<string, object> dic, string key)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                dic.Remove(key);
            }
        }
    }
}
