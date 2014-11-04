using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using BTmonCF;

namespace BTmonCFdemo
{
    public partial class Form1 : Form
    {
        btmon _btMon;
        public Form1()
        {
            InitializeComponent();
            _btMon = new btmon();
            _btMon.OnBTchangeEventHandler += new btmon.BTmonEventHandler(_btMon_BTchangeEventHandler);
        }

        void _btMon_BTchangeEventHandler(object sender, btmon.BTmonEventArgs e)
        {
            addLog(e.message);
        }
        delegate void SetTextCallback(string text);
        public void addLog(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.txtLog.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(addLog);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (txtLog.Text.Length > 20000)
                    txtLog.Text = "";
                txtLog.Text += text + "\r\n";
                txtLog.SelectionLength = 0;
                txtLog.SelectionStart = txtLog.Text.Length - 1;
                txtLog.ScrollToCaret();
            }
        }
    }
}