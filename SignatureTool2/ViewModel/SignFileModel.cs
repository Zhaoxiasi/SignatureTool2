// SignTool.ViewModel.SignFileModel
using SignatureTool2;

namespace SignatureTool2.ViewModel
{
    public class SignFileModel : ViewModelBase
    {
        private string fileName;

        private string filePath;

        private string signStatus;

        private bool isSelected;

        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }
        }

        public string FilePath
        {
            get
            {
                return filePath;
            }
            set
            {
                filePath = value;
            }
        }

        public string SignStatus
        {
            get
            {
                return signStatus;
            }
            set
            {
                SetProperty(ref signStatus, value, "SignStatus");
            }
        }

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                SetProperty(ref isSelected, value, "IsSelected");
            }
        }
    } 
}
