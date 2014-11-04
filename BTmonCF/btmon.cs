using System;

using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;
using System.Threading;

using DWORD = System.UInt32;
using BOOL = System.Boolean;
using BYTE = System.Byte;
using HANDLE = System.IntPtr;

namespace BTmonCF
{

    public class btmon:System.Windows.Forms.Control
    {
        #region NativeStuff
        /*
        HANDLE CreateMsgQueue(
          LPCWSTR lpszName,
          LPMSGQUEUEOPTIONS lpOptions
        );
        typedef MSGQUEUEOPTIONS_OS{
          DWORD dwSize;
          DWORD dwFlags;
          DWORD dwMaxMessages;
          DWORD cbMaxMessage;
          BOOL bReadAccess;
        } MSGQUEUEOPTIONS, FAR* LPMSGQUEUEOPTIONS, *PMSGQUEUEOPTIONS;
        HANDLE RequestBluetoothNotifications(
          DWORD dwClass,
          HANDLE hMsgQ
        );
        */
        [DllImport("coredll.dll", SetLastError = true)]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        const UInt32 INFINITE = 0xFFFFFFFF;
        enum Wait_Object{
            WAIT_ABANDONED = 0x00000080,
            WAIT_OBJECT_0 = 0x00000000,
            WAIT_TIMEOUT = 0x00000102,
        }

        [DllImport("coredll.dll")]
        static extern IntPtr CreateMsgQueue(string szName, ref MSGQUEUEOPTIONS pOptions);
        [DllImport("coredll.dll")]
        static extern IntPtr CreateMsgQueue(IntPtr hString, ref MSGQUEUEOPTIONS pOptions);

        [DllImport("coredll.dll")]
        static extern BOOL CloseMsgQueue(HANDLE h);

        [DllImport("coredll.dll", SetLastError=true)]
        static extern IntPtr RequestBluetoothNotifications(BTE_CLASSES dwClass, IntPtr hMsgQueue);
    
        [DllImport("coredll.dll")]
        static extern BOOL StopBluetoothNotifications(HANDLE h);
        
        [StructLayout(LayoutKind.Sequential)]
        struct BTEVENT {
          public DWORD dwEventId;
          public DWORD dwReserved;
          [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
          public BYTE[] baEventData;
        }

        [Flags]
        enum BTE_CLASSES:uint
        {
            BTE_CLASS_CONNECTIONS = 1,
            BTE_CONNECTION = 100,
            BTE_DISCONNECTION = 101,
            BTE_ROLE_SWITCH = 102,
            BTE_MODE_CHANGE = 103,
            BTE_PAGE_TIMEOUT = 104,
            BTE_CONNECTION_FAILED = 105,
            BTE_CONNECTION_AUTH_FAILURE = 105,

            BTE_CLASS_PAIRING	=	2,
            BTE_KEY_NOTIFY		=	200,
            BTE_KEY_REVOKED		=	201,

            BTE_CLASS_DEVICE	=	4,
            BTE_LOCAL_NAME		=	300,
            BTE_COD				=	301,

            BTE_CLASS_STACK		=	8,
            BTE_STACK_UP		=	400,
            BTE_STACK_DOWN		=	401,
/*
            BTE_CLASS_AVDTP		=	16,
            BTE_AVDTP_STATE		=	500,
            BT_AVDTP_STATE_DISCONNECTED     =0,
            BT_AVDTP_STATE_SUSPENDED        =1,
            BT_AVDTP_STATE_STREAMING       = 2,
*/
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MSGQUEUEOPTIONS
        {
            public DWORD dwSize;
            public DWORD dwFlags;
            public DWORD dwMaxMessages;
            public DWORD cbMaxMessage;
            public BOOL bReadAccess;
            //MSGQUEUEOPTIONS()
            //{
            //    dwSize = Marshal.SizeOf(MSGQUEUEOPTIONS);
            //    dwFlags = 0;
            //    dwMaxMessages = 10;
            //    cbMaxMessage = 0;
            //    bReadAccess = true;
            //}
        }

        string[] szBTerror=new string[]{
	        "0x00 undefined",
	        "0x01 Unknown HCI Command.",
	        "0x02 No Connection.",
	        "0x03 Hardware Failure.",
	        "0x04 Page Timeout.",
	        "0x05 Authentication Failure.",
	        "0x06 Key Missing.",
	        "0x07 Memory Full.",
	        "0x08 Connection Timeout.",
	        "0x09 Max Number Of Connections.",
	        "0x0A Max Number Of SCO Connections To A Device.",
	        "0x0B ACL connection already exists.",
	        "0x0C Command Disallowed.",
	        "0x0D Host Rejected due to limited resources.",
	        "0x0E Host Rejected due to security reasons.",
	        "0x0F Host Rejected due to remote device is only a personal device.",
	        "0x10 Host Timeout.",
	        "0x11 Unsupported Feature or Parameter Value.",
	        "0x12 Invalid HCI Command Parameters.",
	        "0x13 Other End Terminated Connection: User Ended Connection.",
	        "0x14 Other End Terminated Connection: Low Resources.",
	        "0x15 Other End Terminated Connection: About to Power Off.",
	        "0x16 Connection Terminated by Local Host.",
	        "0x17 Repeated Attempts.",
	        "0x18 Pairing Not Allowed.",
	        "0x19 Unknown LMP PDU.",
	        "0x1A Unsupported Remote Feature.",
	        "0x1B SCO Offset Rejected.",
	        "0x1C SCO Interval Rejected.",
	        "0x1D SCO Air Mode Rejected.",
	        "0x1E Invalid LMP Parameters.",
	        "0x1F Unspecified Error.",
	        "0x20 Unsupported LMP Parameter Value.",
	        "0x21 Role Change Not Allowed",
	        "0x22 LMP Response Timeout",
	        "0x23 LMP Error Transaction Collision",
	        "0x24 LMP PDU Not Allowed",
	        "0x25 Encryption Mode Not Acceptable",
	        "0x26 Unit Key Used",
	        "0x27 QoS is Not Supported",
	        "0x28 Instant Passed",
	        "x29 Pairing with Unit Key Not Supported",
        };
        #endregion
        System.Threading.Thread msgThread=null;
        bool bRunThread = true;

