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

using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.IO.Ports;

using BTmonCF;

namespace BTmonCFdemo
{
    public partial class Form1 : Form
    {
        BTmon _btMon;
        SerialPort serialPort;
        BTmon.BT_MAC btMAC=new BTmon.BT_MAC();  //empty MAC
        string serialPortString = "COM4";

        Timer timer1=new Timer();

        public Form1()
        {
            InitializeComponent();
            _btMon = new BTmon();            
            
            //watch for changes of BT device with MAC=00:1d:df:54:c5:c5:00:00
            btMAC = new BTmon.BT_MAC(new byte[] { 00, 0x1d, 0xdf, 0x54, 0xc5, 0xc5, 0x00, 0x00 });
            System.Diagnostics.Debug.WriteLine(btMAC.ToString());

            _btMon.OnBTchangeEventHandler += new BTmon.BTmonEventHandler(_btMon_OnBTchangeEventHandler);

            //BTmonCF.BluetoothPort btPort = new BluetoothPort("COM", 6, btMAC.getBT_ADDR());
            //serialPortString = btPort.szComPort;
            if (serialPortString != "")
                openPort();
            else
                addLog("Register COM port failed!", new BTmon.BTmonEventArgs("",true));

            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 1000;
            timer1.Enabled = true;
        }

        void openPort()
        {
            closePort();
            try
            {
                serialPort = new SerialPort(serialPortString);
                serialPort.WriteTimeout= 300;
                serialPort.ReadTimeout = 300;
                serialPort.Open();
                addLog("Serial Port " + serialPortString + " opened", new BTmon.BTmonEventArgs("", true));
            }
            catch (Exception ex)
            {
                addLog("Serial Port " + serialPortString + " open failed with "+ex.Message, new BTmon.BTmonEventArgs("",false));
                closePort();
            }
        }

        void closePort()
        {
            if (serialPort != null)
            {
                serialPort.Close();
                serialPort.Dispose();
                serialPort = null;
            }
        }

        void timer1_Tick(object sender, EventArgs e)
        {
            if (serialPort == null)
                openPort();
            if (serialPort != null)
            {
                try
                {
                    serialPort.Write("");
                    string s = serialPort.ReadExisting();
                }
                catch (Exception)
                {
                    openPort();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        bool lastConnectState = false;
        void _btMon_OnBTchangeEventHandler(object sender, BTmon.BTmonEventArgs e)
        {
            addLog(e.message, e);
            if (lastConnectState != e.connected)
            {
                openPort();
                lastConnectState = e.connected;
            }
            //txtLog.Text += e.message + "\r\n";
        }

        bool bLastState = false;

        delegate void SetTextCallback(string text, BTmon.BTmonEventArgs e);
        public void addLog(string text, BTmon.BTmonEventArgs e)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.txtLog.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(addLog);
                this.Invoke(d, new object[] { text, e });
            }
            else
            {
                if (e.connected)
                {
                    if (bLastState != e.connected)
                    {
                        bLastState = e.connected;
                        openPort();
                    }
                    if (btMAC.Equals(e.btMAC))  //a disconnect does only know the handle, no BT_MAC
                        panel1.BackColor = Color.LightGreen;
                }
                else
                {
                    panel1.BackColor = Color.LightPink;
                    bLastState = false;
                }
                if (txtLog.Text.Length > 20000)
                    txtLog.Text = "";
                txtLog.Text += text + "\r\n";
                txtLog.SelectionLength = 0;
                txtLog.SelectionStart = txtLog.Text.Length - 1;
                txtLog.ScrollToCaret();
            }
        }

        private void Form1_Closing(object sender, CancelEventArgs e)
        {
            _btMon.Dispose();   //use to let the thread not block your exit!

            closePort();
        }

    }
}