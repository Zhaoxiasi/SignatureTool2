// SignTool.Utilites.Directorys.FolderTool
using System;
using System.Collections.Generic;
using System.IO;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.Utilites.Files;

namespace SignatureTool2.Utilites.Directorys
{
    public static class FolderTool
    {
        public static bool DeleteFolder(string folderPath, Action<Exception> errorCallBack = null)
        {
            bool result = false;
            if (Directory.Exists(folderPath))
            {
                string[] directories = Directory.GetDirectories(folderPath);
                for (int i = 0; i < directories.Length; i++)
                {
                    DeleteFolder(directories[i], errorCallBack);
                }
                directories = Directory.GetFiles(folderPath);
                for (int i = 0; i < directories.Length; i++)
                {
                    FileTool.DeleteFile(directories[i], errorCallBack);
                }
                try
                {
                    Directory.Delete(folderPath);
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
            return true;
        }

        public static long GetFolderSize(string folderPath)
        {
            if (!folderPath.IsExistsFolder())
            {
                return 0L;
            }
            long num = 0L;
            string[] directories = Directory.GetDirectories(folderPath);
            foreach (string folderPath2 in directories)
            {
                num += GetFolderSize(folderPath2);
            }
            directories = Directory.GetFiles(folderPath);
            foreach (string filePath in directories)
            {
                num += FileTool.GetFileSize(filePath);
            }
            return num;
        }

        public static string GetAvailablePath(string folderPath, bool createDir = true)
        {
            string text = folderPath;
            string fileName = Path.GetFileName(folderPath);
            string directoryName = Path.GetDirectoryName(folderPath);
            int num = 1;
            while (Directory.Exists(text))
            {
                text = Path.Combine(directoryName, $"{fileName}({num++})");
            }
            if (createDir)
            {
                Directory.CreateDirectory(text);
            }
            return text;
        }

        public static bool CopyFolder(string sourcePath, string targetPath, bool isOnlyFirstLevelFile, Action<Exception> callBack)
        {
            bool flag = true;
            string[] files = Directory.GetFiles(sourcePath);
            Directory.CreateDirectory(targetPath);
            string[] array = files;
            foreach (string text in array)
            {
                if (FileTool.CopyFile(text, Path.Combine(targetPath, Path.GetFileName(text)), callBack) != 0)
                {
                    flag = false;
                    break;
                }
            }
            if (!isOnlyFirstLevelFile && flag)
            {
                array = Directory.GetDirectories(sourcePath);
                foreach (string text2 in array)
                {
                    flag = CopyFolder(text2, Path.Combine(targetPath, Path.GetFileName(text2)), isOnlyFirstLevelFile, callBack);
                    if (!flag)
                    {
                        break;
                    }
                }
            }
            if (!flag)
            {
                DeleteFolder(targetPath);
            }
            return flag;
        }

        internal static List<string> GetAllFiles(string sourcePath)
        {
            List<string> list = new List<string>();
            list.AddRange(Directory.GetFiles(sourcePath));
            string[] directories = Directory.GetDirectories(sourcePath);
            foreach (string sourcePath2 in directories)
            {
                list.AddRange(GetAllFiles(sourcePath2));
            }
            return list;
        }
    } 
}
