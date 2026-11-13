using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SignatureTool2.Utilites
{
    /// <summary>
    /// 统一管理一次运行（构建/签名/压缩）的取消令牌与子进程。
    /// Stop() 会立即取消令牌并终止所有已登记的子进程（含进程树）。
    /// </summary>
    public static class RunControl
    {
        private static readonly object _lock = new object();
        private static CancellationTokenSource _cts;
        private static readonly List<Process> _processes = new List<Process>();

        public static bool IsRunning { get; private set; }

        public static CancellationToken Token
        {
            get
            {
                lock (_lock)
                {
                    return _cts?.Token ?? CancellationToken.None;
                }
            }
        }

        public static void Start()
        {
            lock (_lock)
            {
                if (IsRunning)
                {
                    return;
                }
                _cts = new CancellationTokenSource();
                _processes.Clear();
                IsRunning = true;
            }
        }

        /// <summary>立即停止：取消令牌并 taskkill /T /F 终止所有已登记的子进程。</summary>
        public static void Stop()
        {
            lock (_lock)
            {
                if (!IsRunning)
                {
                    return;
                }
                try { _cts.Cancel(); }
                catch (ObjectDisposedException) { }
                foreach (Process process in _processes.ToArray())
                {
                    KillTree(process);
                }
            }
        }

        public static void End()
        {
            lock (_lock)
            {
                _processes.Clear();
                try { _cts?.Dispose(); }
                catch { }
                _cts = null;
                IsRunning = false;
            }
        }

        public static void Track(Process process)
        {
            if (process == null)
            {
                return;
            }
            lock (_lock)
            {
                if (IsRunning && !process.HasExited)
                {
                    _processes.Add(process);
                }
            }
        }

        public static void Untrack(Process process)
        {
            if (process == null)
            {
                return;
            }
            lock (_lock)
            {
                _processes.Remove(process);
            }
        }

        /// <summary>用 taskkill /T /F 结束进程树（cmd.exe 的子进程 msbuild/signtool 会一并结束）。</summary>
        public static void KillTree(Process process)
        {
            if (process == null)
            {
                return;
            }
            try
            {
                if (process.HasExited)
                {
                    return;
                }
                using (Process killer = Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/T /F /PID {process.Id}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                }))
                {
                    killer?.WaitForExit(5000);
                }
                Trace.TraceInformation($"已终止进程树 PID<{process.Id}>");
            }
            catch (Exception ex)
            {
                try { if (!process.HasExited) process.Kill(); }
                catch { }
                Trace.TraceWarning($"终止进程 PID<{process.Id}>失败: {ex.Message}");
            }
        }
    }
}
