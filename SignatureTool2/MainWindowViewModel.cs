// SignTool.MainWindowViewModel
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Devices;
using SignatureTool2.View;
using SignatureTool2.View.Setting;
using SignatureTool2.View.Signature;
using SignatureTool2.ViewModel;
using SignatureTool2.ViewModel.Setting;
using SignatureTool2.ViewModel.Signature;
using SnappyWinscard;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SignatureTool2
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<TabModel> Tables { get; }

        public MainWindowViewModel()
        {
            Tables = new ObservableCollection<TabModel>();
            Init();
            InitSmartCard();
            DeviceNotification.OnDeviceArrival().Subscribe(d => DeviceChanged(d));
        }

        private void InitSmartCard()
        {
            CardIo1 card = new CardIo1();
            var list = card.ListReaders();
            if(list!=null)
            {
                foreach (var item in list)
                {
                    card.SelectDevice(item);
                    var id = card.GetCardUID();
                    if (id == "68810000")
                    {
                        SafeNetTool.GemooIn = true;
                    }
                }

            }
        }

        private void DeviceChanged(DeviceInterfaceChangeInfo deviceinfo)
        {
            if (deviceinfo != null)
            {
                if (deviceinfo.Device.DeviceClass == Dapplo.Windows.Devices.Enums.DeviceInterfaceClass.SmartCardFilter)
                {
                    InitSmartCard();
                    //检测到插入Key
                    if (deviceinfo.Device.DisplayName.Contains("6&35f002a8&0&02"))
                    {
                        //imobie
                        if (deviceinfo.EventType == Dapplo.Windows.Devices.Enums.DeviceChangeEvent.DeviceArrival)
                            SafeNetTool.iMobieIn = true;
                        else if (deviceinfo.EventType == Dapplo.Windows.Devices.Enums.DeviceChangeEvent.DeviceRemoveComplete)
                            SafeNetTool.iMobieIn = false;
                    }
                    else if (deviceinfo.Device.DisplayName.Contains("6&35f002a8&1&01"))
                    {
                        //Gemoo
                        if (deviceinfo.EventType == Dapplo.Windows.Devices.Enums.DeviceChangeEvent.DeviceArrival)
                            SafeNetTool.GemooIn = true;
                        else if (deviceinfo.EventType == Dapplo.Windows.Devices.Enums.DeviceChangeEvent.DeviceRemoveComplete)
                            SafeNetTool.GemooIn = false;

                    }
                }
            }

        }

        private void Init()
        {
            TabModel tabModel = new TabModel
            {
                IsSelected = true,
                TabName = "批量签名"
            };
            SignatureView signatureView = new SignatureView();
            signatureView.DataContext = new SignatureViewModel();
            tabModel.TabContent = signatureView;
            Tables.Add(tabModel);
            tabModel = new TabModel
            {
                TabName = "自定义签名"
            };
            SelectFileSignatureView selectFileSignatureView = new SelectFileSignatureView();
            selectFileSignatureView.DataContext = new SelectFileSignatureViewModel();
            tabModel.TabContent = selectFileSignatureView;
            Tables.Add(tabModel);
            tabModel = new TabModel
            {
                TabName = "编译安装包"
            };
            SetupSignatureView setupSignatureView = new SetupSignatureView();
            setupSignatureView.DataContext = new SetupSignatureViewModel();
            tabModel.TabContent = setupSignatureView;
            Tables.Add(tabModel);
            tabModel = new TabModel
            {
                TabName = "编译器设置"
            };
            CompilerSettingView compilerSettingView = new CompilerSettingView();
            compilerSettingView.DataContext = new CompilerSettingViewModel();
            tabModel.TabContent = compilerSettingView;
            Tables.Add(tabModel); 
            tabModel = new TabModel
            {
                TabName = "混淆器设置"
            };
            ProtecterSettingView protecterSettingView = new ProtecterSettingView();
            protecterSettingView.DataContext = new ProtecterSettingViewModel();
            tabModel.TabContent = protecterSettingView;
            Tables.Add(tabModel);
            tabModel = new TabModel
            {
                TabName = "公司设置"
            };
            CompanySettingView companySettingView = new CompanySettingView();
            companySettingView.DataContext = new CompanySettingViewModel();
            tabModel.TabContent = companySettingView;
            Tables.Add(tabModel);
        }
    }

    internal class SafeNetTool
    {
        public static bool GemooIn { get; internal set; }
        public static bool iMobieIn { get; internal set; }

    }
    public class CardIo1 : INotifyPropertyChanged
    {
        public enum ReaderState
        {
            Unavailable,
            NoCard,
            CardReady
        }

        private int CurrentState;

        private uint retCode;

        private int swInt;

        private const int SwOk = 36864;

        private const int SwUnknown = -1;

        private const int SwNoContext = -2;

        private ReaderState currentReaderState;

        private int hContext;

        private int hCard;

        private int Protocol;

        private Winscard.SCARD_IO_REQUEST pioSendRequest;

        private string currentDevice;

        public ReaderState CurrentReaderState
        {
            get
            {
                return currentReaderState;
            }
            set
            {
                if (currentReaderState != value)
                {
                    currentReaderState = value;
                    this.ReaderStateChanged?.Invoke(value);
                    NotifyPropertyChanged("CurrentReaderState");
                }
            }
        }

        public ObservableCollection<string> Devices { get; } = new ObservableCollection<string>();


        public string CurrentDevice
        {
            get
            {
                return currentDevice;
            }
            set
            {
                currentDevice = value;
                NotifyPropertyChanged("CurrentDevice");
            }
        }

        private string ReaderName
        {
            get
            {
                if (currentDevice == null)
                {
                    return "\\\\?PnP?\\Notification";
                }

                return currentDevice;
            }
        }

        public string StatusText => Winscard.GetScardErrMsg(retCode);

        public string SubStatusText => swInt switch
        {
            36864 => "Success",
            25344 => "Failed",
            27265 => "Not supported",
            _ => "Unexpected",
        };

        public event Action<ReaderState> ReaderStateChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public CardIo1()
        {
            Initialize();
            Task.Run(delegate
            {
                HandleCardStatus();
            });
        }

        public List<string> ListReaders()
        {
            int pcchReaders = 0;
            List<string> list = new List<string>();
            retCode = Winscard.SCardListReaders(hContext, null, null, ref pcchReaders);
            if (retCode != 0)
            {
                return null;
            }

            byte[] array = new byte[pcchReaders];
            retCode = Winscard.SCardListReaders(hContext, null, array, ref pcchReaders);
            if (retCode != 0)
            {
                return null;
            }

            string text = "";
            int i = 0;
            if (pcchReaders <= 0)
            {
                return null;
            }

            for (; array[i] != 0; i++)
            {
                for (; array[i] != 0; i++)
                {
                    string text2 = text;
                    char c = (char)array[i];
                    text = text2 + c;
                }

                list.Add(text);
                text = "";
            }

            return list;
        }

        private void Initialize()
        {
            CurrentState = 0;
            pioSendRequest.dwProtocol = 0;
            pioSendRequest.cbPciLength = 8;
            retCode = Winscard.SCardEstablishContext(2, 0, 0, ref hContext);
            if (retCode != 0)
            {
                swInt = -2;
            }
            else
            {
                SelectDevice("");
            }
        }

        public bool ConnectCard()
        {
            retCode = Winscard.SCardConnect(hContext, currentDevice, 2, 3, ref hCard, ref Protocol);
            swInt = 0;
            uint num = retCode;
            if (num != 0)
            {
                if (num == 2148532254u)
                {
                    Initialize();
                }

                return false;
            }

            return true;
        }

        public void SelectDevice(string device)
        {
            List<string> list = ListReaders();
            Devices.Clear();
            if (list != null)
            {
                list.ForEach(delegate (string d)
                {
                    Devices.Add(d);
                });
                if (string.IsNullOrEmpty(device))
                {
                    if (!list.Contains(currentDevice))
                    {
                        CurrentDevice = list?[0].ToString();
                    }
                }
                else
                {
                    if (list.Contains(device))
                        CurrentDevice = device;
                }

            }
        }

        public string GetCardUID()
        {
            if (ConnectCard())
            {
                string text = "";
                byte[] array = new byte[256];
                Winscard.SCARD_IO_REQUEST sCARD_IO_REQUEST = default(Winscard.SCARD_IO_REQUEST);
                sCARD_IO_REQUEST.dwProtocol = 2;
                sCARD_IO_REQUEST.cbPciLength = Marshal.SizeOf(typeof(Winscard.SCARD_IO_REQUEST));
                Winscard.SCARD_IO_REQUEST sCARD_IO_REQUEST2 = sCARD_IO_REQUEST;
                byte[] sendBuff = new byte[5] { 255, 202, 0, 0, 0 };
                int RecvLen = array.Length;
                if (SCardTransmit(sendBuff, array, ref sCARD_IO_REQUEST2, ref RecvLen))
                {
                    return array.Take(4).Aggregate("", (string a, byte b) => a += b.ToString("x2"));
                }

                return "Error";
            }

            return null;
        }

        public void StoreKey(byte[] key, byte keySlot)
        {
            if (key.Length != 6)
            {
                throw new ArgumentException("Key must be 6 bytes long", "key");
            }

            byte[] array = new byte[11]
            {
                255, 130, 0, keySlot, 6, 0, 0, 0, 0, 0,
                0
            };
            key.CopyTo(array, 5);
            int RecvLen = 2;
            byte[] recvBuff = new byte[RecvLen];
            SCardTransmit(array, recvBuff, ref pioSendRequest, ref RecvLen);
        }

        public bool AuthenticateBlock(byte Block, byte KeyType, byte KeySlot)
        {
            byte[] obj = new byte[10] { 255, 134, 0, 0, 5, 1, 0, 0, 0, 0 };
            obj[7] = Block;
            obj[8] = KeyType;
            obj[9] = KeySlot;
            byte[] sendBuff = obj;
            byte[] array = new byte[2];
            int RecvLen = array.Length;
            return SCardTransmit(sendBuff, array, ref pioSendRequest, ref RecvLen);
        }

        private bool SCardTransmit(byte[] SendBuff, byte[] RecvBuff, ref Winscard.SCARD_IO_REQUEST pioSendRequest, ref int RecvLen)
        {
            retCode = Winscard.SCardTransmit(hCard, ref pioSendRequest, ref SendBuff[0], SendBuff.Length, ref pioSendRequest, ref RecvBuff[0], ref RecvLen);
            ReadSw(RecvBuff, RecvLen - 2);
            return retCode == 0;
        }

        private void ReadSw(byte[] buff, int i)
        {
            if (buff.Length - i < 2)
            {
                swInt = -1;
            }
            else
            {
                swInt = buff[i] * 256 + buff[i + 1];
            }
        }

        public byte[] ReadCardBlock(byte block, byte keyType, byte keySlot)
        {
            if (AuthenticateBlock(block, keyType, keySlot))
            {
                return ReadCardBlock1(block);
            }

            return null;
        }

        private byte[] ReadCardBlock1(byte block)
        {
            byte[] sendBuff = new byte[5] { 255, 176, 0, block, 16 };
            int RecvLen = 18;
            byte[] array = new byte[RecvLen];
            if (SCardTransmit(sendBuff, array, ref pioSendRequest, ref RecvLen) && swInt == 36864)
            {
                return array.Take(RecvLen - 2).ToArray();
            }

            return null;
        }

        public void WriteCardBlock(byte[] data, byte block, byte keyType, byte keySlot)
        {
            if (data != null && AuthenticateBlock(block, keyType, keySlot))
            {
                byte[] array = new byte[5 + (byte)data.Length];
                array[0] = byte.MaxValue;
                array[1] = 214;
                array[2] = 0;
                array[3] = block;
                array[4] = (byte)data.Length;
                data.CopyTo(array, 5);
                int RecvLen = 2;
                byte[] recvBuff = new byte[RecvLen];
                SCardTransmit(array, recvBuff, ref pioSendRequest, ref RecvLen);
            }
        }

        public void HandleCardStatus()
        {
            while (true)
            {
                Winscard.SCARD_READERSTATE[] array = new Winscard.SCARD_READERSTATE[1];
                array[0].RdrName = ReaderName;
                array[0].RdrCurrState = CurrentState;
                switch (Winscard.SCardGetStatusChange(hContext, uint.MaxValue, array, 1))
                {
                    case 0u:
                        {
                            if (ReaderName == "\\\\?PnP?\\Notification")
                            {
                                SelectDevice("");
                                break;
                            }

                            bool flag = (array[0].RdrEventState & 0x20) != 0;
                            ReaderState readerState2 = (CurrentReaderState = (((array[0].RdrEventState & 8) == 0) ? ((!flag) ? ReaderState.NoCard : ReaderState.CardReady) : ReaderState.Unavailable));
                            CurrentState = array[0].RdrEventState;
                            break;
                        }
                    case 2148532254u:
                        Initialize();
                        break;
                }
                Thread.Sleep(100);
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
