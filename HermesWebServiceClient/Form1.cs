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

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            AddLog("Timer initiated");
            try
            {
                DateTime fromDate = main.getMaxCallDate();
                if (fromDate != DateTime.MinValue)
                    main.ListAgents(fromDate, DateTime.Now);
                else
                {
                    AddLog("ERROR: Cannot determine valid from date");
                    main.ListAgents(new DateTime(2014, 9, 7), DateTime.Now);
                }
            }
            catch (Exception exc)
            {
                AddLog(exc.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                main.Login();
            }
            catch (Exception exc)
            {
                AddLog(exc.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                if (endDateVal.Value < fromDateVal.Value)
                    AddLog("End Date cannot be earlier than Start Date");
                else
                    main.ListAgents(fromDateVal.Value, endDateVal.Value);
            }
            catch (Exception exc)
            {
                AddLog(exc.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int val = int.MinValue;
            try
            {
                val = int.Parse(txtTimerInterval.Text);
                if (val > 60 || val < 2)
                {
                    val = int.MinValue;
                }
            }
            catch (Exception exc)
            {
                val = int.MinValue;
            }

            if (val == int.MinValue)
            {
                txtTimerInterval.Text = "";
                MessageBox.Show("Invalid value for interval", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                timer1.Stop();
                timer1.Interval = val * (60 * 1000);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            timer1.Stop();
        }
    }
}
