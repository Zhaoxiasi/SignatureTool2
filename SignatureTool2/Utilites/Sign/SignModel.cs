// SignTool.Utilites.Sign.SignModel

namespace SignatureTool2.Utilites.Sign
{
    public class SignModel
    {
        public string FilePath { get; set; }
        public string sha1 { get; set; }

        public SignResultEnum SignResult { get; set; }
    }
}
