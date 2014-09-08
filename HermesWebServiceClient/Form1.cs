using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HermesWebServiceClient.ServiceReference1;
using LogFileDLL;

namespace HermesWebServiceClient
{
    public partial class Form1 : Form
    {
        
        static Main main;

        public Form1()
        {
            InitializeComponent();

            main = new Main();
            main.LogEvent += new LogEventDelegate(this.AddLog);

            try
            {
                main.LoadSettings();
            }
            catch (Exception exc)
            {
                if (MessageBox.Show("Unable to read application settings\r\nSee detailed information?", "Config Error", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                    MessageBox.Show(exc.ToString(), "Config Error");
                return;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                main.Login();
            }
            catch (Exception exc)
            {
                AddLog(exc.Message);
            }
        }

        public void AddLog(string text)
        {
            //string strLine = DateTime.Now.ToString() + "] " + text.TrimEnd('\0') + "\r\n";
            string strLine = String.Format("{0:yyyy/MM/dd HH:mm:ss.ffff}] {1}\r\n", DateTime.Now, text.TrimEnd('\0'));
            int maxLines = 100;
            string[] linebuffer = new string[maxLines];

            if (main.Settings != null)
                main.Settings.Log.AddText(text);

            //string strLine = DateTime.Now.ToString() + "] " + strEvent.TrimEnd('\0') + ' ' + textBox2.Lines[0 + "\r\n";
            if (InvokeRequired) //this is necessary to make control thread-safe
            {
                try
                {
                    this.Invoke(new MethodInvoker(delegate
                    {
                        //this.textBox2.Text += strLine;
                        if (txtLog.Lines.Length >= maxLines)
                        {
                            Array.Copy(txtLog.Lines, txtLog.Lines.Length - maxLines, linebuffer, 0, maxLines);
                            txtLog.Lines = linebuffer;
                        }
                        this.txtLog.AppendText(strLine);
                    }));
                }
                catch (InvalidOperationException ioe)
                {
                    Console.WriteLine("Log Invoke InvalidOperationException " + ioe.Message);
                }
                return;
            }
            else
            {

                if (txtLog.Lines.Length >= maxLines)
                {
                    Array.Copy(txtLog.Lines, txtLog.Lines.Length - maxLines, linebuffer, 0, maxLines);
                    txtLog.Lines = linebuffer;
                }
                try
                {
                    this.txtLog.AppendText(strLine);
                }
                catch (ObjectDisposedException) { }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                main.ListAgents(new DateTime(2014, 9, 5), DateTime.Now);
            }
            catch (Exception exc)
            {
                AddLog(exc.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {

                /*GetMyInfoRequestMessage infoReq = new GetMyInfoRequestMessage();
               // = wsClient.getMyInfo(infoReq);

                infoReq.sid = sid;
                infoReq.startDate = new DateTime(2014, 9, 1);
                infoReq.endDate = DateTime.Now;

                GetMyInfoResponseMessage info  = wsClient.getMyInfo(infoReq);

                AddLog(String.Format("Agent ID:{0} {1} {2}", info.agentInfo.agentId, info.agentInfo.firstName, info.agentInfo.lastName));
                foreach (ServiceReference1.CallInfoDataContract call in info.calls)
                {
                    AddLog(String.Format("Call :{0}", call.ToString()));
                }*/

            }
            catch (Exception exc)
            {
                AddLog(exc.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {

                /*GetAgentInfoRequestMessage agentInfoReq = new GetAgentInfoRequestMessage();

                agentInfoReq.sid = sid;
                agentInfoReq.agentid = 1000;
                agentInfoReq.startDate = new DateTime(2014, 8, 1);
                agentInfoReq.endDate = DateTime.Now;

                GetAgentInfoResponseMessage info = wsClient.getAgentInfo(agentInfoReq);

                //foreach (AgentInfoDataContract agent in listAgentsRsp.agentInfos)
                //{
                AddLog(String.Format("Agent ID:{0} {1} {2}", info.agentInfo.agentId, info.agentInfo.firstName, info.agentInfo.lastName));
                foreach (ServiceReference1.CallInfoDataContract call in info.calls)
                {
                    AddLog(String.Format("Call :{0}", call.ToString()));
                }*/
            }
            catch (Exception exc)
            {
                AddLog(exc.Message);
            }
        }
    }
}
