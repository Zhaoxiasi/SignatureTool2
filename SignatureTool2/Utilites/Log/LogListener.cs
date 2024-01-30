// SignTool.Utilites.Log.LogListener
using System;
using System.Diagnostics;

namespace SignatureTool2.Utilites.Log
{
    public class LogListener : TraceListener
    {
        public event Action<TraceEventType, string> WriteLogEvent;

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (eventType == TraceEventType.Error || eventType == TraceEventType.Warning || eventType == TraceEventType.Information)
            {
                this.WriteLogEvent?.Invoke(eventType,message);
            }
            base.TraceEvent(eventCache, source, eventType, id, message);
        }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
		{
            if (message.StartsWith("Transfer:"))
            {
                this.WriteLogEvent?.Invoke(TraceEventType.Transfer, message.Replace("Transfer:",""));
            }
		}
	}
}

