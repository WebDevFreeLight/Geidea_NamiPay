using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.Windows.Media;
using System.Diagnostics;
using System.Timers;
using Timer = System.Threading.Timer;
using System.Collections;
using CoreSkybandECR;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Serilog;

namespace SkyBandECRNew
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string TerminalId = "";
        int status = 0;
        string cashRegister = "";
        string transaction = "";

        string prvsRCRNumber;
        public MainWindow()
        {
            InitializeComponent();
            InitiateTimer();
            loadUserSettings();
            InitializeRetentionData();
            InitiateTimer();
            createLogFile();;
            Log.Information("-:Application Start:-");


        }
        static string textFocus = string.Empty;
        static string hostIp = string.Empty;
        static string port;
        static string PortCom;
        string previousDatetime = string.Empty;
        static string baudRateCom = string.Empty;
        static string parityCom = string.Empty;
        static int dataBitsCom;
        static int stopBitsCom;
        static int result = 1;
        static int resultDisconnect;
        static string tcpComSelect = string.Empty;
        static string inputReqData = "";
        static string transactionType = "";
        static string etpInput = "";
        static string pfcInput = "";
        static string ecrReferenceNo = "";
        static string szSignature = "";
        string originalTransactionDate = "";
        static string originalTransactionDateRefund = "";
        static string name;
        static bool printCheck;
        static bool fpCapture;
        static bool ECRrefNum;
        static bool pfcCheck;
        static string transTypeSelected;
        static string AMOUNT;
        static string ECRREFtext;
        static string CASHBACK;
        static string RRN;
        static string OAC;
        static string CashRegisterNumber;
        static string VendorID;
        static string VendorTerminaltype;
        static string TRSMID;
        static string VendorKeyIndex;
        static string SAMAKeyIndex;
        static string BillerID;
        static string BillNumber;
        static string prvsECRNumber;
        static string BUFFERSENDtext;
        static string BUFFERRECEIVEDtext;
        static string msgShow;
        static string ECRREferenceNumber;
        static string htmlString = "";
        string sendData;
        static int countAmount = 0;
        static int countCB = 0;
        string filetoDelete = string.Empty;
        string expirationDate = string.Empty;
        string currentDay = string.Empty;
        string expiryValue = string.Empty;
        string loglevel = string.Empty;
        bool islogAllowed;
        int noOfDayValue = 0;
        string filepath = string.Empty;

        BackgroundWorker worker;
        BackgroundWorker workerSend;

        private void TCPCOMOnChange(object sender, RoutedEventArgs e)
        {
            List<string> portList = obj.GetQualcommPorts();
            PORTCOM.ItemsSource = portList;

            if (rdbCom.IsChecked == true)
            {
                tcpComSelect = "TCP/IP";
                grpTcpSetting.Visibility = Visibility.Hidden;
                grpComSetting.Visibility = Visibility.Visible;
            }
            if (rdbTcp.IsChecked == true)
            {
                tcpComSelect = "COM";
                grpComSetting.Visibility = Visibility.Hidden;
                grpTcpSetting.Visibility = Visibility.Visible;
                grpTcpSetting.IsEnabled = true;
            }

            if (tcpComSelect.ToString() == "TCP/IP")
            {
                PORTCOM.IsEnabled = true;
                txtIpAddress.IsEnabled = false;
                txtPort.IsEnabled = false;
                BAUDSPEED.IsEnabled = true;
                PARITY.IsEnabled = true;
                DATABITS.IsEnabled = true;
                STOPBITS.IsEnabled = true;
                txtIpAddress.Text = "";
                txtPort.Text = "";
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            else
            {
                txtPort.IsEnabled = true;
                txtIpAddress.IsEnabled = true;
                BAUDSPEED.IsEnabled = false;
                PARITY.IsEnabled = false;
                DATABITS.IsEnabled = false;
                STOPBITS.IsEnabled = false;
                txtPort.Text = "";
                BAUDSPEED.SelectedIndex = 0;
                PARITY.SelectedIndex = 0;
                DATABITS.SelectedIndex = 0;
                STOPBITS.SelectedIndex = 0;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
        }
        InteroperableService obj = new InteroperableService();
        public Timer connectionCheckTimer;

        public int doTransFlag = 0;
        //public void TerminalConnectionCheckerW()
        //{
        //    if (doTransFlag == 0)
        //    {
        //        connectionCheckTimer = new Timer(ConnectionCheckTimer_Tick, null, TimeSpan.FromSeconds(0),
        //        TimeSpan.FromSeconds(15));
        //    }

        //}
        //private void ConnectionCheckTimer_Tick(object state)
        //{
        //    if (doTransFlag == 0)
        //    {
        //        isAppIdle();
        //    }
        //}
        //public void isAppIdle()
        //{
        //    if (port != null && hostIp != "")
        //    {
        //        bool value = obj.doTCPIPHeartBeatCheck(hostIp, int.Parse(port));
        //        if (value)
        //        {
        //            Dispatcher.Invoke(() =>
        //            {
        //                Color color = Color.FromArgb(0xFF, 0x15, 0x82, 0x2B); // ARGB values for the color
        //                SolidColorBrush brush = new SolidColorBrush(color);
        //                txtIpAddress.Background = brush;
        //                txtIpAddress.IsEnabled = false;
        //                txtPort.IsEnabled = false;
        //                btnConnect.IsEnabled = false;
        //                ConnectL.Content = "Connected";
        //            });
        //        }
        //        else
        //        {
        //            Dispatcher.Invoke(() =>
        //            {
        //                Color color = Color.FromArgb(0xFF, 0xCA, 0x25, 0x0A); // ARGB values for the color
        //                SolidColorBrush brush = new SolidColorBrush(color);
        //                txtIpAddress.Background = brush;
        //                txtIpAddress.IsEnabled = true;
        //                txtPort.IsEnabled = true;
        //                btnConnect.IsEnabled = true;
        //                ConnectL.Content = "Not Connected";


        //            });

        //        }
        //    }

        //}
        static bool statusSerialConn = false;
        DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        bool TCPIPConnection = false;
        private void Connect(object sender, RoutedEventArgs e)
        {
            if (tcpComSelect.ToString() == "TCP/IP")
            {
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
                dispatcherTimer.Start();
            }
            btnConnect.IsEnabled = false;
            hostIp = txtIpAddress.Text;
            port = txtPort.Text;
            PortCom = GetComPortNumber(PORTCOM.Text);
            baudRateCom = BAUDSPEED.Text;
            parityCom = PARITY.Text;
            dataBitsCom = int.Parse(DATABITS.Text);
            stopBitsCom = int.Parse(STOPBITS.Text);
            fullScreen.IsEnabled = false;
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();
        }


    public static string GetComPortNumber(string input)
    {
        var match = Regex.Match(input, @"COM(\d+)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private void dispatcherTimer_Tick(object sender, EventArgs e)
        {

            if (statusSerialConn)
            {
                //bool status = obj.doCOMConnection(int.Parse(PortCom), baudRateCom, parityCom, dataBitsCom, stopBitsCom);
                //if (!status)
                //{
                //    MessageBox.Show("Serial Cable Disconncected, Please connect serial cable");
                //    statusSerialConn = false;
                //}

                int status = obj.doCOMConnection(int.Parse(PortCom), baudRateCom, parityCom, dataBitsCom, stopBitsCom);
                if (status == 1)
                {
                    MessageBox.Show("Serial Cable Disconncected, Please connect serial cable");
                    statusSerialConn = false;
                }

            }
            else if (!statusSerialConn)
            {
                var result = obj.doCOMConnection(int.Parse(PortCom), baudRateCom, parityCom, dataBitsCom, stopBitsCom);
                // bool status = obj.doCOMStatusConnection(int.Parse(PortCom), baudRateCom, parityCom, dataBitsCom, stopBitsCom);
                if (result == 0)
                {
                    MessageBox.Show("Serial Cable Connected.");
                    statusSerialConn = true;
                }
            }
        }
        private void loadUserSettings()
        {
            if (SkybandECR.Properties.Settings.Default.LogOption == "True")
            {
                isEnabledlog.IsChecked = true;
                islogAllowed = true;
            }
            else
            {
                isEnabledlog.IsChecked = false;
            }

            if (string.IsNullOrEmpty(SkybandECR.Properties.Settings.Default.Path))
            {
                Filepath.Text ="C:\\ECRSkyband";
            }
            else
            {
                Filepath.Text = SkybandECR.Properties.Settings.Default.Path;
            }

            if (string.IsNullOrEmpty(SkybandECR.Properties.Settings.Default.RetainDays))
            {
                NoDay.Text = "2";
            }
            else
            {
                NoDay.Text = SkybandECR.Properties.Settings.Default.RetainDays;
            }

            if (string.IsNullOrEmpty(SkybandECR.Properties.Settings.Default.LogLevel))
            {
                LogLevel.Text = "error";
            }
            else
            {
                LogLevel.Text = SkybandECR.Properties.Settings.Default.LogLevel;
            }
        }
     
        private void InitializeRetentionData()
        {
            // Initialize filetoDelete and retentionDate based on current date and NoDay value
            SkybandECR.Properties.Settings.Default.filetoDelete = "NamiPayECRNet_" + DateTime.Now.Date.ToString("yyyy-MM-dd") + ".log";
            SkybandECR.Properties.Settings.Default.retentionDate = DateTime.Now.AddDays(double.Parse(NoDay.Text) - 1).ToString("yyyy-MM-dd");
            SkybandECR.Properties.Settings.Default.Save();
        }

        private void InitiateTimer()
        {
            // Create and start a timer with a 6-second interval
            System.Timers.Timer newTimer = new System.Timers.Timer();
            newTimer.Elapsed += new ElapsedEventHandler(deleteFileifNeeded);
            newTimer.Interval = 6000; // 6 seconds
            newTimer.Start();
        }

        private void deleteFileifNeeded(object source, ElapsedEventArgs e)
        {
            // Use the Dispatcher to safely update UI elements or properties
            Application.Current.Dispatcher.Invoke(() =>
            {
                filepath = Filepath.Text;
                filetoDelete = SkybandECR.Properties.Settings.Default.filetoDelete;
                expirationDate = SkybandECR.Properties.Settings.Default.retentionDate;

                // If filetoDelete is empty, initialize it
                if (string.IsNullOrEmpty(filetoDelete))
                {
                    SkybandECR.Properties.Settings.Default.filetoDelete = "NamiPayECRNet_" + DateTime.Now.Date.ToString("yyyy-MM-dd") + ".log";
                }

                // If expirationDate is empty, calculate and initialize it
                if (string.IsNullOrEmpty(expirationDate))
                {
                    expirationDate = DateTime.Now.AddDays(double.Parse(NoDay.Text) - 1).ToString("yyyy-MM-dd");
                    SkybandECR.Properties.Settings.Default.retentionDate = expirationDate;
                }

                // Perform file deletion based on conditions
                if (!string.IsNullOrEmpty(filetoDelete) && LogFile.deleteFileifNeeded(filepath, filetoDelete, expirationDate))
                {
                    // Handle file deletion and rotation
                    if (SkybandECR.Properties.Settings.Default.RetainDays == "1")
                    {
                        SkybandECR.Properties.Settings.Default.filetoDelete = "NamiPayECRNet_" + (DateTime.Parse(expirationDate).AddDays(1)).ToString("yyyy-MM-dd") + ".log";
                    }
                    else if (!string.IsNullOrEmpty(expiryValue) && DateTime.Parse(expiryValue) == DateTime.Parse(expirationDate))
                    {
                        SkybandECR.Properties.Settings.Default.filetoDelete = "NamiPayECRNet_" + DateTime.Parse(expirationDate).ToString("yyyy-MM-dd") + ".log";
                    }
                    else
                    {
                        expiryValue = expirationDate;
                        SkybandECR.Properties.Settings.Default.filetoDelete = "NamiPayECRNet_" + DateTime.Parse(expirationDate).AddDays(-1).ToString("yyyy-MM-dd") + ".log";
                    }

                    // Update the retention date for the next log file
                    SkybandECR.Properties.Settings.Default.retentionDate = DateTime.Parse(expirationDate).AddDays(1).ToString("yyyy-MM-dd");
                    SkybandECR.Properties.Settings.Default.Save();

                    // If the next log file doesn't exist, ensure it is created
                    if (!File.Exists(Path.Combine(filepath, SkybandECR.Properties.Settings.Default.filetoDelete)))
                    {
                        SkybandECR.Properties.Settings.Default.filetoDelete = "NamiPayECRNet_" + DateTime.Parse(expirationDate).ToString("yyyy-MM-dd") + ".log";
                    }

                    // Create a new log file
                    createLogFile();
                }

                // Handle daily file creation if retention is set to 1 day
                if (SkybandECR.Properties.Settings.Default.RetainDays == "1" && !File.Exists(Path.Combine(filepath, SkybandECR.Properties.Settings.Default.filetoDelete)))
                {
                    SkybandECR.Properties.Settings.Default.filetoDelete = "NamiPayECRNet_" + DateTime.Parse(expirationDate).AddDays(1).ToString("yyyy-MM-dd") + ".log";
                }

                // Create a new log file if the previous day has passed
                if (!string.IsNullOrEmpty(previousDatetime) && DateTime.Parse(previousDatetime) < DateTime.Now.Date)
                {
                    createLogFile();
                }

                // Update the previous date and save settings
                previousDatetime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                currentDay = DateTime.Now.Date.ToString();
                SkybandECR.Properties.Settings.Default.Save();
            });
        }

        public void createLogFile()
        {
            string loglevelval = SkybandECR.Properties.Settings.Default.LogLevel;
            int noOfDayValue = 0;

            switch (loglevelval.ToLower()) // Convert input to lowercase for case-insensitivity
            {
                case "error":
                    loglevel = "1";
                    break;
                case "debug":
                    loglevel = "4";
                    break;
                case "information":
                    loglevel = "3";
                    break;
                default:
                    loglevel = "1";
                    break;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                noOfDayValue = int.Parse(NoDay.Text);
                filepath = Filepath.Text;
            });

            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }
            if (islogAllowed)
            {
                LogFile.SetLogOptions(int.Parse(loglevel), true, filepath, noOfDayValue);
            }
        }

        // A placeholder for the method that creates the log file


        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool checkNullValidation = true;
            (sender as BackgroundWorker).ReportProgress(0);
            try
            {
                InteroperableService obj = new InteroperableService();
                if (tcpComSelect.ToString() == "TCP/IP")
                {
                    if (PortCom.ToString() == string.Empty || baudRateCom.ToString() == string.Empty || parityCom.ToString() == string.Empty || dataBitsCom.ToString() == string.Empty || stopBitsCom.ToString() == string.Empty)
                    {
                        checkNullValidation = false;
                        (sender as BackgroundWorker).ReportProgress(3);
                    }
                    else
                    {
                        result = obj.doCOMConnection(int.Parse(PortCom), baudRateCom, parityCom, dataBitsCom, stopBitsCom);
                        if (result.ToString() == "0")
                        {
                            statusSerialConn = true;
                        }
                    }
                }
                else
                {
                    var parts = hostIp.Split('.');
                    bool isValid = parts.Length == 4
                                   && !parts.Any(
                                       x =>
                                       {
                                           int y;
                                           return Int32.TryParse(x, out y) && y > 255 || y < 0;
                                       });
                    if (hostIp.ToString() == string.Empty)
                    {
                        checkNullValidation = false;
                        (sender as BackgroundWorker).ReportProgress(2);

                    }
                    else if (port.ToString() == string.Empty)
                    {
                        checkNullValidation = false;
                        (sender as BackgroundWorker).ReportProgress(3);
                    }
                    else if (isValid != true)
                    {
                        checkNullValidation = false;
                        (sender as BackgroundWorker).ReportProgress(5);
                    }
                    else
                    {
                        result = obj.doTCPIPConnection(hostIp, int.Parse(port));
                        if (result == 0)
                        {
                            TCPIPConnection = true;
                        }
                    }

                }
                if (checkNullValidation)
                {
                    if (result == 0)
                    {
                        (sender as BackgroundWorker).ReportProgress(1);
                    }
                    else
                    {
                        (sender as BackgroundWorker).ReportProgress(4);
                    }

                }
            }
            catch (Exception ex)
            {
                (sender as BackgroundWorker).ReportProgress(1);
                MessageBox.Show(ex.Message);
            }
        }
        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage.ToString() == "0")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Visible;
                ProgBar.Visibility = Visibility.Visible;
                ProgBar.IsIndeterminate = true;
            }
            if (e.ProgressPercentage.ToString() == "2")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                MessageBox.Show("IP Address Or PORT Should not be empty");
                btnConnect.IsEnabled = true;
            }
            if (e.ProgressPercentage.ToString() == "3")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                MessageBox.Show("PORT Should not be empty");
                btnConnect.IsEnabled = true;
            }
            if (e.ProgressPercentage.ToString() == "4")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                MessageBox.Show("Problem connecting to Terminal");
                btnConnect.IsEnabled = true;
            }
            if (e.ProgressPercentage.ToString() == "5")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                MessageBox.Show("Ip address not correct Format");
                btnConnect.IsEnabled = true;
            }
            if (e.ProgressPercentage.ToString() == "1")
            {
                ProgBar.IsIndeterminate = false;
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                if (result == 0)
                {
                    BitmapImage bmi = new BitmapImage();
                    bmi.BeginInit();
                    bmi.UriSource = new Uri("img\\connectedIcon.png", UriKind.Relative);
                    bmi.EndInit();
                    ConnectImg.Stretch = Stretch.Fill;
                    ConnectImg.Source = bmi;
                    ConnectL.Content = "Connected";
                    rdbCom.IsEnabled = false;
                    rdbTcp.IsEnabled = false;
                    CashRegisterNumberL.IsEnabled = false;
                    CashRegisterNumberT.IsEnabled = false;
                    btnConnect.IsEnabled = false;
                    btnDisConnect.IsEnabled = true;
                    enter.IsEnabled = true;
                    TRANSTYPE.IsEnabled = true;
                    TRANSTYPEL.IsEnabled = true;
                    AdminTransaction.IsEnabled = true;
                    AdminTransactionL.IsEnabled = true;
                    txtPort.IsEnabled = false;
                    PORTCOM.IsEnabled = false;
                    txtIpAddress.IsEnabled = false;
                    BAUDSPEED.IsEnabled = false;
                    PARITY.IsEnabled = false;
                    DATABITS.IsEnabled = false;
                    STOPBITS.IsEnabled = false;
                    int number = 1;
                    txtEcrref.Text = number.ToString("D6");
                    CashRegisterNumberL.Focus();
                }
                else
                {
                    btnConnect.IsEnabled = true;
                }
            }
        }

        bool flagstatus = false;
        private void Enter(object sender, RoutedEventArgs e)
        {

            //int checkstatus = 0;
            //int retrycount = 3;

            //if (rdbTcp.IsChecked.Value)
            //{
            //    if (!TCPIPConnection)
            //    {
            //        checkstatus = obj.doTCPIPConnection(hostIp, int.Parse(port));
            //    }

            //    flagstatus = true;
            //    if (checkstatus == 1)
            //    {
            //        for (int i = 0; i < retrycount; i++)
            //        {
            //            checkstatus = obj.doTCPIPConnection(hostIp, int.Parse(port));
            //            if (checkstatus == 0)
            //            {
            //                flagstatus = true;
            //                break;
            //            }
            //        }
            //    }
            //}

            BUFFERSEND.Text = "";
            BUFFERRECEIVED.Text = "";
            myBrowser.Navigate((Uri)null);
            AMOUNT = AMOUNTT.Text.ToString();
            CASHBACK = CASHBACKT.Text.ToString();
            string validateMsg = "";
            bool flag = false;
            bool flagDate = true;
            string firstDigitAmount = string.Empty;
            string firstDigitCashback = string.Empty;
            var date = string.Empty;
            if (AMOUNT.Length == 0)
            {
                flag = true;
            }
            else if (Convert.ToDecimal(AMOUNT) <= 0)
            {
                firstDigitCashback = "0";
                validateMsg = "Amount";
            }
            if (CASHBACK.Length == 0)
            {
                flag = true;
            }
            else if (Convert.ToDecimal(CASHBACK) <= 0)
            {
                firstDigitCashback = "0";
                if (firstDigitCashback.ToString() == "0")
                {
                    validateMsg = "Cashback";
                }
            }
            if (DateT.Text != "")
            {
                date = DateT.SelectedDate.Value.ToString("dd MM yy");
                originalTransactionDate = date.Replace(" ", "");
                string a = DateTime.Parse(DateT.Text).ToString();
                string b = DateTime.Now.ToString();
                if (DateTime.Parse(a) > DateTime.Parse(b) && TRANSTYPE.Text.ToString() == "Refund")
                {
                    flagDate = false;
                }
            }
            if (TRANSTYPE.Text.ToString() != "" || AdminTransaction.Text.ToString() != "")
            {
                if (TRANSTYPE.Text.ToString() != "")
                {
                    transTypeSelected = TRANSTYPE.Text.ToString();
                }
                else
                {
                    transTypeSelected = AdminTransaction.Text.ToString();
                }
                if (flagDate)
                {
                    if (txtEcrref.Text.ToString() == "000000")
                    {
                        MessageBox.Show("Please Enter the valid ECR Reference Number");
                    }
                    else
                    {
                        if (firstDigitAmount.ToString() != "0" && firstDigitCashback.ToString() != "0")
                        {
                            ComboBoxItem ComboItem = (ComboBoxItem)TRANSTYPE.SelectedItem;
                            name = ComboItem.Name;
                            printCheck = chkEtp.IsChecked ?? false;
                            fpCapture = chkfpCapture.IsChecked ?? false;

                            // STEP 1: CashRegister.xml dependency removed
                            // CashRegisterNumber will be handled via UI input in Step 2
                            if (transTypeSelected == "Register")
                            {
                                string enteredCashRegister = CashRegisterNumberT.Text?.Trim();

                                if (string.IsNullOrEmpty(enteredCashRegister))
                                {
                                    MessageBox.Show("Please enter Cash Register Number");
                                    return;
                                }

                                if (enteredCashRegister.Length != 8 || !enteredCashRegister.All(char.IsDigit))
                                {
                                    MessageBox.Show("Cash Register Number must be exactly 8 digits");
                                    return;
                                }

                                cashRegister = enteredCashRegister;
                                CashRegisterNumber = enteredCashRegister;
                            }

                            if (AMOUNT.Length != 0)
                            {
                                int amt = (int)(Convert.ToDecimal(AMOUNT) * 100);
                                AMOUNT = amt.ToString();
                            }
                            if (CASHBACK.Length != 0)
                            {
                                int cashAmt = (int)(Convert.ToDecimal(CASHBACK) * 100);
                                CASHBACK = cashAmt.ToString();
                            }

                            // NOTE: CashRegisterNumber intentionally not assigned in Step 1

                            ECRREFtext = txtEcrref.Text.ToString();
                            RRN = RRNT.Text.ToString();
                            OAC = OACT.Text.ToString();
                            BUFFERSENDtext = BUFFERSEND.Text.ToString();
                            BUFFERRECEIVEDtext = BUFFERRECEIVED.Text.ToString();
                            VendorID = VendorIDT.Text.ToString();
                            TRSMID = TRSMIDT.Text.ToString();
                            VendorTerminaltype = VendorTerminaltypeT.Text.ToString();
                            VendorKeyIndex = VendorKeyIndexT.Text.ToString();
                            SAMAKeyIndex = SAMAKeyIndexT.Text.ToString();
                            BillerID = BillerIDT.Text.ToString();
                            BillNumber = BillNumberT.Text.ToString();
                            prvsECRNumber = prvsECRNumberT.Text.ToString();
                            ECRrefNum = chkEcrref.IsChecked ?? false;

                            enter.IsEnabled = false;
                            fullScreen.IsEnabled = false;

                            workerSend = new BackgroundWorker();
                            workerSend.WorkerReportsProgress = true;
                            workerSend.DoWork += worker_DoWorkSend;
                            workerSend.ProgressChanged += worker_ProgressSend;
                            workerSend.RunWorkerAsync();
                        }
                        else
                        {
                            MessageBox.Show("" + validateMsg + " should not start with zero");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Date should not greater than today's date");
                }
            }
            else
            {
                MessageBox.Show("Please Select one Transaction");
            }
        }
        void worker_DoWorkSend(object sender, DoWorkEventArgs e)
        {
            (sender as BackgroundWorker).ReportProgress(0);
            int responseInt = 1;
            DateTime now = DateTime.Now;

            string dateTime = now.ToString("dd MM yy HH:mm:ss");
            dateTime = dateTime.Replace(" ", "").Replace(":", "");
            string year = now.ToString("dd MM yyyy");
            year = year.Replace(" ", "");
            year = year.Substring(4, 4);
            bool checkNullValidation = true;
            // 
            if (printCheck)
            {
                etpInput = "1";
            }
            else etpInput = "0";
            if (fpCapture)
            {
                pfcInput = "1";
            }
            else pfcInput = "0";
            Console.WriteLine("partial full amount " + pfcInput);
            //string pfcInput = "1";
            ECRREferenceNumber = String.Concat(CashRegisterNumber, ECRREFtext);
            //register-----------------------------------------------------------------
            if (transTypeSelected.ToString() == "Register")
            {
                if (CashRegisterNumber == string.Empty)
                {
                    checkNullValidation = false;
                    messageShow("Cash register number Should not empty");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
                else if (CashRegisterNumber.Length != 8)
                {
                    checkNullValidation = false;
                    messageShow("Cash register number Should be 8 digits");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
                else
                {
                    transactionType = "17";
                    inputReqData = dateTime + ";" + CashRegisterNumber;
                }
            }
            //Start Session---------------------------------------------------------------
            if (transTypeSelected.ToString() == "Start Session")
            {
                if (TerminalId != "")
                {
                    if (CashRegisterNumber == string.Empty)
                    {
                        checkNullValidation = false;
                        messageShow("Cash register number Should not empty");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                    else if (CashRegisterNumber.Length != 8)
                    {
                        checkNullValidation = false;
                        messageShow("Cash register number Should be 8 digits");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                    else
                    {
                        transactionType = "18";
                        inputReqData = dateTime + ";" + CashRegisterNumber;
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            //End Session---------------------------------------------------------------
            if (transTypeSelected.ToString() == "End Session")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (CashRegisterNumber == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Cash register number Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (CashRegisterNumber.Length != 8)
                        {
                            checkNullValidation = false;
                            messageShow("Cash register number Should be 8 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "19";
                            inputReqData = dateTime + ";" + CashRegisterNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }

                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            // purchase-------------------------------------------------------------------
            if (transTypeSelected.ToString() == "Purchase")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT.ToString() == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "0";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            // purchase cashback-----------------------------------------------------
            if (transTypeSelected.ToString() == "Purchase Cashback")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (CASHBACK == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Cashback Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (Convert.ToDecimal(CASHBACK) >= Convert.ToDecimal(AMOUNT))
                        {
                            checkNullValidation = false;
                            messageShow("Cashback Amount should be lesser than Purchase Amount");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "1";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + CASHBACK + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            //refund---------------------------------------------------------------------------------
            if (transTypeSelected.ToString() == "Refund")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("RRN Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (originalTransactionDate.ToString() == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Date Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN.Length != 12)
                        {
                            checkNullValidation = false;
                            messageShow("RRN length should be 12 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "2";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + RRN + ";" + etpInput + ";" + originalTransactionDate + ";" + ECRREferenceNumber;
                            originalTransactionDate = "";
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Pre Authorization")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "3";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Pre Auth Extension")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (RRN == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("RRN Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (originalTransactionDate.ToString() == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("original Transaction Date Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Original aproval code Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN.Length != 12)
                        {
                            checkNullValidation = false;
                            messageShow("RRN length should be 12 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code length should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "5";
                            inputReqData = dateTime + ";" + RRN + ";" + originalTransactionDate + ";" + OAC + ";" + etpInput + ";" + ECRREferenceNumber;
                            originalTransactionDate = "";
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Pre Auth Void")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (originalTransactionDate.ToString() == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("original Transaction Date Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("RRN Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN.Length != 12)
                        {
                            checkNullValidation = false;
                            messageShow("RRN length should be 12 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code length should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "6";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + RRN + ";" + originalTransactionDate + ";" + OAC + ";" + etpInput + ";" + ECRREferenceNumber;
                            originalTransactionDate = "";
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Purchase Advice")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("RRN Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (originalTransactionDate.ToString() == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("original Transaction Date Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN.Length != 12)
                        {
                            checkNullValidation = false;
                            messageShow("RRN length should be 12 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code length should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "4";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + RRN + ";" + originalTransactionDate + ";" + OAC + ";" + pfcInput + ";" + etpInput + ";" + ECRREferenceNumber;
                            originalTransactionDate = "";
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Cash Advance")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "8";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Reversal")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (RRN == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("RRN Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN.Length != 12)
                        {
                            checkNullValidation = false;
                            messageShow("RRN length should be 12 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "9";
                            inputReqData = dateTime + ";" + RRN + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Reconciliation")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "10";
                            inputReqData = dateTime + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Parameter Download")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "11";
                            inputReqData = dateTime + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Set Settings")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (VendorID == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("VendorID Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (VendorTerminaltype == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Vendor Terminal type  Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (TRSMID == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("TRSMID Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (VendorKeyIndex == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Vendor Key Index Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (SAMAKeyIndex == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("SAMA Key Index Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (VendorID.Length.ToString() != "2")
                        {
                            checkNullValidation = false;
                            messageShow("Vendor ID should accept only 2 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (VendorTerminaltype.Length.ToString() != "2")
                        {
                            checkNullValidation = false;
                            messageShow("Vendor Terminal type should accept only 2 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (VendorKeyIndex.Length.ToString() != "2")
                        {
                            checkNullValidation = false;
                            messageShow("Vendor Key Index should accept only 2 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (SAMAKeyIndex.Length.ToString() != "2")
                        {
                            checkNullValidation = false;
                            messageShow("SAMA Key Index should accept only 2 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (TRSMID.Length.ToString() != "6")
                        {
                            checkNullValidation = false;
                            messageShow("TRSM ID should accept only 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "12";
                            inputReqData = dateTime + ";" + VendorID + ";" + VendorTerminaltype + " ;" + TRSMID + " ;" + VendorKeyIndex + ";" + SAMAKeyIndex + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Get Settings")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "13";
                            inputReqData = dateTime + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Bill Payment")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("BillPaymentAmount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (BillerID == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("BillerID Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (BillNumber == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("BillNumber Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (BillerID.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("BillerID Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (BillNumber.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("BillNumber Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "20";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + BillerID + ";" + BillNumber + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Running Total")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "21";
                            inputReqData = dateTime + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Print Summary Report")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "22";
                            inputReqData = dateTime + ";" + 0 + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }

            }
            if (transTypeSelected.ToString() == "Repeat")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (prvsECRNumber == "000000")
                        {
                            checkNullValidation = false;
                            messageShow("previous ecr reference number not valid");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (prvsECRNumber == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("previous ECR Number Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (prvsECRNumber.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("previous ECR Number Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "23";
                            inputReqData = dateTime + ";" + prvsECRNumber + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Check Status")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "24";
                            inputReqData = dateTime + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }

            }
            ecrReferenceNo = ECRREFtext;
            if (TerminalId != "")
            {
                szSignature = String.Concat(ecrReferenceNo, TerminalId);
            }
            string response = "ERROR";
            if (checkNullValidation)
            {
                try
                {
                    if (inputReqData != "")
                    {
                        inputReqData = inputReqData + "!";
                        BUFFERSENDtext = inputReqData;
                        (sender as BackgroundWorker).ReportProgress(3);
                    }
                    InteroperableService obj = new InteroperableService();
                    szSignature = obj.ComputeSha256Hash(szSignature);
                    //counting ecr teferebce number-----------------
                    if (transactionType != "17" && transactionType != "18" && transactionType != "19")
                    {
                        int refNumber = int.Parse(ECRREFtext) + 1;
                        ECRREFtext = refNumber.ToString("D6");
                        (sender as BackgroundWorker).ReportProgress(4);
                    }
                    string res = "";

                    if (tcpComSelect.ToString() == "TCP/IP")
                    {
                        responseInt = obj.doCOMTransaction(int.Parse(PortCom), baudRateCom, parityCom, dataBitsCom, stopBitsCom, inputReqData, int.Parse(transactionType), szSignature, out res);
                        string responseSummary;
                        string[] sep = { ";" };
                        string[] responseTemp = new string[1];
                        string[] responseTemp1 = new string[1];
                        if (transactionType == "22")
                        {
                            string[] resp = res.Split(sep, StringSplitOptions.None);
                            string responseCommand = resp[1];
                            int count = int.Parse(resp[5]);
                            responseTemp1 = new string[resp.Length - 2];
                            int m = 1;
                            for (int n = 1; n < resp.Length - 2; n++)
                            {
                                responseTemp1[m] = resp[n];
                                m = m + 1;
                            }
                            res = "";
                            res = ConvertStringArrayToString(responseTemp1);
                            if (count > 0)
                            {
                                responseSummary = ConvertStringArrayToString(responseTemp1);
                                for (int i = 1; i <= count; i++)
                                {
                                    res = "";
                                    ECRREferenceNumber = String.Concat(CashRegisterNumber, ECRREFtext);
                                    ecrReferenceNo = ECRREFtext;
                                    dateTime = now.ToString("dd MM yy HH:mm:ss");
                                    dateTime = dateTime.Replace(" ", "").Replace(":", "");
                                    inputReqData = dateTime + ";" + i + ";" + ECRREferenceNumber + "!";
                                    szSignature = String.Concat(ecrReferenceNo, TerminalId);
                                    szSignature = obj.ComputeSha256Hash(szSignature);
                                    responseInt = obj.doCOMTransaction(int.Parse(PortCom), baudRateCom, parityCom, dataBitsCom, stopBitsCom, inputReqData, int.Parse(transactionType), szSignature, out res);
                                    if (responseInt == 0)
                                    {
                                        string[] resp1 = res.Split(sep, StringSplitOptions.None);
                                        responseTemp = new string[resp1.Length - 6];
                                        int j = 0;
                                        for (int k = 4; k < resp1.Length - 2; k++)
                                        {
                                            responseTemp[j] = resp1[k];
                                            j = j + 1;
                                        }
                                        res = "";
                                        res = ConvertStringArrayToString(responseTemp);
                                        responseSummary = String.Concat(responseSummary, res);
                                        int refNumber1 = int.Parse(ECRREFtext) + 1;
                                        ECRREFtext = refNumber1.ToString("D6");
                                        (sender as BackgroundWorker).ReportProgress(4);
                                    }
                                }
                                res = responseSummary;
                            }
                        }
                    }
                    else
                    {
                        responseInt = obj.doTCPIPTransaction(hostIp, int.Parse(port), inputReqData, int.Parse(transactionType), szSignature, out res);
                        //doTransFlag = 1;
                        //if (flagstatus)
                        //{
                        //    responseInt = obj.doTCPIPTransaction(hostIp, int.Parse(port), inputReqData, int.Parse(transactionType), szSignature, out res);
                        //}
                        //else
                        //{
                        //    MessageBox.Show("Check wifi connection");
                        //}
                        // doTransFlag = 0;
                        string responseSummary;
                        string[] sep = { ";" };
                        string[] responseTemp = new string[1];
                        string[] responseTemp1 = new string[1];
                        if (transactionType == "22")
                        {
                            string[] resp = res.Split(sep, StringSplitOptions.None);
                            string responseCommand = resp[1];
                            int count = int.Parse(resp[5]);
                            responseTemp1 = new string[resp.Length - 2];
                            int m = 1;
                            for (int n = 1; n < resp.Length - 2; n++)
                            {
                                responseTemp1[m] = resp[n];
                                m = m + 1;
                            }
                            res = "";
                            res = ConvertStringArrayToString(responseTemp1);
                            if (count > 0)
                            {
                                responseSummary = ConvertStringArrayToString(responseTemp1);
                                for (int i = 1; i <= count; i++)
                                {
                                    res = "";
                                    ECRREferenceNumber = String.Concat(CashRegisterNumber, ECRREFtext);
                                    ecrReferenceNo = ECRREFtext;
                                    dateTime = now.ToString("dd MM yy HH:mm:ss");
                                    dateTime = dateTime.Replace(" ", "").Replace(":", "");
                                    inputReqData = dateTime + ";" + i + ";" + ECRREferenceNumber + "!";
                                    szSignature = String.Concat(ecrReferenceNo, TerminalId);
                                    szSignature = obj.ComputeSha256Hash(szSignature);
                                    responseInt = obj.doTCPIPTransaction(hostIp, int.Parse(port), inputReqData, int.Parse(transactionType), szSignature, out res);
                                    //if (flagstatus)
                                    //{
                                    //    responseInt = obj.doTCPIPTransaction(hostIp, int.Parse(port), inputReqData, int.Parse(transactionType), szSignature, out res);
                                    //}
                                    //else
                                    //{
                                    //    MessageBox.Show("Check wifi connection");
                                    //}
                                    if (responseInt == 0)
                                    {
                                        string[] resp1 = res.Split(sep, StringSplitOptions.None);
                                        responseTemp = new string[resp1.Length - 6];
                                        int j = 0;
                                        for (int k = 4; k < resp1.Length - 2; k++)
                                        {
                                            responseTemp[j] = resp1[k];
                                            j = j + 1;
                                        }
                                        res = "";
                                        res = ConvertStringArrayToString(responseTemp);
                                        responseSummary = String.Concat(responseSummary, res);
                                        int refNumber1 = int.Parse(ECRREFtext) + 1;
                                        ECRREFtext = refNumber1.ToString("D6");
                                        (sender as BackgroundWorker).ReportProgress(4);
                                    }
                                }
                                res = responseSummary;
                            }
                        }
                    }
                    if (responseInt == 0)
                    {
                        string[] sep = { ";" };
                        string[] resp = res.Split(sep, StringSplitOptions.None);
                        string responseCommand = resp[1];
                        string ResponseMessage = resp[3];
                        ResponseMessage = ResponseMessage.ToLower();
                        string covertResponseMessage = char.ToUpper(ResponseMessage[0]) + ResponseMessage.Substring(1);
                        response = resp[0];
                        // TranslatorService service = new TranslatorService();
                        var sourceLanguage = "English";
                        var targetLanguage = "Arabic";
                        string pan = "";
                        if (responseCommand == "0" || responseCommand == "1" || responseCommand == "8" || responseCommand == "3" || responseCommand == "9" || responseCommand == "2" || responseCommand == "4" || responseCommand == "5" || responseCommand == "6" || responseCommand == "20" || responseCommand == "22")
                        {
                            if (resp[4].Length > 12)
                            {
                                pan = obj.MaskDigitsPan(resp[4]);
                            }

                        }
                        if (responseCommand == "18")
                        {
                            if (resp[2] == "00")
                            {
                                status = 1;
                            }
                        }
                        if (responseCommand == "19")
                        {
                            if (resp[2] == "00")
                            {
                                status = 0;
                            }
                        }
                        if (responseCommand == "17")
                        {
                            status = 0;
                            TerminalId = resp[3];
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Terminal id" + "\t" + "\t" + "\t" + ": " + resp[3] + "\n";
                        }
                        else if (responseCommand == "0" && resp.Length > 27)
                        {
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Purchase(customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];

                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(0, 2) + "/" + respDateTime.Substring(2, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("cardEntryMode", resp[24]);
                                builder.Replace("cardEntyModeResp", resp[2]);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("ResponseMessage", covertResponseMessage);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AID", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[6]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[27]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }

                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                        }
                        else if (responseCommand == "1" && resp.Length > 29)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";

                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Purchase cashback(customer copy)).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal TransactionAmount = Convert.ToDecimal(resp[5]) / 100;
                                decimal CashbackAmount = Convert.ToDecimal(resp[6]) / 100;
                                decimal TotalAmount = Convert.ToDecimal(resp[7]) / 100;
                                string ExpiryDate = resp[9];

                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(0, 2) + "/" + respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(4, 4);
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSARPUR", numToArabicConverter(string.Format("{0:0.00##}", TransactionAmount.ToString())));
                                builder.Replace("amountSARcashback", numToArabicConverter(string.Format("{0:0.00##}", CashbackAmount.ToString())));
                                builder.Replace("amountSARtotal", numToArabicConverter(string.Format("{0:0.00##}", TotalAmount.ToString())));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[8]);
                                builder.Replace("authCode", resp[13]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("TransactionAmount", (string.Format("{0:0.00##}", TransactionAmount)));
                                builder.Replace("CashbackAmount", (string.Format("{0:0.00##}", CashbackAmount)));
                                builder.Replace("TotalAmount", (string.Format("{0:0.00##}", TotalAmount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CardEntryMode", resp[26]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AID", resp[17]);
                                builder.Replace("TVR", resp[21]);
                                builder.Replace("CVR", resp[20]);
                                builder.Replace("applicationCryptogram", resp[18]);
                                builder.Replace("CID", resp[19]);
                                builder.Replace("MID", resp[15]);
                                builder.Replace("TID", resp[14]);
                                builder.Replace("RRN", resp[12]);
                                builder.Replace("StanNo", resp[9]);
                                builder.Replace("TSI", resp[22]);
                                builder.Replace("ApplicationVersion", resp[31]);
                                builder.Replace("SchemeLabel", resp[29]);
                                builder.Replace("MerchantCategoryCode", resp[27]);
                                builder.Replace("Merchant Name", resp[33]);
                                builder.Replace("Merchant Address", resp[34]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[35]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[36]));
                                builder.Replace("Disclaimer", resp[32]);
                                string arabicpin = CheckingArabicPin(resp[27]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "8" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext =
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";

                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Cash_Advance(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[8];

                                string currentDate = respDateTime.Substring(0, 2) + "/" + respDateTime.Substring(2, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("ResponseMessage", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CardEntryMode", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AID", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[6]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[27]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "3" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext =
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Pre-Auth(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("ResponseMessage", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CardEntryMode", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AID", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[6]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[27]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "9" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext =
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                            if (resp[2] == "400")
                            {
                                string pathToHTMLFile = @"printReceipt\\Reversal(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("ResponseMessage", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CardEntryMode", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AID", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[6]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[27]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "2" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";

                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Refund(customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("ResponseMessage", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CardEntryMode", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AID", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[6]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[27]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "4" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";

                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Purchase Advice(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("ResponseMessage", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CardEntryMode", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AID", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[6]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[27]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "5" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";

                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Pre-Extension(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("ResponseMessage", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CardEntryMode", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AID", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[6]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[27]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "6" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";

                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Pre-void(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("ResponseMessage", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CardEntryMode", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AID", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[6]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[27]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "10")
                        {
                            if (resp[2] == "500" || resp[2] == "501")
                            {
                                string pathToHTMLFile = @"printReceipt\\Reconcilation.html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);

                                string summaryToHTMLFilePosTable = @"printReceipt\\PosTable.html";
                                string summaryhtmlStringPosTable = File.ReadAllText(summaryToHTMLFilePosTable);
                                StringBuilder htmlSummaryReportPosTable = new StringBuilder(summaryhtmlStringPosTable);

                                string summaryToHTMLFileMadaHost = @"printReceipt\\madaHostTable.html";
                                string summaryhtmlStringMadaHost = File.ReadAllText(summaryToHTMLFileMadaHost);
                                StringBuilder htmlSummaryReportMadaHost = new StringBuilder(summaryhtmlStringMadaHost);

                                string summaryToHTMLFilePosDetials = @"printReceipt\\PosTerminalDetails.html";
                                string summaryhtmlStringPosDetials = File.ReadAllText(summaryToHTMLFilePosDetials);
                                StringBuilder htmlSummaryReportPosDetials = new StringBuilder(summaryhtmlStringPosDetials);

                                string SummaryFinalReport = "";
                                string respDateTime = resp[4];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);

                                int b = 9;
                                int totalSchemeLengthL = int.Parse(resp[9]);
                                for (int j = 1; j <= totalSchemeLengthL; j++)
                                {

                                    if (resp[b + 2] == "0")
                                    {
                                        string ReconcilationToHTMLFile = @"printReceipt\\ReconcilationTable.html";
                                        string ReconcilationNoTable = File.ReadAllText(ReconcilationToHTMLFile);
                                        StringBuilder htmlreconcilationNoTable = new StringBuilder(ReconcilationNoTable);
                                        string arabi = CheckingArabic(resp[b + 1]);
                                        arabi = arabi.Replace("\u08F1", "");

                                        htmlreconcilationNoTable.Replace("Scheme", resp[b + 1]);
                                        htmlreconcilationNoTable.Replace("قَدِيرٞ", arabi);
                                        b = b + 3;

                                        SummaryFinalReport += htmlreconcilationNoTable.ToString();
                                        htmlreconcilationNoTable = new StringBuilder(ReconcilationNoTable);
                                    }
                                    else
                                    {
                                        if (resp[b + 3] == "mada HOST")
                                        {
                                            j = j - 1;
                                            string arabic12 = CheckingArabic(resp[b + 1]);
                                            arabic12 = arabic12.Replace("\u08F1", "");
                                            htmlSummaryReportMadaHost.Replace("مدى", arabic12);
                                            htmlSummaryReportMadaHost.Replace("schemename", resp[b + 1]);
                                            htmlSummaryReportMadaHost.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 4])).ToString());
                                            htmlSummaryReportMadaHost.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 5]) / 100))));
                                            htmlSummaryReportMadaHost.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 6])).ToString());
                                            htmlSummaryReportMadaHost.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 7]) / 100))));
                                            htmlSummaryReportMadaHost.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 8])).ToString());
                                            htmlSummaryReportMadaHost.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 9]) / 100))));
                                            htmlSummaryReportMadaHost.Replace("CADVCount", (Convert.ToDecimal(resp[b + 10])).ToString());
                                            htmlSummaryReportMadaHost.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 11]) / 100))));
                                            htmlSummaryReportMadaHost.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 12])).ToString());
                                            htmlSummaryReportMadaHost.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 13]) / 100))));
                                            htmlSummaryReportMadaHost.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 14])).ToString());
                                            htmlSummaryReportMadaHost.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 15]) / 100))));
                                            b = b + 15;
                                            SummaryFinalReport += htmlSummaryReportMadaHost.ToString();
                                            htmlSummaryReportMadaHost = new StringBuilder(summaryhtmlStringMadaHost);
                                        }
                                        else if (resp[b + 2] == "POS TERMINAL")
                                        {
                                            j = j - 1;
                                            htmlSummaryReportPosTable.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 3])).ToString());
                                            htmlSummaryReportPosTable.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 4]) / 100))));
                                            htmlSummaryReportPosTable.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 5])).ToString());
                                            htmlSummaryReportPosTable.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 6]) / 100))));
                                            htmlSummaryReportPosTable.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 7])).ToString());
                                            htmlSummaryReportPosTable.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 8]) / 100))));
                                            htmlSummaryReportPosTable.Replace("CADVCount", (Convert.ToDecimal(resp[b + 9])).ToString());
                                            htmlSummaryReportPosTable.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 10]) / 100))));
                                            htmlSummaryReportPosTable.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 11])).ToString());
                                            htmlSummaryReportPosTable.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 12]) / 100))));
                                            htmlSummaryReportPosTable.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 13])).ToString());
                                            htmlSummaryReportPosTable.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 14]) / 100))));
                                            b = b + 14;
                                            SummaryFinalReport += htmlSummaryReportPosTable.ToString();
                                            htmlSummaryReportPosTable = new StringBuilder(summaryhtmlStringPosTable);
                                        }
                                        else if (resp[b + 2] == "POS TERMINAL DETAILS")
                                        {
                                            j = j - 1;
                                            htmlSummaryReportPosDetials.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 3])).ToString());
                                            htmlSummaryReportPosDetials.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 4]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 5])).ToString());
                                            htmlSummaryReportPosDetials.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 6]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 7])).ToString());
                                            htmlSummaryReportPosDetials.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 8]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("CADVCount", (Convert.ToDecimal(resp[b + 9])).ToString());
                                            htmlSummaryReportPosDetials.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 10]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 11])).ToString());
                                            htmlSummaryReportPosDetials.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 12]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 13])).ToString());
                                            htmlSummaryReportPosDetials.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 14]) / 100))));
                                            b = b + 14;
                                            SummaryFinalReport += htmlSummaryReportPosDetials.ToString();
                                            htmlSummaryReportPosDetials = new StringBuilder(summaryhtmlStringPosDetials);
                                        }
                                        else if (resp[b + 1] == "0")
                                        {
                                            string ReconcilationToHTMLFile = @"printReceipt\\ReconcilationTable1.html";
                                            string ReconcilationNoTable = File.ReadAllText(ReconcilationToHTMLFile);
                                            SummaryFinalReport += ReconcilationNoTable;
                                            b = b + 1;
                                        }
                                    }
                                }
                                builder.Replace("PosTable", SummaryFinalReport);
                                builder.Replace("merchantId", resp[5]);
                                builder.Replace("busscode", resp[6]);
                                builder.Replace("traceNumber", resp[7]);
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("AppVersion", resp[8]);
                                builder.Replace("TerminalId", TerminalId);
                                builder.Replace("Merchant Name", resp[b + 1]);
                                builder.Replace("Merchant Address", resp[b + 2]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[b + 3]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[b + 4]));
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));

                                string printSettlment =
                                                   "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                   "Scheme HOST" + "\t" + "\t" + "\t" + ": SchemeHOST \n" +
                                                   "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                   "Total Debit Count" + "\t" + "\t" + "\t" + ": TotalDebitCount \n" +
                                                   "Total Debit Amount" + "\t" + "\t" + ": TotalDebitAmount \n" +
                                                   "Total Credit Count" + "\t" + "\t" + "\t" + ": TotalCreditCount \n" +
                                                   "Total Credit Amount" + "\t" + "\t" + ": TotalCreditAmount \n" +
                                                   "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                   "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                   "C/ADV Count" + "\t" + "\t" + "\t" + ": CADVCount \n" +
                                                   "C/ADV Amount" + "\t" + "\t" + "\t" + ": CADVAmount \n" +
                                                   "Auth Count" + "\t" + "\t" + "\t" + ": AuthCount \n" +
                                                   "Auth Amount" + "\t" + "\t" + "\t" + ": AuthAmount \n" +
                                                   "Total Count" + "\t" + "\t" + "\t" + ": TotalCount \n" +
                                                   "Total Amount" + "\t" + "\t" + "\t" + ": TotalAmount \n";

                                string printSettlmentPos =
                                                        "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                        "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                        "Total Debit Count" + "\t" + "\t" + "\t" + ": TotalDebitCount \n" +
                                                        "Total Debit Amount" + "\t" + "\t" + ": TotalDebitAmount \n" +
                                                        "Total Credit Count" + "\t" + "\t" + "\t" + ": TotalCreditCount \n" +
                                                        "Total Credit Amount" + "\t" + "\t" + ": TotalCreditAmount \n" +
                                                        "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                        "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                        "C/ADV Count" + "\t" + "\t" + "\t" + ": CADVCount \n" +
                                                        "C/ADV Amount" + "\t" + "\t" + "\t" + ": CADVAmount \n" +
                                                        "Auth Count" + "\t" + "\t" + "\t" + ": AuthCount \n" +
                                                        "Auth Amount" + "\t" + "\t" + "\t" + ": AuthAmount \n" +
                                                        "Total Count" + "\t" + "\t" + "\t" + ": TotalCount \n" +
                                                        "Total Amount" + "\t" + "\t" + "\t" + ": TotalAmount \n";
                                string printSettlmentPosDetails =
                                                        "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                        "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                        "P/OFF Count" + "\t" + "\t" + "\t" + ": POFFCount \n" +
                                                        "P/OFF Amount" + "\t" + "\t" + "\t" + ": POFFAmount \n" +
                                                        "P/ON Count" + "\t" + "\t" + "\t" + ": PONCount \n" +
                                                        "P/ON Amount" + "\t" + "\t" + "\t" + ": PONAmount \n" +
                                                        "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                        "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                        "REVERSAL Count" + "\t" + "\t" + "\t" + ": REVERSALCount \n" +
                                                        "REVERSAL Amount" + "\t" + "\t" + ": REVERSALAmount \n" +
                                                        "REFUND Count" + "\t" + "\t" + "\t" + ": REFUNDCount \n" +
                                                        "REFUND Amount" + "\t" + "\t" + "\t" + ": REFUNDAmount \n" +
                                                        "COMP Count" + "\t" + "\t" + "\t" + ": COMPCount \n" +
                                                        "COMP Amount" + "\t" + "\t" + "\t" + ": COMPAmount \n";
                                StringBuilder printSettlment1 = new StringBuilder(printSettlment);
                                StringBuilder printSettlmentPos1 = new StringBuilder(printSettlmentPos);
                                StringBuilder printSettlmentPosDetails1 = new StringBuilder(printSettlmentPosDetails);

                                string printFinalReport1 = "";
                                int k = 9;
                                int totalSchemeLength = int.Parse(resp[9]);
                                for (int i = 1; i <= totalSchemeLength; i++)
                                {

                                    if (resp[k + 2] == "0")
                                    {
                                        string printSettlmentNO = "Scheme Name" + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                                   "<No Transaction> \n";
                                        k = k + 3;
                                        StringBuilder printSettlment2 = new StringBuilder(printSettlmentNO);
                                        printFinalReport1 += printSettlment2.ToString();
                                    }
                                    else
                                    {
                                        if (resp[k + 3] == "mada HOST")
                                        {
                                            i = i - 1;
                                            printSettlment1.Replace("SchemeName", resp[k + 1]);
                                            printSettlment1.Replace("TransactionAvailableFlag", resp[k + 2]);
                                            printSettlment1.Replace("SchemeHOST", resp[k + 3]);
                                            printSettlment1.Replace("TotalDebitCount", resp[k + 4]);
                                            printSettlment1.Replace("TotalDebitAmount", "");
                                            printSettlment1.Replace("TotalCreditCount", resp[k + 6]);
                                            printSettlment1.Replace("TotalCreditAmount", resp[k + 7]);
                                            printSettlment1.Replace("NAQDCount", resp[k + 8]);
                                            printSettlment1.Replace("NAQDAmount", resp[k + 9]);
                                            printSettlment1.Replace("CADVCount", resp[k + 10]);
                                            printSettlment1.Replace("CADVAmount", resp[k + 11]);
                                            printSettlment1.Replace("AuthCount", resp[k + 12]);
                                            printSettlment1.Replace("AuthAmount", resp[k + 13]);
                                            printSettlment1.Replace("TotalCount", resp[k + 14]);
                                            printSettlment1.Replace("TotalAmount", "");
                                            k = k + 15;
                                            printFinalReport1 += printSettlment1.ToString();
                                            printSettlment1 = new StringBuilder(printSettlment);
                                        }
                                        else if (resp[k + 2] == "POS TERMINAL")
                                        {
                                            i = i - 1;
                                            printSettlmentPos1.Replace("TransactionAvailableFlag", resp[k + 1]);
                                            printSettlmentPos1.Replace("SchemeName", resp[k + 2]);
                                            printSettlmentPos1.Replace("TotalDebitCount", resp[k + 3]);
                                            printSettlmentPos1.Replace("TotalDebitAmount", resp[k + 4]);
                                            printSettlmentPos1.Replace("TotalCreditCount", resp[k + 5]);
                                            printSettlmentPos1.Replace("TotalCreditAmount", resp[k + 6]);
                                            printSettlmentPos1.Replace("NAQDCount", resp[k + 7]);
                                            printSettlmentPos1.Replace("NAQDAmount", resp[k + 8]);
                                            printSettlmentPos1.Replace("CADVCount", resp[k + 9]);
                                            printSettlmentPos1.Replace("CADVAmount", resp[k + 10]);
                                            printSettlmentPos1.Replace("AuthCount", resp[k + 11]);
                                            printSettlmentPos1.Replace("AuthAmount", resp[k + 12]);
                                            printSettlmentPos1.Replace("TotalCount", resp[k + 13]);
                                            printSettlmentPos1.Replace("TotalAmount", resp[k + 14]);
                                            k = k + 14;
                                            printFinalReport1 += printSettlmentPos1.ToString();
                                            printSettlmentPos1 = new StringBuilder(printSettlmentPos);
                                        }
                                        else if (resp[k + 2] == "POS TERMINAL DETAILS")
                                        {
                                            i = i - 1;
                                            printSettlmentPosDetails1.Replace("TransactionAvailableFlag", resp[k + 1]);
                                            printSettlmentPosDetails1.Replace("SchemeName", resp[k + 2]);
                                            printSettlmentPosDetails1.Replace("POFFCount", resp[k + 3]);
                                            printSettlmentPosDetails1.Replace("POFFAmount", resp[k + 4]);
                                            printSettlmentPosDetails1.Replace("PONCount", resp[k + 5]);
                                            printSettlmentPosDetails1.Replace("PONAmount", resp[k + 6]);
                                            printSettlmentPosDetails1.Replace("NAQDCount", resp[k + 7]);
                                            printSettlmentPosDetails1.Replace("NAQDAmount", resp[k + 8]);
                                            printSettlmentPosDetails1.Replace("REVERSALCount", resp[k + 9]);
                                            printSettlmentPosDetails1.Replace("REVERSALAmount", resp[k + 10]);
                                            printSettlmentPosDetails1.Replace("REFUNDCount", resp[k + 11]);
                                            printSettlmentPosDetails1.Replace("REFUNDAmount", resp[k + 12]);
                                            printSettlmentPosDetails1.Replace("COMPCount", resp[k + 13]);
                                            printSettlmentPosDetails1.Replace("COMPAmount", resp[k + 14]);
                                            k = k + 14;
                                            printFinalReport1 += printSettlmentPosDetails1.ToString();
                                            printSettlmentPosDetails1 = new StringBuilder(printSettlmentPosDetails);
                                        }
                                        else if (resp[k + 1] == "0")
                                        {
                                            string printSettlmentNO1 = "Scheme Name" + "\t" + "\t" + "\t" + ": POS TERMINAL \n" +
                                                                    "<No Transaction> \n" +
                                                                     "Scheme Name" + "\t" + "\t" + "\t" + ": POS TERMINAL Details\n" +
                                                                     "<No Transaction> \n";
                                            k = k + 1;
                                            StringBuilder printSettlment2 = new StringBuilder(printSettlmentNO1);
                                            printFinalReport1 += printSettlment2.ToString();
                                        }
                                    }
                                }
                                BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                     "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                     "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                     "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                     "Merchent ID" + "\t" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                     "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                     "Trace Number" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                     "Application Version" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                     "Total Scheme Length" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                     printFinalReport1 +
                                                     "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                     "Merchant Address " + "\t" + "\t" + ": " + resp[k + 2] + "\n" +
                                                     "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[k + 3] + "\n" +
                                                      "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[k + 4] + "\n" +
                                                      "ECR Transaction Reference Number" + "\t" + ": " + resp[k + 5] + "\n" +
                                                      "Signature" + "\t" + "\t" + "\t" + ": " + resp[k + 6] + "\n";
                            }
                            else
                            {
                                BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                     "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                     "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                     "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                     "Merchant Address " + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                     "ECR Transaction Reference Number : " + resp[6] + "\n" +
                                                     "Signature" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n";
                            }


                        }
                        else if (responseCommand == "11" && resp.Length > 4)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                 "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                 "ECR Transaction Reference Number : " + resp[5] + "\n" +
                                                 "Signature" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n";
                            if (resp[2] == "300" && resp[3] != "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Parameter download.html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                string respDateTime = resp[4];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("terminalId", TerminalId.Substring(0, 8));
                                builder.Replace("responseCode", resp[2]);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "12" && resp.Length > 4)
                        {

                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                 "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                 "ECR Transaction Reference Number : " + resp[5] + "\n" +
                                                 "Signature" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n";
                        }
                        else if (responseCommand == "13" && resp.Length > 9)
                        {

                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                 "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                 "Vendor ID" + "\t" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                 "Vendor Terminal type" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                 "TRSM ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                 "Vendor Key Index" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                 "SAMA Key Index" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                 "ECR Transaction Reference Number : " + resp[10] + "\n" +
                                                 "Signature" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n";
                        }
                        else if (responseCommand == "18")
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n";
                        }
                        else if (responseCommand == "19")
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n";
                        }
                        else if (responseCommand == "20" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                   "Product Info" + "\t" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[31] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[32] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[33] + "\n";
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Bill Pyment(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];

                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("ResponseMessage", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CardEntryMode", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AID", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[6]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[27]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "21")
                        {
                            if (resp[2] == "00")
                            {
                                string pathToHTMLFile = @"printReceipt\\Detail_Report.html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);

                                string summaryToHTMLFilePosTable = @"printReceipt\\PosTableRunning.html";
                                string summaryhtmlStringPosTable = File.ReadAllText(summaryToHTMLFilePosTable);
                                StringBuilder htmlSummaryReportPosTable = new StringBuilder(summaryhtmlStringPosTable);

                                string summaryToHTMLFilePosDetials = @"printReceipt\\PosTerminalDetails.html";
                                string summaryhtmlStringPosDetials = File.ReadAllText(summaryToHTMLFilePosDetials);
                                StringBuilder htmlSummaryReportPosDetials = new StringBuilder(summaryhtmlStringPosDetials);

                                string SummaryFinalReport = "";
                                string respDateTime = resp[4];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);

                                int b = 8;
                                int totalSchemeLengthL = int.Parse(resp[8]);
                                for (int j = 1; j <= totalSchemeLengthL; j++)
                                {

                                    if (resp[b + 2] == "0")
                                    {
                                        string ReconcilationToHTMLFile = @"printReceipt\\ReconcilationTable.html";
                                        string ReconcilationNoTable = File.ReadAllText(ReconcilationToHTMLFile);
                                        StringBuilder htmlreconcilationNoTable = new StringBuilder(ReconcilationNoTable);
                                        string arabi = CheckingArabic(resp[b + 1]);
                                        arabi = arabi.Replace("\u08F1", "");

                                        htmlreconcilationNoTable.Replace("Scheme", resp[b + 1]);
                                        htmlreconcilationNoTable.Replace("قَدِيرٞ", arabi);
                                        b = b + 2;

                                        SummaryFinalReport += htmlreconcilationNoTable.ToString();
                                        htmlreconcilationNoTable = new StringBuilder(ReconcilationNoTable);
                                    }
                                    else
                                    {
                                        if (resp[b + 3] == "POS TERMINAL")
                                        {
                                            string arabi = CheckingArabic(resp[b + 1]);
                                            arabi = arabi.Replace("\u08F1", "");
                                            htmlSummaryReportPosTable.Replace("مدى", arabi);
                                            htmlSummaryReportPosTable.Replace("schemename", resp[b + 1]);
                                            htmlSummaryReportPosTable.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 4])).ToString());
                                            htmlSummaryReportPosTable.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 5]) / 100))));
                                            htmlSummaryReportPosTable.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 6])).ToString());
                                            htmlSummaryReportPosTable.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 7]) / 100))));
                                            htmlSummaryReportPosTable.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 8])).ToString());
                                            htmlSummaryReportPosTable.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 9]) / 100))));
                                            htmlSummaryReportPosTable.Replace("CADVCount", (Convert.ToDecimal(resp[b + 10])).ToString());
                                            htmlSummaryReportPosTable.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 11]) / 100))));
                                            htmlSummaryReportPosTable.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 12])).ToString());
                                            htmlSummaryReportPosTable.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 13]) / 100))));
                                            htmlSummaryReportPosTable.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 14])).ToString());
                                            htmlSummaryReportPosTable.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 15]) / 100))));
                                            b = b + 15;
                                            SummaryFinalReport += htmlSummaryReportPosTable.ToString();
                                            htmlSummaryReportPosTable = new StringBuilder(summaryhtmlStringPosTable);
                                        }
                                        else if (resp[b + 2] == "POS TERMINAL DETAILS")
                                        {
                                            j = j - 1;
                                            htmlSummaryReportPosDetials.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 3])).ToString());
                                            htmlSummaryReportPosDetials.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 4]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 5])).ToString());
                                            htmlSummaryReportPosDetials.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 6]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 7])).ToString());
                                            htmlSummaryReportPosDetials.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 8]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("CADVCount", (Convert.ToDecimal(resp[b + 9])).ToString());
                                            htmlSummaryReportPosDetials.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 10]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 11])).ToString());
                                            htmlSummaryReportPosDetials.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 12]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 13])).ToString());
                                            htmlSummaryReportPosDetials.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 14]) / 100))));
                                            b = b + 14;
                                            SummaryFinalReport += htmlSummaryReportPosDetials.ToString();
                                            htmlSummaryReportPosDetials = new StringBuilder(summaryhtmlStringPosDetials);
                                        }
                                    }

                                }
                                builder.Replace("PosTable", SummaryFinalReport);
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("BuzzCode", resp[6]);
                                builder.Replace("AppVersion", resp[7]);
                                builder.Replace("TerminalId", TerminalId);
                                builder.Replace("Merchant Name", resp[b + 40]);
                                builder.Replace("Merchant Address", resp[b + 41]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[b + 42]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[b + 43]));
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));

                                string printSettlmentPos =
                                                  "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                   "Scheme HOST" + "\t" + "\t" + "\t" + ": SchemeHOST \n" +
                                                   "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                   "Total Debit Count" + "\t" + "\t" + "\t" + ": TotalDebitCount \n" +
                                                   "Total Debit Amount" + "\t" + "\t" + ": TotalDebitAmount \n" +
                                                   "Total Credit Count" + "\t" + "\t" + "\t" + ": TotalCreditCount \n" +
                                                   "Total Credit Amount" + "\t" + "\t" + ": TotalCreditAmount \n" +
                                                   "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                   "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                   "C/ADV Count" + "\t" + "\t" + "\t" + ": CADVCount \n" +
                                                   "C/ADV Amount" + "\t" + "\t" + "\t" + ": CADVAmount \n" +
                                                   "Auth Count" + "\t" + "\t" + "\t" + ": AuthCount \n" +
                                                   "Auth Amount" + "\t" + "\t" + "\t" + ": AuthAmount \n" +
                                                   "Total Count" + "\t" + "\t" + "\t" + ": TotalCount \n" +
                                                   "Total Amount" + "\t" + "\t" + "\t" + ": TotalAmount \n";
                                string printSettlmentPosDetails =
                                                        "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                        "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                        "P/OFF Count" + "\t" + "\t" + "\t" + ": POFFCount \n" +
                                                        "P/OFF Amount" + "\t" + "\t" + "\t" + ": POFFAmount \n" +
                                                        "P/ON Count" + "\t" + "\t" + "\t" + ": PONCount \n" +
                                                        "P/ON Amount" + "\t" + "\t" + "\t" + ": PONAmount \n" +
                                                        "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                        "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                        "REVERSAL Count" + "\t" + "\t" + "\t" + ": REVERSALCount \n" +
                                                        "REVERSAL Amount" + "\t" + "\t" + ": REVERSALAmount \n" +
                                                        "REFUND Count" + "\t" + "\t" + "\t" + ": REFUNDCount \n" +
                                                        "REFUND Amount" + "\t" + "\t" + "\t" + ": REFUNDAmount \n" +
                                                        "COMP Count" + "\t" + "\t" + "\t" + ": COMPCount \n" +
                                                        "COMP Amount" + "\t" + "\t" + "\t" + ": COMPAmount \n";
                                StringBuilder printSettlmentPos1 = new StringBuilder(printSettlmentPos);
                                StringBuilder printSettlmentPosDetails1 = new StringBuilder(printSettlmentPosDetails);

                                string printFinalReport1 = "";
                                int k = 8;
                                int totalSchemeLength = int.Parse(resp[8]);
                                for (int i = 1; i <= totalSchemeLength; i++)
                                {

                                    if (resp[k + 2] == "0")
                                    {
                                        string printSettlmentNO = "Scheme Name" + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                                   "<No Transaction> \n";
                                        k = k + 2;
                                        StringBuilder printSettlment2 = new StringBuilder(printSettlmentNO);
                                        printFinalReport1 += printSettlment2.ToString();
                                    }
                                    else
                                    {
                                        if (resp[k + 3] == "POS TERMINAL")
                                        {
                                            printSettlmentPos1.Replace("SchemeName", resp[k + 1]);
                                            printSettlmentPos1.Replace("TransactionAvailableFlag", resp[k + 2]);
                                            printSettlmentPos1.Replace("SchemeHOST", resp[k + 3]);
                                            printSettlmentPos1.Replace("TotalDebitCount", resp[k + 4]);
                                            printSettlmentPos1.Replace("TotalDebitAmount", resp[k + 5]);
                                            printSettlmentPos1.Replace("TotalCreditCount", resp[k + 6]);
                                            printSettlmentPos1.Replace("TotalCreditAmount", resp[k + 7]);
                                            printSettlmentPos1.Replace("NAQDCount", resp[k + 8]);
                                            printSettlmentPos1.Replace("NAQDAmount", resp[k + 9]);
                                            printSettlmentPos1.Replace("CADVCount", resp[k + 10]);
                                            printSettlmentPos1.Replace("CADVAmount", resp[k + 11]);
                                            printSettlmentPos1.Replace("AuthCount", resp[k + 12]);
                                            printSettlmentPos1.Replace("AuthAmount", resp[k + 13]);
                                            printSettlmentPos1.Replace("TotalCount", resp[k + 14]);
                                            printSettlmentPos1.Replace("TotalAmount", resp[k + 15]);
                                            k = k + 15;
                                            printFinalReport1 += printSettlmentPos1.ToString();
                                            printSettlmentPos1 = new StringBuilder(printSettlmentPos);
                                        }
                                        else if (resp[k + 2] == "POS TERMINAL DETAILS")
                                        {
                                            i = i - 1;
                                            printSettlmentPosDetails1.Replace("TransactionAvailableFlag", resp[k + 1]);
                                            printSettlmentPosDetails1.Replace("SchemeName", resp[k + 2]);
                                            printSettlmentPosDetails1.Replace("POFFCount", resp[k + 3]);
                                            printSettlmentPosDetails1.Replace("POFFAmount", resp[k + 4]);
                                            printSettlmentPosDetails1.Replace("PONCount", resp[k + 5]);
                                            printSettlmentPosDetails1.Replace("PONAmount", resp[k + 6]);
                                            printSettlmentPosDetails1.Replace("NAQDCount", resp[k + 7]);
                                            printSettlmentPosDetails1.Replace("NAQDAmount", resp[k + 8]);
                                            printSettlmentPosDetails1.Replace("REVERSALCount", resp[k + 9]);
                                            printSettlmentPosDetails1.Replace("REVERSALAmount", resp[k + 10]);
                                            printSettlmentPosDetails1.Replace("REFUNDCount", resp[k + 11]);
                                            printSettlmentPosDetails1.Replace("REFUNDAmount", resp[k + 12]);
                                            printSettlmentPosDetails1.Replace("COMPCount", resp[k + 13]);
                                            printSettlmentPosDetails1.Replace("COMPAmount", resp[k + 14]);
                                            k = k + 14;
                                            printFinalReport1 += printSettlmentPosDetails1.ToString();
                                            printSettlmentPosDetails1 = new StringBuilder(printSettlmentPosDetails);
                                        }
                                    }
                                }
                                BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                     "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                     "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                     "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                     "Trace Number" + "\t" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                     "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                     "Application Version" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                     "Total Scheme Length" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                     printFinalReport1 +
                                                     "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[k + 40] + "\n" +
                                                     "Merchant Address " + "\t" + "\t" + ": " + resp[k + 41] + "\n" +
                                                     "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[k + 42] + "\n" +
                                                      "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[k + 43] + "\n" +
                                                      "ECR Transaction Reference Number" + "\t" + ": " + resp[k + 44] + "\n" +
                                                      "Signature" + "\t" + "\t" + "\t" + ": " + resp[k + 45] + "\n";
                            }
                            else
                            {
                                BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                    "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                    "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                     "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                     "Merchant Address " + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                     "ECR Transaction Reference Number : " + resp[6] + "\n" +
                                                     "Signature" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n";
                            }
                        }
                        else if (responseCommand == "22")
                        {
                            string pathToHTMLFile = @"printReceipt\\Summary_Report.html";
                            htmlString = File.ReadAllText(pathToHTMLFile);
                            StringBuilder builder = new StringBuilder(htmlString);
                            string summaryToHTMLFile = @"printReceipt\\Summary.html";
                            string summaryhtmlString = File.ReadAllText(summaryToHTMLFile);
                            StringBuilder htmlSummaryReport = new StringBuilder(summaryhtmlString);
                            string SummaryFinalReport = "";
                            string respDateTime = resp[4];
                            string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                            string currentTime = BUFFERSENDtext.Substring(6, 2) + ":" + BUFFERSENDtext.Substring(8, 2) + ":" + BUFFERSENDtext.Substring(10, 2);

                            int j = 7;
                            int transactionsLength = int.Parse(resp[6]);
                            for (int i = 1; i <= transactionsLength; i++)
                            {
                                string date = resp[j + 1];
                                date = date.Substring(0, 2) + "-" + date.Substring(2, 2) + "-" + date.Substring(4, 2);
                                string time = resp[j + 6];
                                time = time.Substring(0, 2) + ":" + time.Substring(2, 2);
                                htmlSummaryReport.Replace("transactionType", resp[j]);
                                htmlSummaryReport.Replace("transactionDate", date);
                                htmlSummaryReport.Replace("transactionRRN", resp[j + 2]);
                                htmlSummaryReport.Replace("transactionAmount", (Convert.ToDecimal(resp[j + 3])).ToString());
                                htmlSummaryReport.Replace("Claim1", resp[j + 4]);
                                htmlSummaryReport.Replace("transactionState", resp[j + 5]);
                                htmlSummaryReport.Replace("transactionTime", time);
                                htmlSummaryReport.Replace("transactionPANNumber", resp[j + 7]);
                                htmlSummaryReport.Replace("authCode", resp[j + 8]);
                                htmlSummaryReport.Replace("transactionNumber", resp[j + 9]);
                                j = j + 10;
                                SummaryFinalReport += htmlSummaryReport.ToString();
                                htmlSummaryReport = new StringBuilder(summaryhtmlString);
                            }

                            builder.Replace("no_Transaction", SummaryFinalReport);
                            builder.Replace("currentTime", currentTime);
                            builder.Replace("currentDate", currentDate);
                            builder.Replace("terminalId", TerminalId.Substring(0, 8));
                            builder.Replace("CountDB", resp[j + 1]);
                            builder.Replace("AmountDB", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[j + 2]) / 100))));
                            builder.Replace("CountCR", resp[j + 3]);
                            builder.Replace("AmountCR", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[j + 4]) / 100))));
                            builder.Replace("AmountTotal", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[j + 5]) / 100))));
                            builder.Replace("merchant name", resp[j + 6]);
                            builder.Replace("merchant address", resp[j + 7]);
                            Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));

                            string printSummaryReportString = "Transaction transactionNumberHead\n" +
                                                                "----------------------------------------------------------\n" +
                                                                "Transaction Type" + "\t" + "\t" + "\t" + ": TransactionType1  \n" +
                                                                "Date" + "\t" + "\t" + "\t" + "\t" + ": Date1 \n" +
                                                                "RRN" + "\t" + "\t" + "\t" + "\t" + ": RRN1 \n" +
                                                                "Transaction Amount" + "\t" + "\t" + ": TransactionAmount1 \n" +
                                                                "Claim " + "\t" + "\t" + "\t" + "\t" + ": Claim1 \n" +
                                                                "State" + "\t" + "\t" + "\t" + "\t" + ": State1 \n" +
                                                                "Time" + "\t" + "\t" + "\t" + "\t" + ": Time1 \n" +
                                                                "PAN Number" + "\t" + "\t" + "\t" + ": PANNumber1 \n" +
                                                                "authCode" + "\t" + "\t" + "\t" + ": authCode1 \n" +
                                                                "transactionNumber" + "\t" + "\t" + ": transactionNumber1 \n" +
                                                                "----------------------------------------------------------\n";
                            StringBuilder htmlSummaryReport1 = new StringBuilder(printSummaryReportString);
                            string SummaryFinalReport1 = "";
                            int k = 7;
                            for (int a = 1; a <= transactionsLength; a++)
                            {
                                htmlSummaryReport1.Replace("transactionNumberHead", a.ToString());
                                htmlSummaryReport1.Replace("TransactionType1", resp[k]);
                                htmlSummaryReport1.Replace("Date1", resp[k + 1]);
                                htmlSummaryReport1.Replace("RRN1", resp[k + 2]);
                                htmlSummaryReport1.Replace("TransactionAmount1", resp[k + 3]);
                                htmlSummaryReport1.Replace("Claim1", resp[k + 4]);
                                htmlSummaryReport1.Replace("State1", resp[k + 5]);
                                htmlSummaryReport1.Replace("Time1", resp[k + 6]);
                                htmlSummaryReport1.Replace("PANNumber1", resp[k + 7]);
                                htmlSummaryReport1.Replace("authCode1", resp[k + 8]);
                                htmlSummaryReport1.Replace("transactionNumber1", resp[k + 9]);
                                k = k + 10;
                                SummaryFinalReport1 += htmlSummaryReport1.ToString();
                                htmlSummaryReport1 = new StringBuilder(printSummaryReportString);
                            }

                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                 "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                 "Transaction Requests Count" + "\t" + ": " + resp[5] + "\n" +
                                                 "Total Transactions Length" + "\t" + ": " + resp[6] + "\n" +
                                                 SummaryFinalReport1 +
                                                 "Topic" + "\t" + "\t" + "\t" + "\t" + ": " + resp[k] + "\n" +
                                                 "DB Count" + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                 "DB Amount" + "\t" + "\t" + "\t" + " : " + resp[k + 2] + "\n" +
                                                 "CR Count" + "\t" + "\t" + "\t" + ": " + resp[k + 3] + "\n" +
                                                 "CR Amount" + "\t" + "\t" + "\t" + ": " + resp[k + 4] + "\n" +
                                                 "Total Amount " + "\t" + "\t" + "\t" + ": " + resp[k + 5] + "\n" +
                                                 "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[k + 6] + "\n" +
                                                 "Merchant Address " + "\t" + "\t" + ": " + resp[k + 7] + "\n" +
                                                 "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[k + 8] + "\n" +
                                                 "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[k + 9] + "\n" +
                                                 "ECR Transaction Reference Number" + "\t" + ": " + resp[k + 10] + "\n" +
                                                 "Signature" + "\t" + "\t" + "\t" + ": " + resp[k + 11] + "\n";
                        }
                        else if (responseCommand == "23")
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                 "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                 "Previous Transaction Response" + "\t" + ": " + resp[5] + "\n" +
                                                 "Previous ECR Number" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                 "ECR Transaction Reference Number : " + resp[7] + "\n" +
                                                 "Signature Transaction" + "\t" + "\t" + ": " + resp[8] + "\n";
                        }
                        else if (responseCommand == "24")
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                   "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                   "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                   "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                   "ECR Transaction Reference Number  : " + resp[5] + "\n" +
                                                   "Signature" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n";
                        }
                        else if (responseCommand == "50")
                        {
                            BUFFERRECEIVEDtext =
                                                   "Response Code" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n";
                            // "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n";

                        }
                        else
                        {
                            BUFFERRECEIVEDtext = "Response Code" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message " + "\t" + ": " + resp[3] + "\n";
                            (sender as BackgroundWorker).ReportProgress(1);
                        }
                    }
                    else if (responseInt == 1)
                    {
                        messageShow("Problem connecting to Terminal");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                    else if (responseInt == 2)
                    {
                        messageShow("Problem pack method");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                    else if (responseInt == 3)
                    {
                        messageShow("Problem connecting to Terminal");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                    else if (responseInt == 4)
                    {
                        if (transactionType == "24")
                        {
                            messageShow("Terminal is busy");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            messageShow("Timeout Please try again");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                    }
                    (sender as BackgroundWorker).ReportProgress(1);
                }
                catch (Exception ex)
                {
                    BUFFERRECEIVEDtext = response;
                    messageShow(ex.Message);
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
        }
        void messageShow(string message)
        {
            msgShow = message;
        }

        void worker_ProgressSend(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage.ToString() == "0")
            {
                ProgBarL.Content = "Sending...";
                ProgBarL.Visibility = Visibility.Visible;
                ProgBar.Visibility = Visibility.Visible;
                ProgBar.IsIndeterminate = true;
            }
            if (e.ProgressPercentage.ToString() == "1")
            {
                ProgBar.IsIndeterminate = false;
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                BUFFERRECEIVED.Text = BUFFERRECEIVEDtext;
                enter.IsEnabled = true;
            }
            if (e.ProgressPercentage.ToString() == "2")
            {
                ProgBar.IsIndeterminate = false;
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                enter.IsEnabled = true;
                MessageBox.Show(msgShow);
            }
            if (e.ProgressPercentage.ToString() == "3")
            {
                BUFFERSEND.Text = BUFFERSENDtext;
            }
            if (e.ProgressPercentage.ToString() == "4")
            {
                if (ECRrefNum)
                {
                    chkEcrref.IsChecked = false;
                    txtEcrref.IsEnabled = false;
                }
                txtEcrref.Text = ECRREFtext;
            }
        }
        private void DisConnect(object sender, RoutedEventArgs e)
        {
            try
            {
                InteroperableService obj = new InteroperableService();
                if (tcpComSelect.ToString() == "TCP/IP")
                {
                    resultDisconnect = obj.doCOMDisconnection();
                    statusSerialConn = false;
                    dispatcherTimer.Tick -= new EventHandler(dispatcherTimer_Tick);
                    dispatcherTimer.Stop();
                }
                else
                {
                    resultDisconnect = obj.doTCPIPDisconnection();
                }
                if (resultDisconnect == 0)
                {
                    MessageBox.Show("Disconnected Successfully");
                    ConnectL.Content = "Not Connected";
                    ConnectImg.Source = null;
                    txtIpAddress.Text = "";
                    txtPort.Text = "";
                    BUFFERSEND.Text = "";
                    BUFFERRECEIVED.Text = "";
                    CASHBACKT.Text = "";
                    AMOUNTT.Text = "";
                    RRNT.Text = "";
                    DateT.Text = "";
                    OACT.Text = "";
                    PORTCOM.Text = "";
                    chkfpCapture.IsChecked = false;
                    chkfpCapture.IsEnabled = false;
                    TRANSTYPE.IsEnabled = false;
                    TRANSTYPEL.IsEnabled = false;
                    CASHBACKL.IsEnabled = false;
                    AMOUNTL.IsEnabled = false;
                    AMOUNTT.IsEnabled = false;
                    RRNL.IsEnabled = false;
                    RRNT.IsEnabled = false;
                    DateL.IsEnabled = false;
                    DateT.IsEnabled = false;
                    OACL.IsEnabled = false;
                    OACT.IsEnabled = false;
                    chkEtp.IsEnabled = false;
                    rdbCom.IsEnabled = true;
                    rdbTcp.IsEnabled = true;
                    txtPort.IsEnabled = true;
                    txtIpAddress.IsEnabled = true;
                    SetSetting.IsEnabled = false;
                    chkEcrref.IsEnabled = false;
                    txtEcrref.IsEnabled = false;
                    CashRegisterNumberT.IsEnabled = false;
                    myBrowser.Navigate((Uri)null);
                    btnDisConnect.IsEnabled = false;
                    btnConnect.IsEnabled = true;
                    enter.IsEnabled = false;
                    TRANSTYPE.SelectedIndex = 0;
                    AdminTransaction.SelectedIndex = 0;
                    AdminTransactionL.IsEnabled = false;
                    AdminTransaction.IsEnabled = true;
                    grpTcpSetting.IsEnabled = false;
                    rdbCom.IsChecked = false;
                    rdbTcp.IsChecked = false;
                    BAUDSPEED.SelectedIndex = 0;
                    PARITY.SelectedIndex = 0;
                    DATABITS.SelectedIndex = 0;
                    STOPBITS.SelectedIndex = 0;
                    result = 1;
                    this.ProgBar.IsIndeterminate = false;
                    ProgBar.Visibility = Visibility.Hidden;
                    ProgBarL.Visibility = Visibility.Hidden;
                    txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                    txtIpAddress.Focus();
                }
                else
                {
                    MessageBox.Show("Try Again");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void PORTTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (txtPort.Text.Length > 4)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void AmountValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (AMOUNTT.Text.Length > 12)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void CashbackValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (CASHBACKT.Text.Length > 12)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void RRNValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (RRNT.Text.Length > 11)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void OACValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (OACT.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void ECRRefValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (txtEcrref.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void prvsECRNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (prvsECRNumberT.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }

        private void CashRegisterNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (CashRegisterNumberT.Text.Length > 7)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void VendorIDNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (VendorIDT.Text.Length > 1)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void TRSMIDNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (TRSMIDT.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void VendorKeyIndexNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (VendorKeyIndexT.Text.Length > 1)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void VendorTerminaltypeNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (VendorTerminaltypeT.Text.Length > 1)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void SAMAKeyIndexNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (SAMAKeyIndexT.Text.Length > 1)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void BillerIDNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (BillerIDT.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void BillNumberNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (BillNumberT.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");

            if (txtIpAddress.Text.Length > 14)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }

        private void TRANSTYPESelection(object sender, SelectionChangedEventArgs e)
        {
            string type = (e.AddedItems[0] as ComboBoxItem).Content as string;
            if (type == "Register")
            {
                transaction = "Register";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                chkfpCapture.IsChecked = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                chkfpCapture.IsEnabled = false;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = true;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                CashRegisterNumberL.Focus();
            }
            if (type == "Start Session")
            {
                transaction = "Start Session";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTL.Focus();
            }
            if (type == "End Session")
            {
                transaction = "End Session";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTL.Focus();
            }

            if (type == "Purchase")
            {
                transaction = "Purchase";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                countAmount = 0;
                amountDefaultVal();
                chkfpCapture.IsEnabled = false;
                chkfpCapture.IsChecked = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = true;
                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();

            }
            if (type == "Purchase Cashback")
            {
                transaction = "Purchase Cashback";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                cbDefaultVal();
                countAmount = 0;
                countCB = 0;
                chkfpCapture.IsEnabled = false;
                chkfpCapture.IsChecked = false;
                CASHBACKT.IsEnabled = true;
                CASHBACKL.IsEnabled = true;
                AMOUNTL.IsEnabled = true;
                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (type == "Refund")
            {
                transaction = "Refund";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                countAmount = 0;
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = true;
                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = true;
                RRNT.IsEnabled = true;
                DateL.IsEnabled = true;
                DateT.IsEnabled = true;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Pre Authorization")
            {
                transaction = "Pre Authorization";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                countAmount = 0;
                chkfpCapture.IsEnabled = false;
                chkfpCapture.IsChecked = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = true;
                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (type == "Pre Auth Extension")
            {
                transaction = "Pre Auth Extension";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                countAmount = 0;
                chkfpCapture.IsEnabled = false;
                chkfpCapture.IsChecked = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = true;
                RRNT.IsEnabled = true;
                DateL.IsEnabled = true;
                DateT.IsEnabled = true;
                OACL.IsEnabled = true;
                OACT.IsEnabled = true;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Pre Auth Void")
            {
                transaction = "Pre Auth Void";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                countAmount = 0;
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = true;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = true;
                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = true;
                RRNT.IsEnabled = true;
                DateL.IsEnabled = true;
                DateT.IsEnabled = true;
                OACL.IsEnabled = true;
                OACT.IsEnabled = true;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Purchase Advice")
            {
                transaction = "Purchase Advice";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = true;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = true;
                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = true;
                RRNT.IsEnabled = true;
                DateL.IsEnabled = true;
                DateT.IsEnabled = true;
                OACL.IsEnabled = true;
                OACT.IsEnabled = true;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Cash Advance")
            {
                transaction = "Cash Advance";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                countAmount = 0;
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = true;
                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (type == "Reversal")
            {
                transaction = "Reversal";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                countAmount = 0;
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = true;
                RRNT.IsEnabled = true;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Reconciliation")
            {
                transaction = "Reconciliation";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                countAmount = 0;
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                txtEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTL.Focus();
            }
            if (type == "Bill Payment")
            {
                transaction = "Bill Payment";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = true;
                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                BillerIDL.IsEnabled = true;
                BillerIDT.IsEnabled = true;
                BillNumberL.IsEnabled = true;
                BillNumberT.IsEnabled = true;
                myBrowser.Navigate((Uri)null);
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (type == "Repeat")
            {
                transaction = "Repeat";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = true;
                myBrowser.Navigate((Uri)null);
                int refNumber = int.Parse(txtEcrref.Text) - 1;
                prvsECRNumberT.Text = refNumber.ToString("D6");
                AMOUNTL.Focus();
            }
            if (type == "Parameter Download")
            {
                transaction = "Parameter Download";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = true;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTL.Focus();
            }
            if (type == "Set Settings")
            {
                transaction = "Set Settings";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                SetSetting.IsEnabled = true;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTL.Focus();
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (type == "Get Settings")
            {
                transaction = "Get Settings";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTL.Focus();
            }
            if (type == "Running Total")
            {
                transaction = "Running Total";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTL.Focus();
            }
            if (type == "Print Summary Report")
            {
                transaction = "Print Summary Report";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTL.Focus();
            }
            if (type == "Check Status")
            {
                transaction = "Check Status";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                chkfpCapture.IsChecked = false;
                chkfpCapture.IsEnabled = false;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;
                AMOUNTL.IsEnabled = false;
                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTL.Focus();
            }
        }
        private void Clear(object sender, RoutedEventArgs e)
        {
            countAmount = 0;
            countCB = 0;
            BUFFERSEND.Text = "";
            BUFFERRECEIVED.Text = "";
            CASHBACKT.Text = "";
            AMOUNTT.Text = "";
            RRNT.Text = "";
            DateT.Text = "";
            OACT.Text = "";
            VendorIDT.Text = "";
            VendorTerminaltypeT.Text = "";
            TRSMIDT.Text = "";
            VendorKeyIndexT.Text = "";
            SAMAKeyIndexT.Text = "";
            BillerIDT.Text = "";
            BillNumberT.Text = "";
            myBrowser.Navigate((Uri)null);
            if (transaction == "Bill Payment" || transaction == "Purchase" || transaction == "Refund" || transaction == "Pre Authorization" || transaction == "Pre Auth Void" || transaction == "Purchase Advice" || transaction == "Cash Advance")
            {
                amountDefaultVal();
            }
            else if (transaction == "Purchase Cashback")
            {
                amountDefaultVal();
                cbDefaultVal();
            }

        }


        private void ONE(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.01";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "0." + ch[2] + "1";
                }
                else if (countAmount == 3)
                {
                    AMOUNTT.Text = ch[1] + "." + ch[2] + "1";
                }
                else
                {
                    cleanAmount = cleanAmount + "1";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + 1;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.01";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "1";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "1";
                }
                else
                {
                    cleanAmount = cleanAmount + "1";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 1;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 1;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 1;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 1;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 1;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 1;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 1;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 1;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 1;
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 1;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 1;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 1;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 1;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 1;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 1;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }

        }

        private void AmountTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "AmountText";
        }
        private void CashbackTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "CashbackText";
        }
        private void RrnTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "RrnText";
        }
        private void OacTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "OacTextChanged";
        }
        private void IPTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "IPText";
        }
        private void PortTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "PortText";
        }
        private void ECRRefTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "ECRRefText";
        }

        private void CashRegisterNumberTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "CashRegisterNumberText";
        }
        private void VendorIDTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "VendorIDText";
        }

        private void TRSMIDTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "TRSMIDText";
        }
        private void SAMAKeyIndexTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "SAMAKeyIndexText";
        }
        private void VendorKeyIndexTectChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "VendorKeyIndexText";
        }
        private void VendorTerminaltypeTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "VendorTerminaltypeText";
        }
        private void BillPaymentAmountTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "BillPaymentAmountText";
        }
        private void BillerIDTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "BillerIDText";
        }
        private void BillNumberTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "BillNumberText";
        }
        private void prvsECRNumberTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "prvsECRNumberText";
        }


        private void TWO(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.02";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "0." + ch[2] + "2";
                }
                else if (countAmount == 3)
                {
                    AMOUNTT.Text = ch[1] + "." + ch[2] + "2";
                }
                else
                {
                    cleanAmount = cleanAmount + "2";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + 2;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.02";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "2";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "2";
                }
                else
                {
                    cleanAmount = cleanAmount + "2";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 2;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 2;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 2;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 2;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 2;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 2;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 2;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 2;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 2;
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 2;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 2;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 2;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 2;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 2;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 2;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void THREE(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.03";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "0." + ch[2] + "3";
                }
                else if (countAmount == 3)
                {
                    AMOUNTT.Text = ch[1] + "." + ch[2] + "3";
                }
                else
                {
                    cleanAmount = cleanAmount + "3";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + 3;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.03";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "3";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "3";
                }
                else
                {
                    cleanAmount = cleanAmount + "3";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 3;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 3;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 3;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 3;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 3;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 3;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 3;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 3;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 3;
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 3;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 3;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 3;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 3;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 3;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 3;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void FOUR(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.04";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "0." + ch[2] + "4";
                }
                else if (countAmount == 3)
                {
                    AMOUNTT.Text = ch[1] + "." + ch[2] + "4";
                }
                else
                {
                    cleanAmount = cleanAmount + "4";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + 4;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.04";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "4";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "4";
                }
                else
                {
                    cleanAmount = cleanAmount + "4";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 4;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 4;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 4;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 4;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 4;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 4;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 4;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 4;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 4;
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 4;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 4;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 4;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 4;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 4;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 4;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void FIVE(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.05";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "0." + ch[2] + "5";
                }
                else if (countAmount == 3)
                {
                    AMOUNTT.Text = ch[1] + "." + ch[2] + "5";
                }
                else
                {
                    cleanAmount = cleanAmount + "5";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + 5;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.05";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "5";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "5";
                }
                else
                {
                    cleanAmount = cleanAmount + "5";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 5;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 5;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 5;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 5;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 5;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 5;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 5;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 5;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 5;
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 5;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 5;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 5;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 5;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 5;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 5;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void SIX(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.06";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "0." + ch[2] + "6";
                }
                else if (countAmount == 3)
                {
                    AMOUNTT.Text = ch[1] + "." + ch[2] + "6";
                }
                else
                {
                    cleanAmount = cleanAmount + "6";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + 6;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.06";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "6";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "6";
                }
                else
                {
                    cleanAmount = cleanAmount + "6";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 6;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 6;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 6;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 6;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 6;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 6;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 6;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 6;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 6;
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 6;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 6;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 6;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 6;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 6;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 6;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void SEVEN(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.07";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "0." + ch[2] + "7";
                }
                else if (countAmount == 3)
                {
                    AMOUNTT.Text = ch[1] + "." + ch[2] + "7";
                }
                else
                {
                    cleanAmount = cleanAmount + "7";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + 7;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.07";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "7";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "7";
                }
                else
                {
                    cleanAmount = cleanAmount + "7";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 7;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 7;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 7;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 7;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 7;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 7;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 7;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 7;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 7;
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 7;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 7;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 7;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 7;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 7;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 7;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void EIGHT(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.08";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "0." + ch[2] + "8";
                }
                else if (countAmount == 3)
                {
                    AMOUNTT.Text = ch[1] + "." + ch[2] + "8";
                }
                else
                {
                    cleanAmount = cleanAmount + "8";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + 8;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.08";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "8";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "8";
                }
                else
                {
                    cleanAmount = cleanAmount + "8";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 8;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 8;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 8;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 8;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 8;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 8;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 8;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 8;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 8;
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 8;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 8;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 8;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 8;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 8;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 8;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void NINE(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(">>>>>>>>>>>>>>>>>>NINE" + countAmount);
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.09";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "0." + ch[2] + "9";
                    Console.WriteLine(">>>>>>>>>>>>>>>>>>3" + "0." + ch[2] + "9");
                }
                else if (countAmount == 3)
                {

                    AMOUNTT.Text = ch[1] + "." + ch[2] + "9";
                    Console.WriteLine(">>>>>>>>>>>>>>>>>>3" + ch[1] + "." + ch[2] + "9");
                }
                else
                {
                    cleanAmount = cleanAmount + "9";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + 9;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.09";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "9";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "9";
                }
                else
                {
                    cleanAmount = cleanAmount + "9";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 9;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 9;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 9;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 9;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 9;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 9;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 9;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 9;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 9;
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 9;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 9;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 9;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 9;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 9;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 9;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }

        private void ZERO(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.00";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "0." + ch[2] + "0";
                }
                else if (countAmount == 3)
                {
                    AMOUNTT.Text = ch[1] + "." + ch[2] + "0";
                }
                else
                {
                    cleanAmount = cleanAmount + "0";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + 0;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.00";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "0";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "0";
                }
                else
                {
                    cleanAmount = cleanAmount + "0";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 0;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 0;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 0;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 0;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 0;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 0;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 0;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 0;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 0;
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 0;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 0;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 0;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 0;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 0;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 0;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void DZERO(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length < 11)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                if (countAmount == 1)
                {
                    AMOUNTT.Text = "0.00";
                }
                else if (countAmount == 2)
                {
                    AMOUNTT.Text = "00." + ch[2] + "0";
                }
                else if (countAmount == 3)
                {
                    AMOUNTT.Text = ch[1] + "." + ch[2] + "0";
                }
                else
                {
                    cleanAmount = cleanAmount + "00";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    AMOUNTT.Text = crtFormat;
                }
                //AMOUNTT.Text = AMOUNTT.Text + "00";
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 11)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.00";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "00." + ch[2] + "0";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "0";
                }
                else
                {
                    cleanAmount = cleanAmount + "00";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + "00";
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 11)
            {
                RRNT.Text = RRNT.Text + "00";
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + "00";
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + "00";
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 5)
            {
                txtEcrref.Text = txtEcrref.Text + "00";
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 7)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + "00";
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 0)
            {
                VendorIDT.Text = VendorIDT.Text + "00";
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 2)
            {
                TRSMIDT.Text = TRSMIDT.Text + "00";
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 0)
            {
                SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + "00";
                SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                SAMAKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 0)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + "00";
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 0)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + "00";
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 5)
            {
                BillerIDT.Text = BillerIDT.Text + "00";
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 5)
            {
                BillNumberT.Text = BillNumberT.Text + "00";
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 5)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + "00";
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 5)
            {
                OACT.Text = OACT.Text + "00";
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void DELETE(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.ToString() != "0.00")
            {
                Console.WriteLine(">>>>>>>>>>>>>>>>>>DELETE bf " + countAmount);
                countAmount = countAmount - 1;
                Console.WriteLine(">>>>>>>>>>>>>>>>>>DELETE af " + countAmount);
                if (AMOUNTT.Text.Length != 0)
                {
                    string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                    cleanAmount = cleanAmount.Remove(cleanAmount.Length - 1, 1);
                    char[] ch = cleanAmount.ToArray();//000
                    string crtFormat = string.Empty;
                    Console.WriteLine(">>>>>>>>>>>>>>>>>007  " + cleanAmount);
                    if (countAmount < 1)
                    {
                        Console.WriteLine(">>>>>>>>>>>>>>>>>1  " + countAmount);
                        AMOUNTT.Text = "0.00";
                    }
                    else if (countAmount == 1)
                    {
                        Console.WriteLine(">>>>>>>>>>>>>>>>>2  " + countAmount);
                        AMOUNTT.Text = "0.0" + ch[1];
                    }
                    else if (countAmount == 2)
                    {
                        Console.WriteLine(">>>>>>>>>>>>>>>>3  " + countAmount);
                        AMOUNTT.Text = "0." + ch[0] + ch[1];
                    }
                    else if (countAmount == 3)
                    {
                        AMOUNTT.Text = ch[0] + "." + ch[1] + ch[2];
                    }
                    else
                    {
                        for (int i = 0; i < cleanAmount.Length; i++)
                        {
                            if (cleanAmount.Length - 2 == i)
                            {
                                crtFormat = crtFormat + "." + cleanAmount[i];
                            }
                            else
                            {
                                crtFormat = crtFormat + cleanAmount[i];
                            }
                            AMOUNTT.Text = crtFormat;
                        }
                        AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                        AMOUNTT.Focus();
                    }
                }
            }
            else
            {
                countAmount = 0;
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.ToString() != "0.00")
            {
                --countCB;
                if (CASHBACKT.Text.Length != 0)
                {
                    string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                    cleanAmount = cleanAmount.Remove(cleanAmount.Length - 1, 1);
                    char[] ch = cleanAmount.ToArray();//000
                    string crtFormat = string.Empty;
                    if (countCB == 1)
                    {
                        CASHBACKT.Text = "0.00";
                    }
                    else if (countCB == 2)
                    {
                        CASHBACKT.Text = "0.0" + ch[1];
                    }
                    else if (countCB == 3)
                    {
                        CASHBACKT.Text = "0" + "." + ch[1] + ch[2];
                    }
                    else
                    {
                        for (int i = 0; i < cleanAmount.Length; i++)
                        {
                            if (cleanAmount.Length - 2 == i)
                            {
                                crtFormat = crtFormat + "." + cleanAmount[i];
                            }
                            else
                            {
                                crtFormat = crtFormat + cleanAmount[i];
                            }
                            CASHBACKT.Text = crtFormat;
                        }
                        CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                        CASHBACKT.Focus();
                    }

                }
            }
            else
            {
                countCB = 0;
            }
            if (textFocus.ToString() == "RrnText")
            {
                if (RRNT.Text.Length != 0)
                {
                    RRNT.Text = RRNT.Text.Remove(RRNT.Text.Length - 1, 1);
                    RRNT.Select(RRNT.Text.Length, 0);
                    RRNT.Focus();
                }
            }
            if (textFocus.ToString() == "IPText")
            {
                if (txtIpAddress.Text.Length != 0)
                {
                    txtIpAddress.Text = txtIpAddress.Text.Remove(txtIpAddress.Text.Length - 1, 1);
                    txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                    txtIpAddress.Focus();
                }
            }
            if (textFocus.ToString() == "PortText")
            {
                if (txtPort.Text.Length != 0)
                {
                    txtPort.Text = txtPort.Text.Remove(txtPort.Text.Length - 1, 1);
                    txtPort.Select(txtPort.Text.Length, 0);
                    txtPort.Focus();
                }
            }
            if (textFocus.ToString() == "ECRRefText")
            {
                if (txtEcrref.Text.Length != 0)
                {
                    txtEcrref.Text = txtEcrref.Text.Remove(txtEcrref.Text.Length - 1, 1);
                    txtEcrref.Select(txtEcrref.Text.Length, 0);
                    txtEcrref.Focus();
                }
            }
            if (textFocus.ToString() == "CashRegisterNumberText")
            {
                if (CashRegisterNumberT.Text.Length != 0)
                {
                    CashRegisterNumberT.Text = CashRegisterNumberT.Text.Remove(CashRegisterNumberT.Text.Length - 1, 1);
                    CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                    CashRegisterNumberT.Focus();
                }
            }
            if (textFocus.ToString() == "TRSMIDText")
            {
                if (TRSMIDT.Text.Length != 0)
                {
                    TRSMIDT.Text = TRSMIDT.Text.Remove(TRSMIDT.Text.Length - 1, 1);
                    TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                    TRSMIDT.Focus();
                }
            }
            if (textFocus.ToString() == "SAMAKeyIndexText")
            {
                if (SAMAKeyIndexT.Text.Length != 0)
                {
                    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text.Remove(SAMAKeyIndexT.Text.Length - 1, 1);
                    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
                    SAMAKeyIndexT.Focus();
                }
            }
            if (textFocus.ToString() == "VendorKeyIndexText")
            {
                if (VendorKeyIndexT.Text.Length != 0)
                {
                    VendorKeyIndexT.Text = VendorKeyIndexT.Text.Remove(VendorKeyIndexT.Text.Length - 1, 1);
                    VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                    VendorKeyIndexT.Focus();
                }
            }
            if (textFocus.ToString() == "VendorTerminaltypeText")
            {
                if (VendorTerminaltypeT.Text.Length != 0)
                {
                    VendorTerminaltypeT.Text = VendorTerminaltypeT.Text.Remove(VendorTerminaltypeT.Text.Length - 1, 1);
                    VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                    VendorTerminaltypeT.Focus();
                }
            }
            if (textFocus.ToString() == "BillerIDText")
            {
                if (BillerIDT.Text.Length != 0)
                {
                    BillerIDT.Text = BillerIDT.Text.Remove(BillerIDT.Text.Length - 1, 1);
                    BillerIDT.Select(BillerIDT.Text.Length, 0);
                    BillerIDT.Focus();
                }
            }
            if (textFocus.ToString() == "BillNumberText")
            {
                if (BillNumberT.Text.Length != 0)
                {
                    BillNumberT.Text = BillNumberT.Text.Remove(BillNumberT.Text.Length - 1, 1);
                    BillNumberT.Select(BillNumberT.Text.Length, 0);
                    BillNumberT.Focus();
                }
            }
            if (textFocus.ToString() == "prvsECRNumberText")
            {
                if (prvsECRNumberT.Text.Length != 0)
                {
                    prvsECRNumberT.Text = prvsECRNumberT.Text.Remove(prvsECRNumberT.Text.Length - 1, 1);
                    prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                    prvsECRNumberT.Focus();
                }
            }
            if (textFocus.ToString() == "OacTextChanged")
            {
                if (OACT.Text.Length != 0)
                {
                    OACT.Text = OACT.Text.Remove(OACT.Text.Length - 1, 1);
                    OACT.Select(OACT.Text.Length, 0);
                    OACT.Focus();
                }
            }
            if (textFocus.ToString() == "VendorIDText")
            {
                if (VendorIDT.Text.Length != 0)
                {
                    VendorIDT.Text = VendorIDT.Text.Remove(VendorIDT.Text.Length - 1, 1);
                    VendorIDT.Select(VendorIDT.Text.Length, 0);
                    VendorIDT.Focus();
                }
            }
        }
        private void clearField()
        {
            BUFFERSEND.Text = "";
            BUFFERRECEIVED.Text = "";
            CASHBACKT.Text = "";
            AMOUNTT.Text = "";
            RRNT.Text = "";
            DateT.Text = "";
            OACT.Text = "";
            VendorIDT.Text = "";
            VendorTerminaltypeT.Text = "";
            TRSMIDT.Text = "";
            VendorKeyIndexT.Text = "";
            SAMAKeyIndexT.Text = "";
            BillerIDT.Text = "";
            BillNumberT.Text = "";
            prvsECRNumberT.Text = "";
        }
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            ECRrefNum = chkEcrref.IsChecked ?? false;
            if (ECRrefNum)
            {
                txtEcrref.IsEnabled = true;
            }
            else txtEcrref.IsEnabled = false;
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

        public string ConvertHexArabic(string hexString)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string hs = hexString.Substring(i, 2);
                sb.Append(Convert.ToString(Convert.ToChar(Int32.Parse(hexString.Substring(i, 2), System.Globalization.NumberStyles.HexNumber))));
            }

            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding iso = Encoding.GetEncoding("ISO-8859-6");
            Encoding utf16 = Encoding.Unicode;
            string msg = iso.GetString(utf16.GetBytes(sb.ToString()));

            return msg;
        }
        public string CheckingArabic(string command)
        {
            string arabic = "";
            switch (command)
            {
                case "mada":
                    arabic = "مدى";
                    break;
                case "ELECTRON":
                    arabic = "فيزا";
                    break;
                case "MAESTRO":
                    arabic = "مايسترو";
                    break;
                case "AMEX":
                    arabic = "امريكان اكسبرس";
                    break;
                case "AMERICAN EXPRESS":
                    arabic = "امريكان اكسبرس";
                    break;
                case "MASTER":
                    arabic = "ماستر كارد";
                    break;
                case "MASTERCARD":
                    arabic = "ماستر كارد";
                    break;
                case "VISA":
                    arabic = "فيزا";
                    break;
                case "GCCNET":
                    arabic = "الشبكة الخليجية";
                    break;
                case "JCB":
                    arabic = "ج س ب";
                    break;
                case "DISCOVER":
                    arabic = "ديسكفر";
                    break;
                case "SAR":
                    arabic = "ريال";
                    break;
                case "MADA":
                    arabic = "مدى";
                    break;
                case "APPROVED":
                    arabic = "العملية مقبولة";
                    break;
                case "DECLINED":
                    arabic = "العملية مرفوضه";
                    break;
                case "ACCEPTED":
                    arabic = "مستلمة";
                    break;
                case "NOT ACCEPTED":
                    arabic = "غير مستلمة";
                    break;
                default:
                    Console.WriteLine("Invalid transactionType");
                    arabic = "";
                    break;
            }
            return arabic;
        }

        public string CheckingArabicPin(string command)
        {
            string arabic = "";
            switch (command)
            {
                case "CARDHOLDER VERIFIED BY SIGNATURE":
                    arabic = "تم التحقق بتوقيع العميل";
                    break;
                case "CARDHOLDER PIN VERIFIED":
                    arabic = "تم التحقق من الرقم السري للعميل";
                    break;
                case "DEVICE OWNER IDENTITY VERIFIED":
                    arabic = "تم التحقق من هوية حامل الجهاز";
                    break;
                case "NO VERIFICATION REQUIRED":
                    arabic = "لا يتطلب التحقق";
                    break;
                case "INCORRECT PIN":
                    arabic = "ارقم التعريف الشخصي غير صحيح";
                    break;
                default:
                    Console.WriteLine("Invalid transactionType");
                    arabic = "";
                    break;
            }
            return arabic;
        }
        public static string numToArabicConverter(string num)
        {
            string arabic = string.Empty;
            char[] numChar = num.ToCharArray();
            char[] arabicChar = { '۰', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩' };
            for (int i = 0; i < numChar.Length; i++)
            {
                char temp = numChar[i];
                string temp1 = temp.ToString();
                if (temp1 == ".")
                {
                    arabic += ".";
                }
                for (int j = 0; j < arabicChar.Length; j++)
                {
                    string tempj = j.ToString();
                    if (tempj == temp1)
                    {
                        arabic += arabicChar[j];
                    }
                }
            }
            return arabic;
        }

        private void amountDefaultVal()
        {
            AMOUNTT.Text = "0.00";
        }
        private void cbDefaultVal()
        {
            CASHBACKT.Text = "0.00";
        }

        private void AMOUNTT_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                bool checkNum = true;
                //try
                //{
                string numCheck = e.Key.ToString();//Num
                string numCheckKey = numCheck.Substring(0, 1);
                if (numCheckKey == "D")
                {
                    //string numCheck = e.Key.ToString();//Num
                    numCheck = "Num";
                }
                else
                {
                    //string numCheck = e.Key.ToString();//Num
                    numCheck = numCheck.Substring(0, 3);
                }
                // Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>1   " + numCheck);
                //}
                //catch (Exception)
                //{
                //    MessageBox.Show("Please enter number");
                //    checkNum = false;
                //}
                if (checkNum)
                {
                    string amt = AMOUNTT.Text.ToString();
                    if (amt == "0.00")
                    {
                        countAmount = 0;
                    }
                    string cleanAmount = amt.Replace(".", string.Empty);
                    char[] ch = cleanAmount.ToArray();//000
                    countAmount++;
                    string crtFormat = string.Empty;
                    //Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>2   " + amt);
                    //Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>3"+ countAmount);
                    if (countAmount < 13)
                    {
                        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>3   " + countAmount);
                        if (countAmount == 1)
                        {
                            AMOUNTT.Text = "0." + ch[2];
                        }
                        else if (countAmount == 2)
                        {
                            AMOUNTT.Text = "0." + ch[2];
                        }
                        else if (countAmount == 3)
                        {
                            AMOUNTT.Text = ch[1] + "." + ch[2];
                        }
                        else
                        {
                            //cleanAmount = cleanAmount + "2";
                            // if (cleanAmount.Length <= 13)
                            // {

                            for (int i = 0; i < cleanAmount.Length; i++)
                            {
                                if (cleanAmount.Length - 1 == i)
                                {
                                    crtFormat = crtFormat + "." + cleanAmount[i];
                                }
                                else
                                {
                                    crtFormat = crtFormat + cleanAmount[i];
                                }
                            }
                            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>crtFormat   " + crtFormat);
                            AMOUNTT.Text = crtFormat;
                        }
                        // }
                    }
                    else
                    {
                        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>crtFormatmoshayi   " + amt);
                        AMOUNTT.Text = amt;
                        countAmount--;
                    }

                    AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                    AMOUNTT.Focus();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Please enter number");
                //checkNum = false;
            }

        }

        private void CASHBACKT_KeyDown(object sender,KeyEventArgs e)
        {
            try
            {
                bool checkNum = true;
                string numCheck = e.Key.ToString();//Num
                string numCheckKey = numCheck.Substring(0, 1);
                if (numCheckKey == "D")
                {
                    numCheck = "Num";
                }
                else
                {
                    numCheck = numCheck.Substring(0, 3);
                }
                if (checkNum)
                {
                    string cashbackAmmt = CASHBACKT.Text.ToString();
                    if (cashbackAmmt == "0.00")
                    {
                        countCB = 0;
                    }
                    string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                    char[] ch = cleanAmount.ToArray();//000
                    countCB++;
                    string crtFormat = string.Empty;
                    if (countCB < 13)
                    {
                        if (countCB == 1)
                        {
                            CASHBACKT.Text = "0.0";
                        }
                        else if (countCB == 2)
                        {
                            CASHBACKT.Text = "0." + ch[2];
                        }
                        else if (countCB == 3)
                        {
                            CASHBACKT.Text = ch[1] + "." + ch[2];
                        }
                        else
                        {

                            for (int i = 0; i < cleanAmount.Length; i++)
                            {
                                if (cleanAmount.Length - 1 == i)
                                {
                                    crtFormat = crtFormat + "." + cleanAmount[i];
                                }
                                else
                                {
                                    crtFormat = crtFormat + cleanAmount[i];
                                }
                            }
                            CASHBACKT.Text = crtFormat;
                        }
                    }
                    else
                    {
                        CASHBACKT.Text = cashbackAmmt;
                        countCB--;
                    }

                    CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                    CASHBACKT.Focus();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Please enter number");
                //checkNum = false;
            }
        }

        private void AMOUNTT_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                string backKey = e.Key.ToString();
                if (backKey == "Back")
                {
                    string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                    char[] ch = cleanAmount.ToArray();//000
                    countAmount--;
                    string crtFormat = string.Empty;
                    if (countAmount < 1)
                    {
                        AMOUNTT.Text = "0." + "000";
                        countAmount = 1;
                    }
                    else if (countAmount == 1)
                    {
                        AMOUNTT.Text = "0." + "0" + ch[1] + "0";
                    }
                    else if (countAmount == 2)//
                    {
                        AMOUNTT.Text = "0." + ch[0] + ch[1] + "0";
                    }
                    else
                    {
                        //cleanAmount = cleanAmount + "2";
                        for (int i = 0; i < cleanAmount.Length; i++)
                        {
                            if (cleanAmount.Length - 3 == i)
                            {
                                crtFormat = crtFormat + "." + cleanAmount[i];
                            }
                            else
                            {
                                crtFormat = crtFormat + cleanAmount[i];
                            }
                        }
                        AMOUNTT.Text = crtFormat;
                    }
                    AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                    AMOUNTT.Focus();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Amount field empty");
                //checkNum = false;
            }
        }

        private void CASHBACKT_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                string backKey = e.Key.ToString();
                if (backKey == "Back")
                {
                    string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                    char[] ch = cleanAmount.ToArray();//000
                    countCB--;
                    string crtFormat = string.Empty;
                    if (countCB < 1)
                    {
                        CASHBACKT.Text = "0." + "000";
                        countCB = 1;
                    }
                    else if (countCB == 1)
                    {
                        CASHBACKT.Text = "0." + "0" + ch[1] + "0";
                    }
                    else if (countCB == 2)//
                    {
                        CASHBACKT.Text = "0." + ch[0] + ch[1] + "0";
                    }
                    else
                    {
                        //cleanAmount = cleanAmount + "2";
                        for (int i = 0; i < cleanAmount.Length; i++)
                        {
                            if (cleanAmount.Length - 3 == i)
                            {
                                crtFormat = crtFormat + "." + cleanAmount[i];
                            }
                            else
                            {
                                crtFormat = crtFormat + cleanAmount[i];
                            }
                        }
                        CASHBACKT.Text = crtFormat;
                    }
                    CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                    CASHBACKT.Focus();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Cashback field empty");
                //checkNum = false;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

           
        }
        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            LogLevel.IsEnabled = true;
            NoDay.IsEnabled = true;
            broswing.IsEnabled = true;
            Filepath.IsEnabled = false;
        }

        private void isEnabledlog_Unchecked_1(object sender, RoutedEventArgs e)
        {
            broswing.IsEnabled = false;
            NoDay.IsEnabled = false;
            LogLevel.IsEnabled = false;
            Filepath.IsEnabled = false;
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select a folder"
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string selectedFolderPath = dialog.FileName;
                Filepath.Text = selectedFolderPath;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string loglevelval = LogLevel.Text;
            switch (loglevelval.ToLower()) // Convert input to lowercase for case-insensitivity
            {
                case "error":
                    loglevel = "1";
                    break;
                case "debug":
                    loglevel = "4";
                    break;
                case "information":
                    loglevel = "3";
                    break;
                default:
                    break;
            }
            Dispatcher.Invoke(() =>
            {
                islogAllowed = isEnabledlog.IsChecked == true;

            });
            SkybandECR.Properties.Settings.Default.LogOption = isEnabledlog.IsChecked.ToString();
            SkybandECR.Properties.Settings.Default.Path = Filepath.Text;
            SkybandECR.Properties.Settings.Default.LogLevel = LogLevel.Text;
            string noOfRetainValue = NoDay.Text;


            //if ((string.IsNullOrEmpty(SkybandECR.Properties.Settings.Default.RetainDays)) ||
            //    (!string.IsNullOrEmpty(SkybandECR.Properties.Settings.Default.RetainDays) &&
            //    (float.Parse(SkybandECR.Properties.Settings.Default.RetainDays) != float.Parse(NoDay.Text.ToString()))))
            //{
            //    SkybandECR.Properties.Settings.Default.retentionDate = DateTime.Parse(expirationDate).AddDays(
            //        (double.Parse(NoDay.Text.ToString()) - (double.Parse(SkybandECR.Properties.Settings.Default.RetainDays.ToString())))).ToString();
            //}
            if (noOfRetainValue != "")
            {
                noOfDayValue = int.Parse(noOfRetainValue);
                if (islogAllowed)
                {
                    if (Filepath.Text != "")
                    {
                        filepath = Filepath.Text;
                        if (!Directory.Exists(Path.GetDirectoryName(filepath)))
                        {
                            MessageBox.Show("Invalid file path. Please enter a valid file path.");
                            return;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(filepath) && Directory.Exists(Path.GetDirectoryName(filepath)))
                    {
                        LogFile.SetLogOptions(int.Parse(loglevel), islogAllowed, filepath, noOfDayValue);
                    }
                }
            }

            //11723

            SkybandECR.Properties.Settings.Default.RetainDays = NoDay.Text;
            SkybandECR.Properties.Settings.Default.Save();
            MessageBox.Show("Log Details Save Successfully");

        }
    }
}
