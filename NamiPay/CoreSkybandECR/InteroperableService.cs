using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


namespace CoreSkybandECR
{

    public class InteroperableService
    {

        public InteroperableService() { }
        byte[] terminalResponse = new byte[500];

        [DllImport("ECR.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void pack([MarshalAs(UnmanagedType.LPStr)] string inputReqData, int transactionType, [MarshalAs(UnmanagedType.LPStr)] string szSignature,
             byte[] szEcrBuffer);

        [DllImport("ECR.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void parse(byte[] respData, byte[] szRespFields);

        public Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public SerialPort serial = new SerialPort();
        public static bool transaflag = false;

        public bool checkComConn()
        {
            try
            {
                bool status = serial.IsOpen;
                return status;
            }
            catch (Exception e)
            {
                return false;
            }

        }
        public List<string> GetQualcommPorts()
        {
            List<string> portList = new List<string>();

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%)'"))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        string name = queryObj["Name"]?.ToString();

                        if (!string.IsNullOrEmpty(name))
                        {
                           // string afterreplace = name.Replace("", string.Empty);
                            portList.Add(name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return portList;
        }
        public int doCOMConnection(int port, string baudRateCom, string parityCom, int dataBitsCom, int stopBitsCom)
        {
            string portNumber = "COM" + port.ToString();
            int responseInteger = 1;
            if (serial.IsOpen)
            {
                serial.Close();
                serial.Dispose();
            }
            serial = new SerialPort(portNumber, int.Parse(baudRateCom), (Parity)Enum.Parse(typeof(Parity), parityCom),
                                    dataBitsCom, (StopBits)Enum.Parse(typeof(StopBits), stopBitsCom.ToString()));
            serial.ReadTimeout = 2500;
            serial.WriteTimeout = 1000;
            try
            {
                if (!transaflag)
                {
                    serial.Open();
                    responseInteger = 0;
                    serial.Close();
                }
                else
                {
                    return responseInteger = 0;
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(e.ToString());
                serial.Close();
                serial.Dispose();
            }
            catch (UnauthorizedAccessException un)
            {
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(un.ToString());
                serial.Close();
                serial.Dispose();
            }
            return responseInteger;
        }
        public int doCOMStatusConnection(int port, string baudRateCom, string parityCom, int dataBitsCom, int stopBitsCom)
        {
            string portNumber = "COM" + port.ToString();
            int responseInteger = 1;
            if (serial.IsOpen)
            {
                serial.Close();
                serial.Dispose();
            }
            serial = new SerialPort(portNumber, int.Parse(baudRateCom), (Parity)Enum.Parse(typeof(Parity), parityCom),
                                    dataBitsCom, (StopBits)Enum.Parse(typeof(StopBits), stopBitsCom.ToString()));
            serial.ReadTimeout = 2500;
            serial.WriteTimeout = 1000;
            try
            {

                serial.Open();
                responseInteger = 0;


            }
            catch (IOException e)
            {
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(e.ToString());
                serial.Close();
                serial.Dispose();
            }
            return responseInteger;
        }
        public bool COMStatusConnection(int port, string baudRateCom, string parityCom, int dataBitsCom, int stopBitsCom)
        {
            string portNumber = "COM" + port.ToString();
            bool responseInteger = false;
            var disconnect = doCOMDisconnection();
            if (disconnect == 0)
            {
                serial = new SerialPort(portNumber, int.Parse(baudRateCom), (Parity)Enum.Parse(typeof(Parity), parityCom),
                                        dataBitsCom, (StopBits)Enum.Parse(typeof(StopBits), stopBitsCom.ToString()));
                try
                {

                    serial.Open();
                    responseInteger = true;
                    //serial.Close();

                }
                catch (IOException e)
                {
                    Console.WriteLine("Problem connecting to host");
                    Console.WriteLine(e.ToString());
                    serial.Close();
                    serial.Dispose();
                }
                catch (UnauthorizedAccessException e)
                {
                    serial.Close();
                    serial.Dispose();
                    Console.WriteLine("Unauthorized Access Exception" + e);
                }
                catch (ArgumentException e)
                {
                    serial.Close();
                    serial.Dispose();
                    Console.WriteLine("ArgumentException" + e);
                }
            }
            serial.Close();
            return responseInteger;
        }

        public int doCOMTransaction(int Port, string baudRateCom, string parityCom, int dataBitsCom, int stopBitsCom, string inputReqData, int transactionType, string szSignature, out string response)
        {
            Log.Debug("Entering doCOMTransaction() method with parameters: Port={Port}, baudRateCom={baudRateCom}, parityCom={parityCom}, dataBitsCom={dataBitsCom}, stopBitsCom={stopBitsCom}, transactionType={transactionType}, szSignature={szSignature}", Port, baudRateCom, parityCom, dataBitsCom, stopBitsCom, transactionType, szSignature);

            transaflag = true;
            byte[] terminalResponse = new byte[50000];
            String[] resp1 = new String[500];
            String[] respsend = new String[500];
            string[] responseTemp = new string[1];
            string responseFinal = "";
            string portNumber = "COM" + Port.ToString();
            int responseInteger = 1;
            int resConnect = 1;

            try
            {
                int resDisConnect = doCOMDisconnection();
                if (resDisConnect == 0)
                {
                    //Log.Information("Disconnected successfully. Attempting to establish COM connection on port: {portNumber}", portNumber);
                    resConnect = doCOMStatusConnection(Port, baudRateCom, parityCom, dataBitsCom, stopBitsCom);
                }

                if (resConnect == 0)
                {
                    Log.Information("COM connection established successfully on port: {portNumber}", portNumber);

                    byte[] packResponse = new byte[150];
                    responseInteger = 2;
                    Log.Information("Packing transaction data");
                    pack(inputReqData, transactionType, szSignature, packResponse);

                    byte[] reSizePackResponse = TrimTrailingBytes(packResponse, 0);
                    Log.Information("Packed data resized and ready to send. Data size: {Size}", reSizePackResponse.Length);

                    StringBuilder Hex = new StringBuilder(reSizePackResponse.Length * 2);
                    foreach (byte b in reSizePackResponse)
                        Hex.AppendFormat("{0:x2}", b);
                    string[] sepSend = { "fc" };
                    respsend = Hex.ToString().Split(sepSend, StringSplitOptions.None);
                    for (int i = 0; i < respsend.Length; i++)
                    {
                        Log.Information("respsend[{Index}] = {Value}", i, respsend[i]);
                    }
                    Log.Information("Sending transaction request to terminal on COM port");

                    responseInteger = 3;
                    serial.DiscardOutBuffer();
                    serial.DiscardInBuffer();
                    serial.WriteTimeout = 90000;
                    serial.Write(reSizePackResponse, 0, reSizePackResponse.Length);

                    Log.Information("Request sent. Waiting for terminal response.");

                    responseInteger = 4;
                    SetReadTimeout(respsend[1]);

                    serialReceive(serial.ReadTimeout, out terminalResponse);


                    Log.Information("Parsing the terminal response.");
                    byte[] parseTerminalResponse = new byte[50000];
                    parse(terminalResponse, parseTerminalResponse);

                    byte[] reSizeParseTerminalResponse = TrimTrailingBytes(parseTerminalResponse, 0);
                    Log.Information("Parsed data resized and processed.");

                    StringBuilder HexResponseTerminal = new StringBuilder(reSizeParseTerminalResponse.Length * 2);
                    foreach (byte b1 in reSizeParseTerminalResponse)
                        HexResponseTerminal.AppendFormat("{0:x2}", b1);

                    string[] sep = { "3b" };
                    resp1 = HexResponseTerminal.ToString().Split(sep, StringSplitOptions.None);
                    Log.Information("Response split and parsed. Checking transaction type.");

                    if (resp1[1] == "4332")
                    {
                        if (resp1.Length > 15)
                        {
                            responseTemp = new string[resp1.Length];
                            int j = 1;
                            for (int i = 5; i < resp1.Length - 3; i++)
                            {
                                responseTemp[j] = ConvertHex(resp1[i]);
                                j = j + 1;
                            }
                        }
                        else
                        {
                            responseTemp = new string[resp1.Length];
                            for (int i = 0; i < resp1.Length; i++)
                            {
                                responseTemp[i] = ConvertHex(resp1[i]);
                            }
                        }
                    }
                    else
                    {
                        responseTemp = new string[resp1.Length];
                        for (int i = 0; i < resp1.Length; i++)
                        {
                            responseTemp[i] = ConvertHex(resp1[i]);
                        }
                    }

                    string Type = CheckingTransactionType(responseTemp[1]);
                    responseTemp[1] = Type;
                    responseFinal = ConvertStringArrayToString(responseTemp);
                    Log.Information("Final response assembled and ready: {responseFinal}", responseFinal);

                    responseInteger = 0;
                }
            }
            catch (IOException io)
            {
                serial.Close();
                serial.Dispose();
                Log.Error("SocketException encountered: {Message}", io.ToString());
            }
            catch (UnauthorizedAccessException ex)
            {
                serial.Close();
                serial.Dispose();
            }
            catch (Exception e)
            {
                serial.Close();
                COMStatusConnection(Port, baudRateCom, parityCom, dataBitsCom, stopBitsCom);
                Log.Error("SocketException encountered: {Message}", e.ToString());
            }
            finally
            {
              
                
                transaflag = false;
                if (serial.IsOpen)
                {
                    serial.Close();
                    serial.Dispose();
                }
                    Log.Information("Closed and disposed the serial port.");
                    Log.Information("Exiting doCOMTransaction() method");
            }
            response = responseFinal;
            return responseInteger;
        }
       

        private void SetReadTimeout(string responseCode)
        {
            switch (responseCode)
            {
                case "4232":
                    serial.ReadTimeout = 90000;
                    break;
                case "4130":
                case "4236":
                case "4237":
                    serial.ReadTimeout = 10000;
                    break;
                case "4331":
                    serial.ReadTimeout = 180000;
                    break;
                case "4333":
                    serial.ReadTimeout = 5000;
                    break;
                default:
                    serial.ReadTimeout = 50000;
                    break;
            }
            Log.Information("Set read timeout based on response code {responseCode}: {Timeout}", responseCode, serial.ReadTimeout);
        }

        public int doTCPIPConnection(string IP, int PORT)
        {
            Log.Debug("Enter doTCPIPConnection() method");
            Log.Information("Enter doTCPIPConnection() method");
            int responseInteger = 1;
            IPAddress host = IPAddress.Parse(IP);
            IPEndPoint hostep = new IPEndPoint(host, PORT);
            try
            {
                sock.Close();
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(hostep);
                Log.Information("TCP/IP connection made successfully");
                responseInteger = 0;
            }
            catch (SocketException e)
            {
                Console.WriteLine("Problem connecting to host");
                Log.Information("Problem connecting to host please check wifi or network connection");
                Log.Error("Problem connecting to host please check wifi or network connection" + e.ToString());
                Console.WriteLine(e.ToString());
                responseInteger = 1;
                sock.Close();
            }

            return responseInteger;
        }
        public int doTCPIPConnectionTransaction(string IP, int PORT)
        {
            int responseInteger = 1;
            IPAddress host = IPAddress.Parse(IP);
            IPEndPoint hostep = new IPEndPoint(host, PORT);
            try
            {
                sock.Close();
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(hostep);
                responseInteger = 0;
            }
            catch (SocketException e)
            {
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(e.ToString());
                responseInteger = 1;
                sock.Close();
            }

            return responseInteger;
        }
        public bool doTCPIPHeartBeatCheck(string IP, int PORT)
        {
            Log.Information("Enter doTCPIPHeartBeatCheck() method");
            bool responseInteger = false;
            IPAddress host = IPAddress.Parse(IP);
            IPEndPoint hostep = new IPEndPoint(host, PORT);
            try
            {
                sock.Close();
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(hostep);
                responseInteger = true;
            }
            catch (SocketException e)
            {
                Console.WriteLine("Problem connecting to host");
                Log.Information("TCP/IP connection fail please check network connection/teminal and ecr application must connected with same network");
                Log.Error("Connectin failed" + e.ToString());
                Console.WriteLine(e.ToString());
                responseInteger = false;
                sock.Close();
            }
            finally
            {
                sock.Close();
            }

            return responseInteger;
        }
        public int doTCPIPTransaction(string IP, int PORT, string inputReqData, int transactionType, string szSignature, out string response)
        {
            Log.Debug("Entering doTCPIPTransaction() method");
            Log.Information("Starting TCP/IP transaction with IP: {IP}, Port: {PORT}, Transaction Type: {transactionType}", IP, PORT, transactionType);
            byte[] terminalResponse = new byte[50000];
            String[] resp1 = new String[500];
            String[] respsend = new String[500];
            string[] responseTemp = new string[1];
            string responseFinal = "";
            int responseInteger = 1;
            bool connStatus = false;
            int retryCount = 3;

            try
            {
                sock.Close();
                connStatus = doTCPIPHeartBeatCheck(IP, PORT);
                if (!connStatus)
                {

                    for (int i = 0; i < retryCount; i++)
                    {
                        Log.Information("Retry {retryAttempt}: Waiting for 20 seconds before reattempting", i + 1);
                        connStatus = doTCPIPHeartBeatCheck(IP, PORT);
                        if (connStatus == true)
                        {
                            Log.Information("Connection successful on retry {retryAttempt}", i + 1);
                            break;
                        }
                    }
                }
                if (connStatus)
                {
                    int resConnect = doTCPIPConnectionTransaction(IP, PORT);
                    if (resConnect == 0)
                    {
                        Log.Information("Transaction request: {inputReqData}", inputReqData);
                        byte[] packResponse = new byte[200];
                        //pack method---------------------------------------------------------
                        responseInteger = 2;
                        pack(inputReqData, transactionType, szSignature, packResponse);
                        Log.Debug("Packed request data: {inputReqData}, Transaction Type: {transactionType}, Signature: {szSignature}", inputReqData, transactionType, szSignature);
                        //resize byte array-----------------------------------------------
                        byte[] reSizePackResponse = TrimTrailingBytes(packResponse, 0);
                        Log.Information("Packed data resized and ready to send. Data size: {Size}", reSizePackResponse.Length);

                        int l = reSizePackResponse.Length;

                        StringBuilder Hex = new StringBuilder(reSizePackResponse.Length * 2);
                        foreach (byte b in reSizePackResponse)
                            Hex.AppendFormat("{0:x2}", b);
                        string[] sepSend = { "fc" };
                        respsend = Hex.ToString().Split(sepSend, StringSplitOptions.None);

                        //send to terminal------------------------------------------------
                        responseInteger = 3;
                        int bytesSent = sock.Send(reSizePackResponse);
                        Log.Information("Transaction request sent successfully to the terminal");

                        //recieave from terminal------------------------------------------
                        responseInteger = 4;
                       // sock.ReceiveTimeout = 90000;
                        socketReceive(out terminalResponse);

                        /*
                        if (respsend[1] == "4232")
                        {
                            //sock.ReceiveTimeout = 90000;
                            //socketReceive(90000, out terminalResponse);
                            socketReceive(out terminalResponse);

                        }
                        else if (respsend[1] == "4130" || respsend[1] == "4236" || respsend[1] == "4237")
                        {
                           // sock.ReceiveTimeout = 10000;
                            // socketReceive(10000, out terminalResponse);
                            socketReceive(out terminalResponse);

                        }
                        else if (respsend[1] == "4331")
                        {
                            //sock.ReceiveTimeout = 180000;
                            // socketReceive(180000, out terminalResponse);
                            socketReceive(out terminalResponse);
                        }
                        else if (respsend[1] == "4333")
                        {
                           // sock.ReceiveTimeout = 50000;
                            //socketReceive(5000, out terminalResponse);
                            socketReceive(out terminalResponse);
                        }
                        else
                        {
                            //sock.ReceiveTimeout = 90000;
                            socketReceive(out terminalResponse);
                        }*/
                        //parse method------------------------------
                        byte[] parseTerminalResponse = new byte[50000];
                        parse(terminalResponse, parseTerminalResponse);

                        //resize byte array-----------------------------------------------

                        byte[] reSizeParseTerminalResponse = TrimTrailingBytes(parseTerminalResponse, 0);

                        //byte dec to hex-------------------------------------------------
                        StringBuilder HexResponseTerminal = new StringBuilder(reSizeParseTerminalResponse.Length * 2);
                        foreach (byte b1 in reSizeParseTerminalResponse)
                            HexResponseTerminal.AppendFormat("{0:x2}", b1);

                        //parsing using split --------------------------------------------
                        string[] sep = { "3b" };
                        resp1 = HexResponseTerminal.ToString().Split(sep, StringSplitOptions.None);
                        if (resp1[1] == "4332")
                        {
                            if (resp1.Length > 15)
                            {
                                responseTemp = new string[resp1.Length];
                                int j = 1;
                                for (int i = 5; i < resp1.Length - 3; i++)
                                {
                                    responseTemp[j] = ConvertHex(resp1[i]);
                                    j = j + 1;
                                }
                            }
                            else
                            {
                                responseTemp = new string[resp1.Length];
                                for (int i = 0; i < resp1.Length; i++)
                                {
                                    responseTemp[i] = ConvertHex(resp1[i]);
                                }
                            }
                        }
                        else
                        {
                            responseTemp = new string[resp1.Length];
                            for (int i = 0; i < resp1.Length; i++)
                            {
                                responseTemp[i] = ConvertHex(resp1[i]);
                            }
                        }
                        string Type = CheckingTransactionType(responseTemp[1]);
                        responseTemp[1] = Type;
                        responseFinal = ConvertStringArrayToString(responseTemp);
                        responseInteger = 0;
                    }
                    else
                    {
                        Console.WriteLine("prblem on connection terminal");
                    }

                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
                Log.Error("Socket Exception occurred: {ErrorMessage}", e.Message);
            }
            finally
            {
                sock.Close();
                Log.Information("Exiting doTCPIPTransaction() method");

            }
            response = responseFinal;
            return responseInteger;
        }
        public int doTCPIPDisconnection()
        {
            int responseInteger = 1;
            try
            {
                sock.Close();
                responseInteger = 0;
            }
            catch (IOException e)
            {
                Console.WriteLine("Problem on disconnecting host");
                Console.WriteLine(e.ToString());

            }
            return responseInteger;
        }

        public int doCOMDisconnection()
        {
            int responseInteger = 1;
            try
            {
                serial.Close();
                responseInteger = 0;
            }
            catch (IOException e)
            {
                Console.WriteLine("Problem on disconnecting host");
                Console.WriteLine(e.ToString());

            }
            return responseInteger;
        }
        public byte[] TrimTrailingBytes(byte[] buffer, byte trimValue)
        {
            int i = buffer.Length;
            while (i > 0 && buffer[--i] == trimValue)
            {
                ; // no-op by design
            }
            byte[] resized = new byte[i + 1];
            Array.Copy(buffer, resized, resized.Length);
            return resized;
        }
        public static string ConvertHex(string hexString)
        {
            string ascii = string.Empty;

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string hs = string.Empty;
                hs = hexString.Substring(i, 2);
                ulong decval = Convert.ToUInt64(hs, 16);
                long deccc = Convert.ToInt64(hs, 16);
                char character = Convert.ToChar(deccc);
                ascii += character;
            }
            return ascii;
        }
        public string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public string MaskDigitsPan(String source)
        {
            StringBuilder sb = new StringBuilder(source);

            const int skipLeft = 6;
            const int skipRight = 4;

            int left = -1;

            for (int i = 0, c = 0; i < sb.Length; ++i)
            {
                if (Char.IsDigit(sb[i]))
                {
                    c += 1;

                    if (c > skipLeft)
                    {
                        left = i;

                        break;
                    }
                }
            }
            for (int i = sb.Length - 1, c = 0; i >= left; --i)
                if (Char.IsDigit(sb[i]))
                {
                    c += 1;

                    if (c > skipRight)
                        sb[i] = '*';
                }

            return sb.ToString();
        }
        public string ConvertStringArrayToString(string[] array)
        {
            // Concatenate all the elements into a StringBuilder.
            StringBuilder builder = new StringBuilder();
            foreach (string value in array)
            {
                builder.Append(value);
                builder.Append(';');

            }
            return builder.ToString();
        }
        public string CheckingTransactionType(string command)
        {
            string transactionType = "";
            switch (command)
            {
                case "A0":
                    transactionType = "17";
                    break;
                case "A1":
                    transactionType = "0";
                    break;
                case "A2":
                    transactionType = "1";
                    break;
                case "A3":
                    transactionType = "8";
                    break;
                case "A4":
                    transactionType = "3";
                    break;
                case "A5":
                    transactionType = "9";
                    break;
                case "A6":
                    transactionType = "2";
                    break;
                case "A7":
                    transactionType = "4";
                    break;
                case "A8":
                    transactionType = "5";
                    break;
                case "A9":
                    transactionType = "6";
                    break;
                case "B1":
                    transactionType = "10";
                    break;
                case "B2":
                    transactionType = "11";
                    break;
                case "B3":
                    transactionType = "12";
                    break;
                case "B4":
                    transactionType = "13";
                    break;
                case "B6":
                    transactionType = "18";
                    break;
                case "B7":
                    transactionType = "19";
                    break;
                case "B8":
                    transactionType = "20";
                    break;
                case "B9":
                    transactionType = "21";
                    break;
                case "C1":
                    transactionType = "22";
                    break;
                case "C2":
                    transactionType = "23";
                    break;
                case "C3":
                    transactionType = "24";
                    break;
                default:
                    Console.WriteLine("Invalid transactionType");
                    transactionType = "50";
                    break;
            }
            return transactionType;
        }
        public void socketReceive(out byte[] terminalResponse)
        {
            Log.Debug("Starting socketReceive method.");
            int received = 0;  // how many bytes are already received
            byte[] terminalResponse1 = new byte[50000];
            byte[] reSizeTerminalResponse = new byte[1];
            

            do
            {
                try
                {
                    Log.Debug("Attempting to receive data from socket.");
                    sock.ReceiveTimeout = 90000;
                    received = sock.Receive(terminalResponse1);
                    reSizeTerminalResponse = TrimTrailingBytes(terminalResponse1, 0);
                    Log.Information("Response received from terminal: {terminalResponse}", BitConverter.ToString(terminalResponse1));

                    // Convert byte array to string for logging
                    string receivedDataString = Encoding.UTF8.GetString(reSizeTerminalResponse);
                    Log.Information("Received byte data: {ReceivedData}", receivedDataString);
                }
                catch (SocketException ex)
                {
                    Log.Error("SocketException occurred: {Message}", ex.Message);
                    int resDisConnect = doTCPIPDisconnection();
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably empty, wait and try again
                        Log.Warning("Socket buffer empty, retrying...");
                        Thread.Sleep(30);
                    }
                    else
                    {
                        Log.Error("Serious socket error occurred: {ErrorCode}, {ErrorMessage}", ex.SocketErrorCode, ex.Message);
                        throw;  // any serious error occurred, rethrow the exception
                    }
                }
            } while (received < reSizeTerminalResponse.Length);

            Log.Information("Successfully received data from socket.");
            terminalResponse = reSizeTerminalResponse;
        }

        public void serialReceive(int timeout, out byte[] terminalResponse)
        {
            Log.Debug("Starting serialReceive method with timeout: {Timeout}", timeout);
            //serial.ReadTimeout = 90000;
            int received = 0;  // how many bytes is already received
            byte[] terminalResponse1 = new byte[50000];
            byte[] reSizeTerminalResponse = new byte[5];

            do
            {
                try
                {
                    Log.Debug("Attempting to read data from serial port.");
                    received = serial.Read(terminalResponse1, 0, terminalResponse1.Length);
                    Thread.Sleep(1000);

                    // Trim the received byte array
                    reSizeTerminalResponse = TrimTrailingBytes(terminalResponse1, 0);
                    Log.Information("Response received from terminal: {terminalResponse}", BitConverter.ToString(terminalResponse1));

                    // Convert byte array to string for logging
                    string receivedDataString = Encoding.UTF8.GetString(reSizeTerminalResponse);
                    Log.Information("Received byte data from serial: {ReceivedData}", receivedDataString);
                }
                catch (SocketException ex)
                {
                    Log.Error("SocketException occurred: {Message}", ex.Message);
                    int resDisConnect = doCOMDisconnection();
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        Log.Warning("Socket buffer empty, retrying...");
                        Thread.Sleep(30);
                    }
                    else
                    {
                        Log.Error("Serious socket error occurred: {ErrorCode}, {ErrorMessage}", ex.SocketErrorCode, ex.Message);
                        throw;  // any serious error occurred, rethrow the exception
                    }
                }
                catch (IOException ioEx)
                {
                    Log.Error("IOException occurred: {Message}", ioEx.Message);
                    Log.Information("Closing and disposing the serial connection.");
                    serial.Close();
                    serial.Dispose();
                }
            } while (received < 5);

            Log.Information("Successfully received data from serial.");
            terminalResponse = reSizeTerminalResponse;
        }

    }

}