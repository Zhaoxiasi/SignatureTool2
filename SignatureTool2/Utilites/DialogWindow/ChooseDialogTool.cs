// SignTool.Utilites.DialogWindow.ChooseDialogTool
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAPICodePack.Dialogs;
namespace SignatureTool2.Utilites.DialogWindow
{

    internal class ChooseDialogTool
    {
        public static string OpenSelectFileDialog()
        {
            return OpenSelectFileDialog(new List<(string, string)> { ("All", "(*.*)|*.*") }, multiSelect: false).FirstOrDefault();
        }

        public static List<string> OpenSelectFileDialog(bool multiselect)
        {
            return OpenSelectFileDialog(new List<(string, string)> { ("All", "(*.*)|*.*") }, multiselect);
        }

        public static string OpenSelectFileDialog((string rawDisplayName, string extensionList) filter, bool multiSelect)
        {
            return OpenSelectFileDialog(new List<(string, string)> { filter }, multiSelect).FirstOrDefault();
        }

        public static List<string> OpenSelectFileDialog(List<(string rawDisplayName, string extensionList)> filters, bool multiSelect)
        {
            List<string> list = new List<string>();
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog
            {
                Multiselect = multiSelect,
                IsFolderPicker = false
            };
            foreach (var (rawDisplayName, extensionList) in filters)
            {
                commonOpenFileDialog.Filters.Add(new CommonFileDialogFilter(rawDisplayName, extensionList));
            }
            if (commonOpenFileDialog.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return list;
            }
            IEnumerable<string> fileNames = commonOpenFileDialog.FileNames;
            list.AddRange(fileNames);
            return list;
        }

        public static string OpenSelectFolderDialog(string selectedFolder = "")
        {
            string result = "";
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                DefaultDirectory = selectedFolder
            };
            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                result = commonOpenFileDialog.FileName;
            }
            return result;
        }

        public static IEnumerable<string> OpenMultiSelectFolderDialog(string selectedFolder = "")
        {
            IEnumerable<string> result = new List<string>();
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                DefaultDirectory = selectedFolder,
                Multiselect = true
            };
            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                result = commonOpenFileDialog.FileNames;
            }
            return result;
        }
    } 
}
