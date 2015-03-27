using System;
using System.Web;
using System.Web.Mvc;
using Neodynamic.SDK.Web;

namespace WCPTest.Controllers
{
    public class CodeGeneratorController : Controller
    {
        private readonly Inspection _db = new Inspection();

        public ActionResult Start()
        {
            string lastUtilityPrinted = "SB." + _db.Utilities.Find(1).LastUtilityCodePrinted.Trim().ToString();
            ViewData["lastUtilityCodeUsed"] = lastUtilityPrinted;
            return View();
        }

      

        public void PrintCommands(string sid, FormCollection formCollection, int qty)
        {
            int lastUtilityPrinted = Convert.ToInt32(_db.Utilities.Find(1).LastUtilityCodePrinted.Trim().ToString());
            int numberOfUtilityCodes = Convert.ToInt32(formCollection["qty"]);

           

            int value = lastUtilityPrinted;
            string cpjString = "";
            for (var i = 0; i < numberOfUtilityCodes; i++)
            {
                //Build the printer codes, padding with zeros to left of utility code
                value = value + 1;
                var paddingNeeded = 6 - value.ToString().Length;
                var decimalLength = value.ToString("D").Length + paddingNeeded;
                string printcode = value.ToString("D" + decimalLength.ToString());
                cpjString += "0x02L"; //<STX>L
                cpjString += "0x02n"; //Use imperial inches as measure
                cpjString += "D11";   //1x1 dot format        
                cpjString += "0x0D1711A1600650054Somei";// + printcode;
                cpjString += "0x0D";  //Carriage Return
                cpjString += "1e00";  // 1=no rotation E=Code128 11=repeats D11 above
                cpjString += "040";   //Barcode height 0.4 inches
                cpjString += "0022";  //Row Address Y-value Up and down 0015
                cpjString += "0009";  //Column Address X-Value Left and Right
                cpjString += "SB." + printcode; //Set printable area to be SB. and printcode var
                cpjString += "0x0D1711A2000000008SB." + printcode;// + printcode;
                cpjString += "0x0D";  //Carriage Return
                cpjString += "E";     //End label and execute printing
            }


            if (!WebClientPrint.ProcessPrintJob(System.Web.HttpContext.Current.Request)) return;

            HttpApplicationStateBase app = HttpContext.Application;

            //Create a ClientPrintJob obj that will be processed at the client side by the WCPP
            ClientPrintJob cpj = new ClientPrintJob();

            //get printer commands for this user id
            cpj.ClientPrinter = new DefaultPrinter();
            cpj.PrinterCommands = cpjString.ToString();
            cpj.FormatHexValues = true;

            //get printer settings for this user id
            //int printerTypeId = (int)app[sid + PRINTER_ID];

            //hard coding to 0 because line above throws null exception error
            int printerTypeId = 0;

            if (printerTypeId == 0) //use default printer
            {
                cpj.ClientPrinter = new DefaultPrinter();
            }
            else if (printerTypeId == 1) //show print dialog
            {
                cpj.ClientPrinter = new UserSelectedPrinter();
            }
            else if (printerTypeId == 2) //use specified installed printer
            {
                cpj.ClientPrinter = new InstalledPrinter(app[sid + INSTALLED_PRINTER_NAME].ToString());
            }
            else if (printerTypeId == 3) //use IP-Ethernet printer
            {
                cpj.ClientPrinter = new NetworkPrinter(app[sid + NET_PRINTER_HOST].ToString(), int.Parse(app[sid + NET_PRINTER_PORT].ToString()));
            }
            else if (printerTypeId == 4) //use Parallel Port printer
            {
                cpj.ClientPrinter = new ParallelPortPrinter(app[sid + PARALLEL_PORT].ToString());
            }
            else if (printerTypeId == 5) //use Serial Port printer
            {
                cpj.ClientPrinter = new SerialPortPrinter(app[sid + SERIAL_PORT].ToString(),
                    int.Parse(app[sid + SERIAL_PORT_BAUDS].ToString()),
                    (System.IO.Ports.Parity)Enum.Parse(typeof(System.IO.Ports.Parity), app[sid + SERIAL_PORT_PARITY].ToString()),
                    (System.IO.Ports.StopBits)Enum.Parse(typeof(System.IO.Ports.StopBits), app[sid + SERIAL_PORT_STOP_BITS].ToString()),
                    int.Parse(app[sid + SERIAL_PORT_DATA_BITS].ToString()),
                    (System.IO.Ports.Handshake)Enum.Parse(typeof(System.IO.Ports.Handshake), app[sid + SERIAL_PORT_FLOW_CONTROL].ToString()));
            }

            //Send ClientPrintJob back to the client
            cpj.SendToClient(System.Web.HttpContext.Current.Response);
        }


        const string PRINTER_ID = "-PID";
        const string INSTALLED_PRINTER_NAME = "-InstalledPrinterName";
        const string NET_PRINTER_HOST = "-NetPrinterHost";
        const string NET_PRINTER_PORT = "-NetPrinterPort";
        const string PARALLEL_PORT = "-ParallelPort";
        const string SERIAL_PORT = "-SerialPort";
        const string SERIAL_PORT_BAUDS = "-SerialPortBauds";
        const string SERIAL_PORT_DATA_BITS = "-SerialPortDataBits";
        const string SERIAL_PORT_STOP_BITS = "-SerialPortStopBits";
        const string SERIAL_PORT_PARITY = "-SerialPortParity";
        const string SERIAL_PORT_FLOW_CONTROL = "-SerialPortFlowControl";
        const string PRINTER_COMMANDS = "-PrinterCommands";

        [HttpPost]
        public void ClientPrinterSettings(string sid,
                                             string pid,
                                             string installedPrinterName,
                                             string netPrinterHost,
                                             string netPrinterPort,
                                             string parallelPort,
                                             string serialPort,
                                             string serialPortBauds,
                                             string serialPortDataBits,
                                             string serialPortStopBits,
                                             string serialPortParity,
                                             string serialPortFlowControl,
                                             string printerCommands,
                                             int qty)
        {
            try
            {
                HttpApplicationStateBase app = HttpContext.Application;

                // save settings in the global Application obj

                // save the type of printer selected by the user
                int i = int.Parse(pid);
                app[sid + PRINTER_ID] = i;

                if (i == 2)
                {
                    app[sid + INSTALLED_PRINTER_NAME] = installedPrinterName;
                }
                else if (i == 3)
                {
                    app[sid + NET_PRINTER_HOST] = netPrinterHost;
                    app[sid + NET_PRINTER_PORT] = netPrinterPort;
                }
                else if (i == 4)
                {
                    app[sid + PARALLEL_PORT] = parallelPort;
                }
                else if (i == 5)
                {
                    app[sid + SERIAL_PORT] = serialPort;
                    app[sid + SERIAL_PORT_BAUDS] = serialPortBauds;
                    app[sid + SERIAL_PORT_DATA_BITS] = serialPortDataBits;
                    app[sid + SERIAL_PORT_FLOW_CONTROL] = serialPortFlowControl;
                    app[sid + SERIAL_PORT_PARITY] = serialPortParity;
                    app[sid + SERIAL_PORT_STOP_BITS] = serialPortStopBits;
                }

                //save the printer commands specified by the user
                app[sid + PRINTER_COMMANDS] = printerCommands;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}