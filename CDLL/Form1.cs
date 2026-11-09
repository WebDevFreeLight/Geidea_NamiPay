// TCP/IP Integration Sample Code

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Configuration;


namespace CDLL
{
    public partial class Form1 : Form
    {
       // [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
       // public static extern IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder outResp, byte[] outRespLen);

        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder outResp, byte[] outRespLen);

        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestTCPIPTrxn(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder outResp, byte[] outRespLen);

        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_GetMerchantDetails(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] merchantNameEng, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder merchantNameArb, byte[] merchantAddEng1, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder merchantAddArb1, byte[] merchantAddopt1, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder merchantAddopt2);

        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_TCPIPGetTerminalID(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] terminalId);

        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_CheckStatus(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval);

        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_TCPIPCheckStatus(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval);

        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestReconTCPIPWORcpt(byte[] ip, byte[] port, byte[] inOutBuff, ref int inReqlen, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]

        public static extern IntPtr api_RequestReconCOMWORcpt(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

        [DllImport(@"madaapi_v1_8.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestTCPIPRepeat(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval,byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, byte[] Cardscheme, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

        [DllImport(@"madaapi_v1_8.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestCOMRepeat(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval,  byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }


        private void button2_Click(object sender, EventArgs e)
        {
            string TextString;
            byte[] intval = new byte[1];
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            int trnxType = 0;
            TextString = "4371;0;1!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;

            //var Result = api_RequestCOMTrxn(9, 38400, 0, 8, 0, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            var Result = api_RequestCOMTrxn(5, 38400, 0, 8, 0, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("PAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(schemeId);
            Trans_richTextBox.AppendText("\nCARD ID: " + result);

            result = System.Text.Encoding.UTF8.GetString(trxRrn);
            Trans_richTextBox.AppendText("\nTrxRrn : " + result);

        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;
            byte[] intval = new byte[1];
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            int trnxType = 0;
            TextString = "50;0;1!";
            ip = "192.168.116.166";
            port = "6100";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_RequestTCPIPTrxn(b_ip, b_port, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("PAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(ecrrefno);
            Trans_richTextBox.AppendText("ECR REF NO: " + result);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string TextString;
            byte[] intval = new byte[1];
            byte[] merchantNameEng = new byte[500];
            byte[] merchantAddEng1 = new byte[500];
            byte[] merchantAddopt1 = new byte[500];
            StringBuilder merchantNameArb = new StringBuilder(500);
            StringBuilder merchantAddArb1 = new StringBuilder(500);
            StringBuilder merchantAddopt2 = new StringBuilder(500);
         
            int trnxType = 0;
            TextString = "0A!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_GetMerchantDetails(15, 38400, 0, 8, 0, inOutBuff, intval,merchantNameEng, merchantNameArb, merchantAddEng1, merchantAddArb1, merchantAddopt1, merchantAddopt2);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(merchantNameEng);
            Trans_richTextBox.AppendText("MerchantNameEng: " + result);

            result = merchantNameArb.ToString();
            Trans_richTextBox.AppendText("\nMerchantNameArb: " + result);

            result = System.Text.Encoding.UTF8.GetString(merchantAddEng1);
            Trans_richTextBox.AppendText("\nMerchantAddEng1: " + result);

            result = merchantAddArb1.ToString();
            Trans_richTextBox.AppendText("\nMerchantAddArb1: " + result);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;
            byte[] intval = new byte[1];
            byte[] terminalId = new byte[17];

            int trnxType = 0;
            TextString = "07!";
            ip = "192.168.208.157";
            port = "6100";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_TCPIPGetTerminalID(b_ip, b_port, inOutBuff, intval, terminalId);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(terminalId);
            Trans_richTextBox.AppendText("Terminal ID: " + result);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;
            byte[] intval = new byte[1];
            byte[] terminalId = new byte[17];

            int trnxType = 0;
            TextString = "04!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;
            int portNumber = Convert.ToInt32(ConfigurationManager.AppSettings["PaymentMachinePort"]);
            var Result = api_CheckStatus(portNumber, 38400, 0, 8, 0, inOutBuff, intval);
            //var Result = api_CheckStatus(15, 38400, 0, 8, 0, inOutBuff, intval);
            MessageBox.Show(Result.ToString());
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;
            byte[] intval = new byte[1];
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            int trnxType = 2;
            TextString = "100;123456781234;13112018;1;1";
            ip = "192.168.241.157";
            port = "6100";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_RequestTCPIPTrxn(b_ip, b_port, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("PAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(ecrrefno);
            Trans_richTextBox.AppendText("ECR REF NO: " + result);
        }

        private void CheckStatus_button_Click(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;

            ip = "192.168.208.157";
            port = "6100";
            byte[] intval = new byte[1];

            int trnxType = 0;
            TextString = "04!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_TCPIPCheckStatus(b_ip, b_port, inOutBuff, intval);
            MessageBox.Show(Result.ToString());
        }

        private void Recn_button_Click(object sender, EventArgs e)
        {
            byte[] inReqbuff = new byte[100 * 5];
            string ip;
            string port;
            byte[] intval = new byte[1];
            byte[] rspCode = new byte[30];
            byte[] terminalTotal = new byte[500];
            byte[] hostTotal = new byte[500];
            byte[] stanNo = new byte[30];
            byte[] dataTime = new byte[30];
            byte[] terminalId = new byte[30];
            byte[] merchantId = new byte[30];
            byte[] outresp = new byte[20000];

            int inReqlen = 0, outRsplen = 0;
            uint baudSpeed = 0, parity = 0, dataBits = 0, stopBits = 0;

            Array.Clear(outresp, 0, outresp.Length);
            Array.Clear(inReqbuff, 0, inReqbuff.Length);

            string send_buff = "";
            send_buff += "09;0!";
            inReqbuff = Encoding.ASCII.GetBytes(send_buff);
            inReqlen = inReqbuff.Length;
            string TextString;
            TextString = "09;0;1!";
            ip = "192.168.116.166";
            port = "6100";

            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;


            var Result  = api_RequestReconTCPIPWORcpt(b_ip, b_port, inReqbuff, ref inReqlen, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);

            string result = System.Text.Encoding.UTF8.GetString(terminalTotal);
            Trans_richTextBox.AppendText("\nTerminalTotal: " + result);

            result = System.Text.Encoding.UTF8.GetString(hostTotal);
            Trans_richTextBox.AppendText("\nHostTotal: " + result);
        }

        private void Recon_TCP_Click(object sender, EventArgs e)
        {
            byte[] inReqbuff = new byte[100 * 5];
            byte[] intval = new byte[1];
            byte[] rspCode = new byte[30];
            byte[] terminalTotal = new byte[500];
            byte[] hostTotal = new byte[500];
            byte[] stanNo = new byte[30];
            byte[] dataTime = new byte[30];
            byte[] terminalId = new byte[30];
            byte[] merchantId = new byte[30];
            byte[] outresp = new byte[20000];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            int inReqlen = 0;
            Array.Clear(outresp, 0, outresp.Length);
            Array.Clear(inReqbuff, 0, inReqbuff.Length);

            string TextString = "09;0!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_RequestReconCOMWORcpt(9, 38400, 0, 8, 0, inOutBuff, intval, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);

            string result = System.Text.Encoding.UTF8.GetString(terminalTotal);
            Trans_richTextBox.AppendText("\nTerminalTotal: " + result);

            result = System.Text.Encoding.UTF8.GetString(hostTotal);
            Trans_richTextBox.AppendText("\nHostTotal: " + result);

        }

        private void button8_Click(object sender, EventArgs e)
        {
            string TextString;
            byte[] intval = new byte[1];
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            int trnxType = 2;
            TextString = "50;434615455456;11122024;1;1!";


            //TextString = "100;123456781234;13112018;4153820015307984;0;1";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;

            //var Result = api_RequestCOMTrxn(9, 38400, 0, 8, 0, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            var Result = api_RequestCOMTrxn(5, 38400, 0, 8, 0, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("PAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(schemeId);
            Trans_richTextBox.AppendText("\nCARD ID: " + result);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;
            byte[] intval = new byte[1];
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] cardscheme = new byte[10];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

           
            TextString = "0B!";
            ip = "192.168.8.174";
            port = "6100";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_RequestTCPIPRepeat(b_ip, b_port, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, cardscheme, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("\nPAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(ecrrefno);
            Trans_richTextBox.AppendText("\nECR REF NO: " + result);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            string TextString;
            byte[] intval = new byte[1];
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

 
            TextString = "0B!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;

           
            var Result = api_RequestCOMRepeat(17, 38400, 0, 8, 0, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("PAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(schemeId);
            Trans_richTextBox.AppendText("\nCARD ID: " + result);

            result = System.Text.Encoding.UTF8.GetString(trxRrn);
            Trans_richTextBox.AppendText("\nTrxRrn : " + result);
        }
    }
}
