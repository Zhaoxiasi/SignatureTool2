// SignTool.Utilites.SemaphoreTool
#define TRACE
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SignatureTool2.Utilites
{
    public class SemaphoreTool
    {
        private Semaphore _semaphore;

        private AutoResetEvent _resetEvent;

        private int _allThreadCount;

        private int _finishedCount;

        private int _failedCount;

        private int _inProcessCount;

        private bool _isSkip;

        public bool HasFailed => _failedCount > 0;

        public SemaphoreTool(int allThreadCount, int limitThreadCount)
        {
            _allThreadCount = Math.Max(0, allThreadCount);
            limitThreadCount = Math.Max(1, limitThreadCount);
            _semaphore = new Semaphore(limitThreadCount, limitThreadCount);
            _resetEvent = new AutoResetEvent(initialState: false);
        }

        public void InvokeByTask(Action<object> callBack, object parameter)
        {
            _semaphore.WaitOne();
            _inProcessCount++;
            Task.Factory.StartNew(delegate (object para)
            {
                try
                {
                    callBack?.Invoke(para);
                }
                catch (Exception ex)
                {
                    _failedCount++;
                    WriteError(ex);
                }
                finally
                {
                    _semaphore.Release();
                    Interlocked.Increment(ref _finishedCount);
                    if (_allThreadCount == _finishedCount)
                    {
                        _resetEvent.Set();
                    }
                    CheckIsSkiped();
                }
            }, parameter);
        }

        public void InvokeByThread(Action<object> callBack)
        {
            InvokeByThread(callBack, "");
        }

        public void InvokeByThread(Action<object> callBack, string threadName)
        {
            InvokeByThread(callBack, threadName, null);
        }

        public void InvokeByThread(Action<object> callBack, string threadName, object parameter)
        {
            _semaphore.WaitOne();
            _inProcessCount++;
            Thread thread = new Thread((ThreadStart)delegate
            {
                try
                {
                    callBack?.Invoke(parameter);
                }
                catch (Exception ex)
                {
                    _failedCount++;
                    WriteError(ex);
                }
                finally
                {
                    _semaphore.Release();
                    Interlocked.Increment(ref _finishedCount);
                    if (_allThreadCount == _finishedCount)
                    {
                        _resetEvent.Set();
                    }
                    CheckIsSkiped();
                }
            });
            thread.Name = threadName;
            thread.Start();
        }

        public void WaitAllFinish()
        {
            if (_allThreadCount > 0)
            {
                _resetEvent.WaitOne();
            }
        }

        public void SkipThread()
        {
            _isSkip = true;
        }

        private void CheckIsSkiped()
        {
            Interlocked.Decrement(ref _inProcessCount);
            if (_isSkip && _inProcessCount == 0)
            {
                _resetEvent.Set();
            }
        }

        private void WriteError(Exception ex)
        {
            Trace.TraceError($"semaphore invoke exception: {ex}");
        }
    } 
}
