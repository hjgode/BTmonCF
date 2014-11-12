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
    /// <summary>
    /// class to watch async for BT changes
    /// </summary>
    public partial class BTmon:System.Windows.Forms.Control
    {
        System.Threading.Thread msgThread=null;
        bool bRunThread = true;
        object lockObject = new object();
        bool _Connected = true;
        public bool Connected
        {
            get
            {
                lock (lockObject)
                {
                    return _Connected;
                }
            }
        }

        public BTmon()
        {
            addLog(DateTime.Now.ToLongTimeString() + " " + "BTmon class init");
            startThread();
        }

        ~BTmon()
        {
            this.Dispose();
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
            addLog(DateTime.Now.ToLongTimeString() + " " + "BTmon class Dispose()");
            stopThread();
            base.Dispose();
        }

        bool setConnected(bool bConnected)
        {
            lock (lockObject)
            {
                _Connected = bConnected;
            }
            return bConnected;
        }

        void btMsgThread()
        {
            //only BTE_DISCONNECTION and BTE_CONNECTION change this state!
            bool bLastConnectState = _Connected;
            addLog("btmon thread about to start");
            IntPtr hMsgQueue=IntPtr.Zero;
            IntPtr hBTevent = IntPtr.Zero;
            // allocate space to store the received messages
            IntPtr msgBuffer = Marshal.AllocHGlobal(SIZEOF_BTEVENT);

            try
            {
                BTEVENT btEvent = new BTEVENT();
                //create msgQueueOptions
                MSGQUEUEOPTIONS msgQueueOptions = new MSGQUEUEOPTIONS();                
                msgQueueOptions.dwSize = (DWORD)Marshal.SizeOf(msgQueueOptions);
                msgQueueOptions.dwFlags = 0;// MSGQUEUE_NOPRECOMMIT;
                msgQueueOptions.dwMaxMessages = 10;
                msgQueueOptions.cbMaxMessage = (DWORD)Marshal.SizeOf(btEvent);
                msgQueueOptions.bReadAccess = ACCESS_READONLY;

                hMsgQueue = CreateMsgQueue(IntPtr.Zero, ref msgQueueOptions);

                if (hMsgQueue == IntPtr.Zero)
                {
                    addLog("Create MsgQueue failed");
                    throw new Exception("Create MsgQueue failed");
                }
                hBTevent = RequestBluetoothNotifications(
                            BTE_CLASSES.BTE_CLASS_CONNECTIONS |
                                //BTE_CLASSES.BTE_CONNECTION |
                                //BTE_CLASSES.BTE_DISCONNECTION |
                                //BTE_CLASSES.BTE_ROLE_SWITCH |
                                //BTE_CLASSES.BTE_MODE_CHANGE |
                                //BTE_CLASSES.BTE_PAGE_TIMEOUT |
                                //BTE_CLASSES.BTE_CONNECTION_AUTH_FAILURE |
                            BTE_CLASSES.BTE_CLASS_PAIRING |
                                //BTE_CLASSES.BTE_KEY_NOTIFY |
                                //BTE_CLASSES.BTE_KEY_REVOKED |
                            BTE_CLASSES.BTE_CLASS_DEVICE |
                                //BTE_CLASSES.BTE_LOCAL_NAME |
                                //BTE_CLASSES.BTE_COD |
                            BTE_CLASSES.BTE_CLASS_STACK |
                                //BTE_CLASSES.BTE_STACK_UP |
                                //BTE_CLASSES.BTE_STACK_DOWN |
                            BTE_CLASSES.BTE_CLASS_SERVICE |
                            BTE_CLASSES.BTE_CLASS_AVDTP
                        , hMsgQueue
                    );
                addLog("RequestBluetoothNotifications=" + Marshal.GetLastWin32Error().ToString()); //6 = InvalidHandle

                if (hBTevent == IntPtr.Zero)
                    throw new Exception("RequestBluetoothNotifications failed");

                //the different return data types
                BT_connect_event_data bt_connect_data;
                BT_disconnect_event_data bt_disconnect_data;
                BT_mode_changed_event_data bt_mode_changed_data;
                BT_link_key_event_data bt_link_event_data;
                BT_role_switch_event_data bt_role_switch_event_data;

                Wait_Object waitRes = 0;
                //create a msg queue
                while (bRunThread)
                {
                    // initialise values returned by ReadMsgQueue
                    int bytesRead = 0;
                    int msgProperties = 0;
                    //block until message
                    waitRes = (Wait_Object)WaitForSingleObject(hMsgQueue, 5000);
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
                            //ReadMsgQueue entry
                            bool success = ReadMsgQueue(hMsgQueue,   // the open message queue
                                                        msgBuffer,        // buffer to store msg
                                                        SIZEOF_BTEVENT,   // size of the buffer
                                                        out bytesRead,    // bytes stored in buffer
                                                        -1,         // wait forever
                                                        out msgProperties);
                            if (success)
                            {
                                // marshal the data read from the queue into a BTEVENT structure
                                btEvent = (BTEVENT)Marshal.PtrToStructure(msgBuffer, typeof(BTEVENT));
                            }
                            else
                                continue; //start a new while cirlce

                            addLog("BTEVENT signaled: " + ((BTE_CLASSES)(btEvent.dwEventId)).ToString());

                            // we are interested in the event type
                            switch ((BTE_CLASSES)btEvent.dwEventId)
                            #region BTE_EVENT_PROCESSSING
                            {
                                case BTE_CLASSES.BTE_CONNECTION:
                                    bLastConnectState = true;
                                    bt_connect_data = new BT_connect_event_data(btEvent.baEventData);
                                    OnBTchanged(new BTmonEventArgs("connected " + bt_connect_data.bt_addr.ToString() + " 0x" + bt_connect_data.connect_handle.ToString("x08"),
                                        setConnected(bLastConnectState)));
                                    break;
                                case BTE_CLASSES.BTE_DISCONNECTION:
                                    bLastConnectState = false;
                                    bt_disconnect_data = new BT_disconnect_event_data(btEvent.baEventData);
                                    OnBTchanged(new BTmonEventArgs("disconnected 0x" + bt_disconnect_data.connect_handle.ToString("x08"),
                                        setConnected(bLastConnectState)));
                                    break;
                                case BTE_CLASSES.BTE_ROLE_SWITCH:
                                    bt_role_switch_event_data = new BT_role_switch_event_data(btEvent.baEventData);
                                    OnBTchanged(new BTmonEventArgs("role switch 0x" + bt_role_switch_event_data.bt_addr.ToString() +
                                        "role:" + bt_role_switch_event_data._role.ToString(), 
                                        setConnected(bLastConnectState)));
                                    break;
                                case BTE_CLASSES.BTE_STACK_DOWN:
                                    OnBTchanged(new BTmonEventArgs("stack down! " + DateTime.Now.ToLongTimeString(),
                                        setConnected(bLastConnectState)));
                                    break;
                                case BTE_CLASSES.BTE_STACK_UP:
                                    OnBTchanged(new BTmonEventArgs("stack up! " + DateTime.Now.ToLongTimeString(),
                                        setConnected(bLastConnectState)));
                                    break;
                                case BTE_CLASSES.BTE_CONNECTION_FAILED:
                                    bt_connect_data = new BT_connect_event_data(btEvent.baEventData);
                                    OnBTchanged(new BTmonEventArgs("connect failed " + bt_connect_data.bt_addr.ToString() + " 0x" + bt_connect_data.connect_handle.ToString("x08"),
                                        setConnected(bLastConnectState)));
                                    break;
                                case BTE_CLASSES.BTE_KEY_REVOKED:
                                    bt_link_event_data = new BT_link_key_event_data(btEvent.baEventData);
                                    OnBTchanged(new BTmonEventArgs("key revoked " + bt_link_event_data.bt_addr.ToString(),
                                        setConnected(bLastConnectState)));
                                    break;
                                case BTE_CLASSES.BTE_KEY_NOTIFY:
                                    bt_link_event_data = new BT_link_key_event_data(btEvent.baEventData);
                                    OnBTchanged(new BTmonEventArgs("key notify " + bt_link_event_data.bt_addr.ToString(),
                                        setConnected(bLastConnectState)));
                                    break;
                                case BTE_CLASSES.BTE_MODE_CHANGE:
                                    bt_mode_changed_data = new BT_mode_changed_event_data(btEvent.baEventData);
                                    OnBTchanged(new BTmonEventArgs("mode changed " + bt_mode_changed_data.bt_addr.ToString() + " 0x" + bt_mode_changed_data.connect_handle.ToString("x08"),
                                        setConnected(bLastConnectState)));
                                    break;
                            }//dwEventID
                            break;
                            #endregion
                        case Wait_Object.WAIT_ABANDONED:
                            //wait has abandoned
                            addLog("btMon thread: WAIT_ABANDONED");
                            break;
                        case Wait_Object.WAIT_TIMEOUT:
                            //timed out
                            addLog("btMon thread: WAIT_TIMEOUT");
                            break;
                    }//WaitRes
                }//while bRunThread
            }
            catch (ThreadAbortException ex)
            {
                addLog("btMon thread ThreadAbortException: " + ex.Message + "\r\n" + ex.StackTrace);
            }
            catch (Exception ex)
            {
                addLog("btMon thread exception: " + ex.Message + "\r\n" + ex.StackTrace);
            }
            finally
            {
                Marshal.FreeHGlobal(msgBuffer);
                StopBluetoothNotifications(hBTevent);
                CloseMsgQueue(hMsgQueue);
            }
            addLog("btmon thread ended");
        }

        public delegate void BTmonEventHandler(Object sender, BTmonEventArgs e);
        public event BTmonEventHandler OnBTchangeEventHandler;

        /// <summary>
        /// event will be fired for changes in BT connection
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBTchanged(BTmonEventArgs e)
        {
            BTmonEventHandler handler = OnBTchangeEventHandler;
            if (handler != null)
            {
                // Invokes the delegates. 
                handler(this, e);
            }
            addLog("OnBTchanged: " + e.message);
        }

        /// <summary>
        /// event arguments of OnBTchanged event
        /// </summary>
        public class BTmonEventArgs:EventArgs{
            /// <summary>
            /// a general msg text with BT MAC, handle or general information about the change
            /// </summary>
            public string message="";
            private bool _connected = false;
            /// <summary>
            /// var holding the connection state
            /// </summary>
            public bool connected
            {
                public get { return _connected; }
            }
            /// <summary>
            /// var used to hold a BT MAC address of a BT change
            /// </summary>
            public BT_ADDR btMAC;

            public BTmonEventArgs(string msg, bool connectState)
            {
                this.message = msg;
                this._connected = connectState;
                btMAC = new BT_ADDR();
                addLog(DateTime.Now.ToLongTimeString() + ": '" + msg + "' " + connectState.ToString() + " " + btMAC.ToString());
            }
            public BTmonEventArgs(string msg, bool connectState, BT_ADDR btAddr)
            {
                this.message = msg;
                this._connected = connectState;
                this.btMAC = btAddr;
                addLog(DateTime.Now.ToLongTimeString() + ": '" + msg + "' " + connectState.ToString() + " " + btMAC.ToString());
            }
        }

        #region logging
        static string logFile = "\\BTmonCF.log";
        static object lockFile = new object();
        static void addLog(string s)
        {
            System.Diagnostics.Debug.WriteLine(s);
            try
            {
                lock (lockFile)
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(logFile, true))
                    {
                        sw.WriteLine(s);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("addLog: " + ex.Message);
            }
        }
        #endregion
    }
}
