// SignTool.Utilites.Files.FileTool
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using SignatureTool2.Utilites.Extensions;

namespace SignatureTool2.Utilites.Files
{
    public static class FileTool
    {
        public static bool MatchFileMD5(string filePath, string compareMD5)
        {
            if (!File.Exists(filePath) || string.IsNullOrEmpty(compareMD5))
            {
                return false;
            }
            return GetFileMD5(filePath).ToLower().Equals(compareMD5.ToLower());
        }

        public static string GetFileMD5(string filePath)
        {
            if (filePath.IsNullOrEmpty() || !File.Exists(filePath))
            {
                return "";
            }
            string result = "";
            try
            {
                using FileStream stream = new FileStream(filePath, FileMode.Open);
                result = GetStreamMD5(stream);
                return result;
            }
            catch (Exception)
            {
                return result;
            }
        }

        public static string GetStringMD5(string str)
        {
            return GetByteMD5(Encoding.UTF8.GetBytes(str));
        }

        public static string GetByteMD5(byte[] data)
        {
            byte[] array = new MD5CryptoServiceProvider().ComputeHash(data);
            string text = "";
            for (int i = 0; i < array.Length; i++)
            {
                text += array[i].ToString("X").PadLeft(2, '0');
            }
            return text.ToLower();
        }

        public static string GetStreamMD5(Stream stream)
        {
            byte[] array = new MD5CryptoServiceProvider().ComputeHash(stream);
            string text = "";
            for (int i = 0; i < array.Length; i++)
            {
                text += array[i].ToString("X").PadLeft(2, '0');
            }
            return text.ToLower();
        }

        public static bool GetFileSignature(string filePath, string SignerName, Action<Exception> errorCallBack = null)
        {
            bool result = false;
            try
            {
                if (File.Exists(filePath) && (X509Certificate.CreateFromSignedFile(filePath)?.Subject.Contains(SignerName) ?? false))
                {
                    result = true;
                }
                return result;
            }
            catch (Exception obj)
            {
                if (errorCallBack != null)
                {
                    errorCallBack(obj);
                    return result;
                }
                return result;
            }
        }

        public static bool DeleteFile(string filePath, Action<Exception> errorCallBack = null)
        {
            bool result = false;
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                result = true;
                return result;
            }
            catch (Exception obj)
            {
                if (errorCallBack != null)
                {
                    errorCallBack(obj);
                    return result;
                }
                return result;
            }
        }

        public static long GetFileSize(string filePath)
        {
            if (filePath.IsExistsFile())
            {
                return new FileInfo(filePath).Length;
            }
            return 0L;
        }

