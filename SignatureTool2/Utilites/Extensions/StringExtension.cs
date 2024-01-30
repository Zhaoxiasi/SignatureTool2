// SignTool.Utilites.Extensions.StringExtension
#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SignatureTool2.Utilites.Extensions
{
    public static class StringExtension
    {
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static string Format(this string value, params object[] arg)
        {
            if (!IsNullOrEmpty(value) && arg != null && arg.Length != 0)
            {
                List<string> list = new List<string>();
                string text = "";
                for (int i = 0; i < arg.Length; i++)
                {
                    text = "{" + i + "}";
                    list.Add(text);
                    if (!value.Contains(text))
                    {
                        Trace.TraceWarning("<" + value + "> is not contains: " + text);
                        return string.Format(value, arg);
                    }
                }
                string[] array = value.Split(list.ToArray(), StringSplitOptions.None);
                StringBuilder stringBuilder = new StringBuilder();
                List<object> list2 = arg.ToList();
                for (int j = 0; j < array.Length; j++)
                {
                    stringBuilder.Append(array[j]);
                    if (list2.Count > 0)
                    {
                        stringBuilder.Append(list2[0]);
                        list2.RemoveAt(0);
                    }
                }
                string text2 = stringBuilder.ToString();
                MatchCollection matchCollection = new Regex("{[0-9]{1,9}}").Matches(text2);
                if (matchCollection.Count > 0)
                {
                    stringBuilder.Clear();
                    foreach (object item in matchCollection)
                    {
                        stringBuilder.Append($" {item}");
                    }
                    Trace.TraceWarning("<{value}> is not match, not replaced:" + stringBuilder.ToString());
                    return text2;
                }
                return text2;
            }
            return value;
        }

        public static bool IsExistsFile(this string value)
        {
            bool result = false;
            if (!IsNullOrEmpty(value))
            {
                result = File.Exists(value);
            }
            return result;
        }

        public static bool IsExistsFolder(this string value)
        {
            bool result = false;
            if (!IsNullOrEmpty(value))
            {
                result = Directory.Exists(value);
            }
            return result;
        }

        public static string Combine(this string value, string p1)
        {
            return Path.Combine(value, p1);
        }

        public static string Combine(this string value, string p1, string p2)
        {
            return Path.Combine(value, p1, p2);
        }

        public static string GetFileName(this string value)
        {
            return Path.GetFileName(value);
        }

        public static string GetFileNameWithoutExtension(this string value)
        {
            return Path.GetFileNameWithoutExtension(value);
        }

        public static string GetExtension(this string value)
        {
            string extension = Path.GetExtension(value);
            if (!IsNullOrEmpty(extension))
            {
                return extension;
            }
            return "";
        }
    } 
}
