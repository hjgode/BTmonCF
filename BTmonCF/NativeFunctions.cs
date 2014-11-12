/*======================================================================

UNLESS OTHERWISE AGREED TO IN A SIGNED WRITING BY HONEYWELL INTERNATIONAL INC
(“HONEYWELL”) AND THE USER OF THIS CODE, THIS CODE AND INFORMATION IS PROVIDED
"AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING
BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS
FOR A PARTICULAR PURPOSE.

COPYRIGHT (C) 2014 HONEYWELL INTERNATIONAL INC.

THIS SOFTWARE IS PROTECTED BY COPYRIGHT LAWS OF THE UNITED STATES OF
AMERICA AND OF FOREIGN COUNTRIES. THIS SOFTWARE IS FURNISHED UNDER A
LICENSE AND/OR A NONDISCLOSURE AGREEMENT AND MAY BE USED IN ACCORDANCE
WITH THE TERMS OF THOSE AGREEMENTS. UNAUTHORIZED REPRODUCTION,  DUPLICATION
OR DISTRIBUTION OF THIS SOFTWARE, OR ANY PORTION OF IT  WILL BE PROSECUTED
TO THE MAXIMUM EXTENT POSSIBLE UNDER THE LAW.

======================================================================*/
#pragma warning disable 0168, 0169, 0649

using System;

using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;
using System.Threading;

using DWORD = System.UInt32;
using BOOL = System.Boolean;
using BYTE = System.Byte;
using HANDLE = System.IntPtr;
using USHORT = System.UInt16;
using UCHAR = System.Byte;

namespace BTmonCF
{
    public partial class BTmon : System.Windows.Forms.Control
    {
        #region NativeStuff
        [DllImport("coredll.dll", SetLastError = true)]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        const UInt32 WAIT_INFINITE = 0xFFFFFFFF;
        enum Wait_Object
        {
            WAIT_ABANDONED = 0x00000080,
            WAIT_OBJECT_0 = 0x00000000,
            WAIT_TIMEOUT = 0x00000102,
        }

        [DllImport("coredll.dll")]
        static extern IntPtr CreateMsgQueue(string szName, ref MSGQUEUEOPTIONS pOptions);
        [DllImport("coredll.dll")]
        static extern IntPtr CreateMsgQueue(IntPtr hString, ref MSGQUEUEOPTIONS pOptions);

        [DllImport("coredll.dll", SetLastError = true)]
        internal static extern bool ReadMsgQueue(IntPtr hMsgQ, IntPtr lpBuffer, int cbBufferSize, out int lpNumberOfBytesRead, int dwTimeout, out int pdwFlags);

        [DllImport("coredll.dll")]
        static extern BOOL CloseMsgQueue(HANDLE h);

        [DllImport("coredll.dll", SetLastError = true)]
        static extern IntPtr RequestBluetoothNotifications(BTE_CLASSES dwClass, IntPtr hMsgQueue);

        [DllImport("coredll.dll")]
        static extern BOOL StopBluetoothNotifications(HANDLE h);


        public struct BT_ADDR
        {
            public byte b1;
            public byte b2;
            public byte b3;
            public byte b4;
            public byte b5;
            public byte b6;
            public byte b7;
            public byte b8;
            public override string ToString()
            {
                string s = String.Format("{0:x02}:{1:x02}:{2:x02}:{3:x02}:{4:x02}:{5:x02}:{6:x02}:{7:x02}", b8, b7, b6, b5, b4, b3, b2, b1);
                return s;
            }
            public byte[] toArray()
            {
                byte[] address = new byte[8];
                address[0]=b8 ;
                address[1]=b7 ;
                address[2]=b6 ;
                address[3]=b5 ;
                address[4]=b4 ;
                address[5]=b3 ;
                address[6]=b2 ;
                address[7]=b1;
                return address;
            }
        }
        public class BT_MAC
        {
            BT_ADDR btaddr;
            public BT_MAC()
            {
                btaddr.b1 = 0;
                btaddr.b2 = 0;
                btaddr.b3 = 0;
                btaddr.b4 = 0;
                btaddr.b5 = 0;
                btaddr.b6 = 0;
                btaddr.b7 = 0;
                btaddr.b8 = 0;

            }
            public BT_ADDR getBT_ADDR()
            {
                return this.btaddr;
            }
            public override string ToString()
            {
                return btaddr.ToString();
            }
            public BT_MAC(byte[] address)
            {
                if (address.Length != 8)
                    throw new ArgumentException("address has to have byte[8]");
                btaddr.b8 = address[0];
                btaddr.b7 = address[1];
                btaddr.b6 = address[2];
                btaddr.b5 = address[3];
                btaddr.b4 = address[4];
                btaddr.b3 = address[5];
                btaddr.b2 = address[6];
                btaddr.b1 = address[7];
            }
            public bool Equals(byte[] address)
            {
                if (address.Length != 8)
                    return false;
                bool bRet = true;
                byte[] address1 = btaddr.toArray();
                for (int i = 0; i < 8; i++)
                {
                    if (address[i] != address1[i])
                    {
                        bRet = false;
                        break;
                    }
                }
                return bRet;
            }
        }

        //see bt_api.h
        [Flags]
        enum BTE_CLASSES : uint
        {
            BTE_CLASS_CONNECTIONS = 0x0001,
                BTE_CONNECTION = 100,
                BTE_DISCONNECTION = 101,
                BTE_ROLE_SWITCH = 102,
                BTE_MODE_CHANGE = 103,
                BTE_PAGE_TIMEOUT = 104,
                BTE_CONNECTION_FAILED = 105,
                BTE_CONNECTION_AUTH_FAILURE = 105,

            BTE_CLASS_PAIRING = 0x0002,
                BTE_KEY_NOTIFY = 200,
                BTE_KEY_REVOKED = 201,

            BTE_CLASS_DEVICE = 0x0004,
                BTE_LOCAL_NAME = 300,
                BTE_COD = 301,

            BTE_CLASS_STACK = 0x0008,
                BTE_STACK_UP = 400,
                BTE_STACK_DOWN = 401,

            BTE_CLASS_SERVICE = 0x0100,
                BTE_SERVICE_CONNECTION_REQUEST = 900,
                BTE_SERVICE_DISCONNECTION_REQUEST=901,
                BTE_SERVICE_CONNECTION_EVENT =902,
                BTE_SERVICE_DISCONNECTION_EVENT=903,

            BTE_CLASS_AVDTP		=	16,
            /*
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
            [MarshalAs(UnmanagedType.Bool)]
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
        // WINBASE.h header constants
        private const int MSGQUEUE_NOPRECOMMIT = 1;
        private const int MSGQUEUE_ALLOW_BROKEN = 2;

        // MSGQUEUEOPTIONS constants
        private const bool ACCESS_READWRITE = false;
        private const bool ACCESS_READONLY = true;

        #region bt_error_strings
        string[] szBTerror = new string[]{
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
        #endregion
    }
}