        public static int CopyFile(string sourcePath, string targetPath, Action<Exception> errorCallBack = null)
        {
            int result = 0;
            try
            {
                DeleteFile(targetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, targetPath, overwrite: true);
                    return result;
                }
                result = -1;
                return result;
            }
            catch (Exception obj)
            {
                if (errorCallBack != null)
                {
                    errorCallBack(obj);
                    return result;
                }
                return result;
            }
        }

        public static int CopyFile(string sourcePath, string targetPath, Action<long, double, double> progressCallBack, Action<Exception> errorCallBack = null)
        {
            int result = 0;
            try
            {
                if (!DeleteFile(targetPath, errorCallBack))
                {
                    return -2;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                if (File.Exists(sourcePath))
                {
                    long totalSize = new FileInfo(sourcePath).Length;
                    OpenFile(sourcePath, delegate (FileStream s)
                    {
                        CreateFile(targetPath, delegate (FileStream t)
                        {
                            byte[] array = new byte[2097152];
                            int num = 0;
                            int num2 = 0;
                            while ((num2 = s.Read(array, 0, array.Length)) != 0)
                            {
                                t.Write(array, 0, num2);
                                num += num2;
                                progressCallBack?.Invoke(totalSize, num2, (double)num / (double)totalSize * 100.0);
                            }
                        }, errorCallBack);
                    }, errorCallBack);
                    File.Copy(sourcePath, targetPath, overwrite: true);
                    return result;
                }
                return -1;
            }
            catch (Exception obj)
            {
                errorCallBack?.Invoke(obj);
                return -10;
            }
        }

        public static void OpenFile(string filePath, Action<FileStream> streamCallBack, Action<Exception> errorCallBack = null)
        {
            if (filePath.IsNullOrEmpty() || !File.Exists(filePath))
            {
                streamCallBack?.Invoke(null);
                return;
            }
            try
            {
                using FileStream obj = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                streamCallBack?.Invoke(obj);
            }
            catch (Exception obj2)
            {
                errorCallBack?.Invoke(obj2);
            }
        }

        public static void CreateFile(string filePath, Action<FileStream> streamCallBack, Action<Exception> errorCallBack = null)
        {
            if (filePath.IsNullOrEmpty() || !DeleteFile(filePath))
            {
                streamCallBack?.Invoke(null);
                return;
            }
            using FileStream obj = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            try
            {
                streamCallBack?.Invoke(obj);
            }
            catch (Exception obj2)
            {
                errorCallBack?.Invoke(obj2);
            }
        }

        public static bool SaveFile(string savePath, string data, Action<Exception> errorCallBack = null)
        {
            if (data.IsNullOrEmpty())
            {
                return false;
            }
            return SaveFile(savePath, Encoding.UTF8.GetBytes(data), errorCallBack);
        }

        public static bool SaveFile(string savePath, byte[] data, Action<Exception> errorCallBack = null)
        {
            bool result = false;
            if (DeleteFile(savePath))
            {
                CreateFile(savePath, delegate (FileStream stream)
                {
                    result = SaveFile(stream, data, errorCallBack);
                }, errorCallBack);
            }
            return result;
        }

        public static bool SaveFile(Stream stream, byte[] data, Action<Exception> errorCallBack = null)
        {
            bool result = false;
            if (stream != null && data != null)
            {
                try
                {
                    stream.Write(data, 0, data.Length);
                    result = true;
                    return result;
                }
                catch (Exception obj)
                {
                    if (errorCallBack == null)
                    {
                        return result;
                    }
                    errorCallBack(obj);
                    return result;
                }
            }
            return result;
        }

        public static byte[] ReadFile(string filePath, Action<Exception> errorCallBack = null)
        {
            byte[] data = null;
            if (filePath.IsExistsFile())
            {
                OpenFile(filePath, delegate (FileStream stream)
                {
                    data = ReadStream(stream, errorCallBack);
                }, errorCallBack);
            }
            return data;
        }

        public static byte[] ReadStream(Stream stream, Action<Exception> errorCallBack = null)
        {
            byte[] result = null;
            try
            {
                if (stream != null)
                {
                    if (stream.CanRead)
                    {
                        stream.Position = 0L;
                        List<byte> list = new List<byte>();
                        byte[] array = new byte[2097152];
                        int num = 0;
                        while ((num = stream.Read(array, 0, array.Length)) > 0)
                        {
                            for (int i = 0; i < num; i++)
                            {
                                list.Add(array[i]);
                            }
                        }
                        result = list.ToArray();
                        list.Clear();
                        return result;
                    }
                    return result;
                }
                return result;
            }
            catch (Exception obj)
            {
                if (errorCallBack != null)
                {
                    errorCallBack(obj);
                    return result;
                }
                return result;
            }
        }

        public static string GetAvailableFileName(string fileName)
        {
            if (fileName.IsNullOrEmpty())
            {
                return "";
            }
            string text = Regex.Replace(fileName, "[\\\\\\\\\\\\?\\\\/\\\\*\\\\|<>:\\\\\\\"\\a\\b\\f\\n\\r\\t\\v\\0\ufffd]", "");
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                for (int i = 0; i < text.Length; i++)
                {
                    int num = text[i];
                    if (num > 31 || num == 9)
                    {
                        stringBuilder.Append(text[i]);
                    }
                }
            }
            catch (Exception)
            {
            }
            string text2 = stringBuilder.ToString();
            if (!string.IsNullOrEmpty(text2))
            {
                text2 = text2.Replace("AUX", "");
                text2 = text2.Replace("AUx", "");
                text2 = text2.Replace("AuX", "");
                text2 = text2.Replace("aUX", "");
                text2 = text2.Replace("aUx", "");
                text2 = text2.Replace("aux", "");
                text2 = text2.Replace(",", "");
            }
            return text2;
        }

        public static string GetAvailableFilePath(string filePath)
        {
            string text = filePath;
            string extension = Path.GetExtension(filePath);
            string availableFileName = GetAvailableFileName(Path.GetFileNameWithoutExtension(filePath));
            string fullName = Directory.GetParent(filePath).FullName;
            int num = 1;
            text = Path.Combine(fullName, availableFileName + extension);
            while (text.IsExistsFile())
            {
                text = Path.Combine(fullName, $"{availableFileName}({num++}){extension}");
            }
            return text;
        }
    } 
}
