// SignTool.Utilites.Log.LogTool
using System;
using System.Diagnostics;
namespace SignatureTool2.Utilites.Log
{
    public class LogTool
    {
        private static LogTool logTool;

        public static LogTool Instance
        {
            get
            {
                if (logTool == null)
                {
                    logTool = new LogTool();
                }
                return logTool;
            }
        }

        public event Action<string> WriteLogEvent;
        public event Action<TraceEventType, string> WriteLogEventAdvance;

        private LogTool()
        {
        }

        public void Init()
        {
            LogListener logListener = new LogListener();
            logListener.WriteLogEvent += LogListener_WriteLogEvent;
            Trace.Listeners.Add(logListener);
        }

        private void LogListener_WriteLogEvent(TraceEventType eventType, string msg)
        {
            this.WriteLogEvent?.Invoke( $"{DateTime.Now:HH:mm:ss} - {msg}");
            this.WriteLogEventAdvance?.Invoke(eventType, $"{DateTime.Now:HH:mm:ss} - {msg}");
        }
    }

}