        public btmon()
        {
            startThread();
        }
        
        void startThread()
        {
            bRunThread = true;
            msgThread = new Thread(new ThreadStart( btMsgThread));
            msgThread.Name = "btmon thread";
            msgThread.Start();
        }

        void stopThread()
        {
            bRunThread = false;
            Thread.Sleep(3000);
            if (msgThread != null)
            {
                msgThread.Abort();
            }
        }
        
        public new void Dispose()
        {
            stopThread();
            base.Dispose();
        }

        void btMsgThread()
        {
            addLog("btmon thread about to start");
            IntPtr hMsgQueue=IntPtr.Zero;
            IntPtr hBTevent = IntPtr.Zero;
            try
            {
                BTEVENT btEvent = new BTEVENT();
                //create msgQueueOptions
                MSGQUEUEOPTIONS msgQueueOptions = new MSGQUEUEOPTIONS();
                msgQueueOptions.dwSize = (DWORD)Marshal.SizeOf(msgQueueOptions);
                msgQueueOptions.dwFlags = 0;
                msgQueueOptions.dwMaxMessages = 10;
                msgQueueOptions.cbMaxMessage = (DWORD)Marshal.SizeOf(btEvent);

                hMsgQueue = CreateMsgQueue(IntPtr.Zero, ref msgQueueOptions);

                if (hMsgQueue == IntPtr.Zero)
                    throw new Exception("Create MsgQueue failed");

                hBTevent = RequestBluetoothNotifications(
                            BTE_CLASSES.BTE_CLASS_CONNECTIONS |
                                BTE_CLASSES.BTE_CONNECTION |
                                BTE_CLASSES.BTE_DISCONNECTION |
                                BTE_CLASSES.BTE_ROLE_SWITCH |
                                BTE_CLASSES.BTE_MODE_CHANGE |
                                BTE_CLASSES.BTE_PAGE_TIMEOUT |
                                BTE_CLASSES.BTE_CONNECTION_AUTH_FAILURE |
                            BTE_CLASSES.BTE_CLASS_PAIRING |
                                BTE_CLASSES.BTE_KEY_NOTIFY |
                                BTE_CLASSES.BTE_KEY_REVOKED |
                            BTE_CLASSES.BTE_CLASS_DEVICE |
                                BTE_CLASSES.BTE_LOCAL_NAME |
                                BTE_CLASSES.BTE_COD |
                            BTE_CLASSES.BTE_CLASS_STACK |
                                BTE_CLASSES.BTE_STACK_UP |
                                BTE_CLASSES.BTE_STACK_DOWN
                        , hMsgQueue
                    );
                if (hBTevent == IntPtr.Zero)
                    throw new Exception("RequestBluetoothNotifications failed");

                Wait_Object waitRes = 0;
                //create a msg queue
                while (bRunThread)
                {
                    waitRes = (Wait_Object)WaitForSingleObject(hBTevent, 5000);
                    if ((int)waitRes == -1)
                    {
                        int iErr = Marshal.GetLastWin32Error();
                        addLog("error in WaitForSingleObject=" + iErr.ToString()); //6 = InvalidHandle
                        Thread.Sleep(1000);
                    }
                    switch (waitRes)
                    {
                        case Wait_Object.WAIT_OBJECT_0:
                            //signaled
                            //check event type and fire event
                            OnBTchanged(new BTmonEventArgs("BTEVENT signaled", false));
                            switch (btEvent.dwEventId)
                            {
                                case BTE_CLASSES.BTE_DISCONNECTION:
                                case BTE_CLASSES.BTE_STACK_DOWN: 
                                break;
                                
                            }
                            break;
                        case Wait_Object.WAIT_ABANDONED:
                            //wait has abandoned
                            OnBTchanged(new BTmonEventArgs("WAIT_ABANDONED", false));
                            break;
                        case Wait_Object.WAIT_TIMEOUT:
                            //timed out
                            OnBTchanged(new BTmonEventArgs(".", false));
                            break;
                    }
//                    System.Threading.Thread.Sleep(3000);
//                    OnBTchanged(new BTmonEventArgs("test", false));
                }
            }
            catch (ThreadAbortException ex)
            {
            }
            catch (Exception ex)
            {
            }
            finally
            {
                StopBluetoothNotifications(hBTevent);
                CloseMsgQueue(hMsgQueue);
            }
            addLog("btmon thread ended");
        }

        void addLog(String str)
        {
            System.Diagnostics.Debug.WriteLine(str);
        }

        public delegate void BTmonEventHandler(Object sender, BTmonEventArgs e);
        public event BTmonEventHandler OnBTchangeEventHandler;

        protected virtual void OnBTchanged(BTmonEventArgs e)
        {
            BTmonEventHandler handler = OnBTchangeEventHandler;
            if (handler != null)
            {
                // Invokes the delegates. 
                handler(this, e);
            }
        }

        public class BTmonEventArgs:EventArgs{
            public string message="";
            public bool connected = false;
            public BTmonEventArgs(string msg, bool connectState)
            {
                this.message = msg;
                this.connected = connectState;
            }
        }

    }
}
