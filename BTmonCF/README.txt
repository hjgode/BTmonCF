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

requirements:
	Compact Framework 2.0
	Visual Studio 2005 or 2008
	Windows Forms application
	Windows Mobile 5.x, 6.x, Windows Embedded Handheld 6.5.3
	MS BT stack
	tested on CN51 WEH, firmware version 2.10.02.0051
	
purpose:
	show how to use BT notification queue to watch for BT device 
	disconnect.
	Alternative to sometimes blocking SerialPort.IsOpen function for 
	virtual serial port of a BT device.
	
class name:
	BTmonCF.BTmon
	
usage:
	1. add BTmon object to your code
	2. attach event handler to BTmon object's OnBTchangeEventHandler event
	3. check BTmonEventArgs members for connection state and add. info
		(do not use event handler code update GUI elements directly. Use Invoke instead.
	4. In case of disconnect of a device, close COM port and try re-open in interval
		To verify connection you may also use a simple SerialPort.Write("") with SerialPort.TimeOut=300 (or similar)
	5. Ensure to use Dispose() on BTmon object before trying to exit your app
	
pseudo code sample:
	using BTmonCF;
		BTmon _btMon;	//global var
	    ...
	    _btMon = new BTmon();	//initialize object instance
	    ...    
	    _btMon.OnBTchangeEventHandler += new BTmon.BTmonEventHandler(_btMon_OnBTchangeEventHandler);
	    ...
	    void _btMon_OnBTchangeEventHandler(object sender, BTmon.BTmonEventArgs e)
        {
            addLog(e.message, e);
        }
        