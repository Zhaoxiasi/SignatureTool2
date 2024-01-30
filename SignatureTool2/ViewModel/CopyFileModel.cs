// SignTool.ViewModel.CopyFileModel
using System;

namespace SignatureTool2.ViewModel
{
    public class CopyFileModel
    {
        public string SourcePath { get; set; }

        public string TargetPath { get; set; }

        public string RelativePath { get; set; }

        public DateTime CreateTime { get; set; }
    }

}