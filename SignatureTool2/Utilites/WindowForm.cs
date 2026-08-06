using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignatureTool2.Utilities
{
    public class WindowForm
    {
        public IntPtr Handle { get; private set; }
        public int ThreadID { get; private set; }

        private WindowForm()
        {

        }

        public static WindowForm NewWindow(string windowName)
        {
            var handle = FindWindowA(null, windowName);
            if (handle == IntPtr.Zero)
                return null;
            GetWindowThreadProcessId(handle, out var process);
            var win = new WindowForm()
            {
                Handle = handle,
                ThreadID = (int)process,
            };
            return win;
        }

        public List<ControlInfo> EnumChildControls()
        {
            List<ControlInfo> controls = new List<ControlInfo>();

            EnumChildWindows(Handle, (hWnd, lParam) =>
            {
                StringBuilder title = new StringBuilder(256);
                StringBuilder className = new StringBuilder(256);

                GetWindowText(hWnd, title, title.Capacity);
                GetClassName(hWnd, className, className.Capacity);

                controls.Add(new ControlInfo
                {
                    Handle = hWnd,
                    Title = title.ToString(),
                    ClassName = className.ToString()
                });

                return true;
            }, IntPtr.Zero);
            return controls;
        }

        public IntPtr FindChild(int childID)
        {
            var handle = GetDlgItem(Handle, childID);
            return handle;
        }

        public void SendMessage(IntPtr? hWnd, WindowMessageType messageType)
        {
            SendMessage(hWnd, messageType, IntPtr.Zero, IntPtr.Zero);
        }

        public void SendMessage(IntPtr? hWnd, WindowMessageType messageType, string text)
        {
            SendMessage(hWnd, messageType, IntPtr.Zero, text);
        }

        public void SendMessage(IntPtr? hWnd, WindowMessageType messageType, IntPtr wParam, string content)
        {
            if (!hWnd.HasValue || hWnd == IntPtr.Zero)
                return;
            SetForegroundWindow(Handle);
            Thread.Sleep(2);
            var ret = SendMessage(hWnd.Value, (uint)messageType, wParam, content);
        }

        public void SendMessage(IntPtr? hWnd, WindowMessageType messageType, IntPtr wParam, IntPtr lParam)
        {
            if (!hWnd.HasValue || hWnd == IntPtr.Zero)
                return;
            SetForegroundWindow(Handle);
            Thread.Sleep(2);
            var ret = PostMessage(hWnd.Value, (uint)messageType, wParam, lParam);
        }

        public string GetControlContent(IntPtr? hWnd)
        {
            if (!hWnd.HasValue || hWnd == IntPtr.Zero)
                return "";
            SetForegroundWindow(Handle);
            Thread.Sleep(2);
            int len = (int)SendMessage(hWnd.Value, (uint)WindowMessageType.WM_GetTextLength, IntPtr.Zero, new StringBuilder());
            StringBuilder builder = new StringBuilder(len + 1);
            SendMessage(hWnd.Value, (uint)WindowMessageType.WM_GetText, (IntPtr)builder.Capacity, builder);
            return builder.ToString().Trim();
        }

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr FindWindowA(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetDlgCtrlID(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    public enum WindowMessageType : uint
    {
        WM_Close = 0x10,
        WM_LeftButtonDown = 0x201,
        WM_LeftButtonUp = 0x202,
        WM_SetText = 0x000C,
        WM_GetText = 0x000D,
        WM_GetTextLength = 0x000E,
    }

    public class ControlInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public string ClassName { get; set; }
    }
}
