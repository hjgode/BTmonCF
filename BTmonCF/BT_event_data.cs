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
using UINT = System.UInt32;
using BOOL = System.Boolean;
using BYTE = System.Byte;
using HANDLE = System.IntPtr;
using USHORT = System.UInt16;
using UCHAR = System.Byte;

namespace BTmonCF
{
    public partial class BTmon : System.Windows.Forms.Control
    {
        [StructLayout(LayoutKind.Sequential)]
        struct BTEVENT
        {
            public DWORD dwEventId;
            public DWORD dwReserved;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public BYTE[] baEventData;
        }
        // size of a BTEVENT structure:
        //   DWORD dwEventId           = 4 bytes
        //   DWORD dwReserved          = 4 bytes
        //   BYTE  baEventData[64]     = 64 bytes
        private const int SIZEOF_BTEVENT = 72;

        // BT event data type structs
        // each event uses the same data area inside a BT_EVENT but with different meanings
        [StructLayout(LayoutKind.Sequential)]
        struct BT_CONNECT_EVENT
        {
            public DWORD dwSize;         // To keep track of version
            public USHORT hConnection;   // Baseband connection handle
            public BT_ADDR bta;          // Address of remote device
            public UCHAR ucLinkType;     // Link Type (ACL/SCO)
            public UCHAR ucEncryptMode;  // Encryption mode
        }
        
        class BT_connect_event_data
        {
            BT_CONNECT_EVENT _bt_connect_event;
            public BT_ADDR bt_addr{
                get{return _bt_connect_event.bta;}
            }
            public USHORT connect_handle
            {
                get { return _bt_connect_event.hConnection; }
            }
            public BT_connect_event_data(byte[] event_data)
            {
                GCHandle pinnedPacket = GCHandle.Alloc(event_data, GCHandleType.Pinned);
                _bt_connect_event = (BT_CONNECT_EVENT)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), typeof(BT_CONNECT_EVENT));
                pinnedPacket.Free(); //00:1d:df:54:c5:c5
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BT_DISCONNECT_EVENT
        {
            public DWORD dwSize;         // To keep track of version
            public USHORT hConnection;   // Baseband connection handle
            public UCHAR ucReason;       // Reason for disconnection
        }
        class BT_disconnect_event_data
        {
            BT_DISCONNECT_EVENT _bt_disconnect_event;
            public USHORT connect_handle
            {
                get { return _bt_disconnect_event.hConnection; }
            }
            public BT_disconnect_event_data(byte[] event_data)
            {
                GCHandle pinnedPacket = GCHandle.Alloc(event_data, GCHandleType.Pinned);
                _bt_disconnect_event = (BT_DISCONNECT_EVENT)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), typeof(BT_DISCONNECT_EVENT));
                pinnedPacket.Free();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BT_MODE_CHANGE_EVENT
        {
            public DWORD dwSize;         // To keep track of version
            public USHORT hConnection;   // Baseband connection handle
            public BT_ADDR bta;          // Address of remote device
            public BYTE bMode;           // Power mode (sniff, etc)
            public USHORT usInterval;    // Power mode interval 
        }
        class BT_mode_changed_event_data
        {
            BT_MODE_CHANGE_EVENT _bt_mode_changed_event;
            public BT_ADDR bt_addr
            {
                get { return _bt_mode_changed_event.bta; }
            }

            public USHORT connect_handle
            {
                get { return _bt_mode_changed_event.hConnection; }
            }
            public BT_mode_changed_event_data(byte[] event_data)
            {
                GCHandle pinnedPacket = GCHandle.Alloc(event_data, GCHandleType.Pinned);
                _bt_mode_changed_event = (BT_MODE_CHANGE_EVENT)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), typeof(BT_MODE_CHANGE_EVENT));
                pinnedPacket.Free();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BT_LINK_KEY_EVENT
        {
            public DWORD dwSize;        // To keep track of version
            public BT_ADDR bta;         // Address of remote device
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UCHAR[] link_key;  // Link key data
            public UCHAR key_type;      // Link key type
        }
        class BT_link_key_event_data
        {
            BT_LINK_KEY_EVENT _bt_link_key_event;
            public BT_ADDR bt_addr
            {
                get { return _bt_link_key_event.bta; }
            }

            public BT_link_key_event_data(byte[] event_data)
            {
                GCHandle pinnedPacket = GCHandle.Alloc(event_data, GCHandleType.Pinned);
                _bt_link_key_event = (BT_LINK_KEY_EVENT)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), typeof(BT_LINK_KEY_EVENT));
                pinnedPacket.Free();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BT_ROLE_SWITCH_EVENT{
            DWORD dwSize;         // To keep track of version
            public BT_ADDR bta;          // Address of remote device
            public UINT fRole;// : 1;       // New Role (master/slave)
        } 
        class BT_role_switch_event_data
        {
            BT_ROLE_SWITCH_EVENT _bt_role_switch_event;
            public BT_ADDR bt_addr
            {
                get { return _bt_role_switch_event.bta; }
            }
            public UINT _role
            {
                get { return _bt_role_switch_event.fRole; }
            }
            public BT_role_switch_event_data(byte[] event_data)
            {
                GCHandle pinnedPacket = GCHandle.Alloc(event_data, GCHandleType.Pinned);
                _bt_role_switch_event = (BT_ROLE_SWITCH_EVENT)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), typeof(BT_ROLE_SWITCH_EVENT));
                pinnedPacket.Free();
            }
        }
    }
}
