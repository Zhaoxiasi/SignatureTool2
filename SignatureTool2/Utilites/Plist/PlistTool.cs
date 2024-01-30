// SignTool.Utilites.Plist.PlistTool
#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PList;

namespace SignatureTool2.Utilites.Plist
{
    internal static class PlistTool
    {
        public static bool WritePlist(Dictionary<string, object> obj, string path)
        {
            try
            {
                IPListElement root = ConvertDictionary(obj);
                PListRoot pListRoot = new PListRoot();
                pListRoot.Root = root;
                pListRoot.Save(path);
                return true;
            }
            catch (Exception arg)
            {
                Trace.TraceInformation($"write plist exception: {arg}");
            }
            return false;
        }

        private static IPListElement ConvertDictionary(Dictionary<string, object> dic)
        {
            PListDict pListDict = new PListDict();
            foreach (KeyValuePair<string, object> item in dic)
            {
                switch (item.Value.GetType().ToString())
                {
                    case "System.Collections.Generic.Dictionary`2[System.String,System.Object]":
                        pListDict[item.Key] = ConvertDictionary((Dictionary<string, object>)item.Value);
                        break;
                    case "System.Collections.Generic.List`1[System.Object]":
                        pListDict[item.Key] = ConvertObject(item.Value);
                        break;
                    case "System.Byte[]":
                        pListDict[item.Key] = new PListData((byte[])item.Value);
                        break;
                    case "System.Double":
                        pListDict[item.Key] = new PListReal((double)item.Value);
                        break;
                    case "System.Int32":
                    case "System.Int64":
                    case "System.UInt32":
                        pListDict[item.Key] = new PListInteger(long.Parse(item.Value.ToString()));
                        break;
                    case "System.String":
                        pListDict[item.Key] = new PListString((string)item.Value);
                        break;
                    case "System.DateTime":
                        pListDict[item.Key] = new PListDate((DateTime)item.Value);
                        break;
                    case "System.Boolean":
                        pListDict[item.Key] = new PListBool((bool)item.Value);
                        break;
                    default:
                        Trace.TraceWarning("the type of <" + dic.GetType().Name + "> is not support");
                        break;
                }
            }
            return pListDict;
        }

        private static IPListElement ConvertObject(object dic)
        {
            IPListElement result = null;
            switch (dic.GetType().ToString())
            {
                case "System.Collections.Generic.Dictionary`2[System.String,System.Object]":
                    result = ConvertDictionary(dic as Dictionary<string, object>);
                    break;
                case "System.Collections.Generic.List`1[System.Object]":
                    result = ConvertList(dic as List<object>);
                    break;
                case "System.Byte[]":
                    result = new PListData((byte[])dic);
                    break;
                case "System.Double":
                    result = new PListReal((double)dic);
                    break;
                case "System.Int32":
                case "System.Int64":
                case "System.UInt32":
                    result = new PListInteger(long.Parse(dic.ToString()));
                    break;
                case "System.String":
                    result = new PListString((string)dic);
                    break;
                case "System.DateTime":
                    result = new PListDate((DateTime)dic);
                    break;
                case "System.Boolean":
                    result = new PListBool((bool)dic);
                    break;
                default:
                    Trace.TraceWarning("the type of <" + dic.GetType().Name + "> is not support");
                    break;
            }
            return result;
        }

        private static IPListElement ConvertList(List<object> li)
        {
            PListArray pListArray = new PListArray();
            foreach (object item2 in li)
            {
                IPListElement item = ConvertObject(item2);
                pListArray.Add(item);
            }
            return pListArray;
        }

        internal static Dictionary<string, object> ReadPlist(string readPath)
        {
            object obj = null;
            if (!File.Exists(readPath))
            {
                return null;
            }
            try
            {
                obj = ConvertPlistDict(PListRoot.Load(readPath)?.Root);
            }
            catch (Exception arg)
            {
                Trace.TraceWarning($"read plist exception:{arg}");
            }
            return obj as Dictionary<string, object>;
        }

        private static object ConvertPlistDict(IPListElement element)
        {
            object result = null;
            PListDict pListDict = element as PListDict;
            if (pListDict != null)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                foreach (KeyValuePair<string, IPListElement> item in pListDict)
                {
                    object obj = ConvertPlistDict(item.Value);
                    if (obj != null)
                    {
                        dictionary.Add(item.Key, obj);
                    }
                }
                result = dictionary;
            }
            else
            {
                PListArray pListArray = element as PListArray;
                if (pListArray != null)
                {
                    List<object> list = new List<object>();
                    foreach (IPListElement item2 in pListArray)
                    {
                        object obj2 = ConvertPlistDict(item2);
                        if (obj2 != null)
                        {
                            list.Add(obj2);
                        }
                    }
                    result = list;
                }
                else
                {
                    PListBool pListBool = element as PListBool;
                    if (pListBool != null)
                    {
                        result = pListBool.Value;
                    }
                    else
                    {
                        PListData pListData = element as PListData;
                        if (pListData != null)
                        {
                            result = pListData.Value;
                        }
                        else
                        {
                            PListDate pListDate = element as PListDate;
                            if (pListDate != null)
                            {
                                result = pListDate.Value;
                            }
                            else
                            {
                                PListInteger pListInteger = element as PListInteger;
                                if (pListInteger != null)
                                {
                                    result = pListInteger.Value;
                                }
                                else
                                {
                                    PListReal pListReal = element as PListReal;
                                    if (pListReal != null)
                                    {
                                        result = pListReal.Value;
                                    }
                                    else
                                    {
                                        PListString pListString = element as PListString;
                                        if (pListString != null)
                                        {
                                            result = pListString.Value;
                                        }
                                        else
                                        {
                                            Trace.TraceWarning("the type of <" + element.GetType().Name + "> is not support to read");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
    } 
}
