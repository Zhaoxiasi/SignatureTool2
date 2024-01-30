// SignTool.LogRecord
#define TRACE
using System.Diagnostics;

namespace SignatureTool2
{
    internal class LogRecord
    {
        public void WriteInfo(string msg)
        {
            Trace.TraceInformation(GetType().Name + ": " + msg);
        }

        public void WriteDebugInfo(string msg)
        {
        }

        public void WriteError(string msg)
        {
            Trace.TraceError(GetType().Name + ": " + msg);
        }

        public void WriteDEBUGError(string msg)
        {
        }

        public void WriteWarning(string msg)
        {
            Trace.TraceWarning(GetType().Name + ": " + msg);
        }

        public void WriteDBUGWarning(string msg)
        {
        }
    } 
}

