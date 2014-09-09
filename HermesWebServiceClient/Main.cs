using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using LogFileDLL;
using HermesWebServiceClient.ServiceReference1;
using System.Threading;
using System.Data.SqlClient;

namespace HermesWebServiceClient
{
    class Main
    {
        public event LogEventDelegate LogEvent;
        public event LogEventDelegate DataLogEvent;
        public AppSettings Settings = null;

        private Hermes_ApiClient _wsClient = null;
        private int _lastSID = 0;
        private DateTime _startDate;
        private DateTime _endDate;

        public Main()
        {
            _lastSID = 0;
            _wsClient = new Hermes_ApiClient();
        }

        public void LoadSettings()
        {
            // Read settings from config file
            Settings = (AppSettings)GetConfigSetting("Settings", "General", new AppSettings("Logs", "wsClient", 500, "DataLogs", "data", 0, "", "", "", "", "", ""));
            AddLog("Settings loaded successfully");
        }

        public void Login()
        {
            if (Settings == null)
                LoadSettings();

            try
            {
                LoginRequestMessage login = new LoginRequestMessage();
                login.username = Settings.WSUsername;
                login.password = Settings.WSPassword;

                AddLog("Logging in..");

                _lastSID = 0; //reset to 0
                LoginResponseMessage loginResponse = _wsClient.login(login);
                _lastSID = loginResponse.sid;

                AddLog(String.Format("Login Successful -  SID: {0}", loginResponse.sid));
            }
            catch (Exception inExc)
            {
                AddLog(String.Format("ERR: Logging in: MSG=[{0}] [{1}] [{2}]", inExc.Message, inExc.GetType().FullName, inExc.StackTrace));
            }
        }

        public DateTime getMaxCallDate()
        {
            DateTime returnVal = DateTime.MinValue;

            if (Settings == null)
                LoadSettings();

            using (SqlConnection sqlConn = new SqlConnection(CreateSQLString()))
            {
                sqlConn.Open();

                string sqlStatement = @"SELECT ISNULL(MAX(call_date), '1970-01-01 00:00:00') AS MaxCallDate FROM Tabi_data";

                using (SqlCommand sqlCmd = new SqlCommand(sqlStatement, sqlConn))
                {
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            try
                            {
                                reader.Read();
                                //returnVal = Convert.ToDateTime(reader["MaxCallDate"]).ToString("yyyy-MM-dd hh:mm:ss");
                                returnVal = DateTime.Parse(reader["MaxCallDate"].ToString());

                                if (returnVal == new DateTime(1970, 1, 1, 0, 0, 0))
                                    returnVal = DateTime.MinValue;
                            }
                            catch (FormatException formateExc)
                            {
                                returnVal = DateTime.MinValue;
                                AddLog(String.Format("ERROR Converting db value to valid DateTime [{0}]", reader["MaxCallDate"].ToString()));
                            }
                        }
                    }
                }
            }

            return returnVal;
        }

        public void ListAgents(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (Settings == null)
                    LoadSettings();

                if (_lastSID == 0)
                {
                    Login();
                }

                _startDate = startDate;
                _endDate = endDate;                

                if (_lastSID == 0)
                    return;

                ListAgentsRequestMessage listAgentsRequest = new ListAgentsRequestMessage();
                listAgentsRequest.sid = _lastSID;
                listAgentsRequest.startDate = _startDate;
                listAgentsRequest.endDate = _endDate;

                AddLog(String.Format("Requesting Data: StartDate:{0:yyyy/MM/dd HH:mm:ss.ffff} - EndDate:{1:yyyy/MM/dd HH:mm:ss.ffff}", _startDate, _endDate));

                ListAgentsResponseMessage listAgentsRsp = _wsClient.listAgents(listAgentsRequest);

                AddLog(String.Format("Data Received: StartDate:{0:yyyy/MM/dd HH:mm:ss.ffff} - EndDate:{1:yyyy/MM/dd HH:mm:ss.ffff}", _startDate, _endDate));
                AddLog(String.Format("Importing Data: StartDate:{0:yyyy/MM/dd HH:mm:ss.ffff} - EndDate:{1:yyyy/MM/dd HH:mm:ss.ffff}", _startDate, _endDate));

                using (SqlConnection sqlConn = new SqlConnection(CreateSQLString()))
                {
                    sqlConn.Open();

                    foreach (AgentInfoDataContract agent in listAgentsRsp.agentInfos)
                    {
                        foreach (ServiceReference1.CallInfoDataContract call in agent.calls)
                        {
                            AddDataLog(String.Format(@"--> [Agent_ID]=[{0}],[Ani]=[{1}],[Call_Date]=[{2}],[Call_ID]=[{3}],
                                [Campaign]=[{4}],[Comment]=[{5}],[Customer_ID]=[{6}],[dnis]=[{7}],[Duration]=[{8}],
                                [End_Reason]=[{9}],[Memo]=[{10}],[Queue]=[{11}],[Status_Code]=[{12}],[Status_Detail]=[{13}],
                                [Status_Group]=[{14}],[Status_Text]=[{15}],[Firstname]=[{16}],[Lastname]=[{17}]",
                                        agent.agentId, call.ani, call.calldate, call.callid, call.campaign,
                                        call.comments, call.customerid, call.dnis, call.duration, call.endreason,
                                        call.memo, call.queue, call.statuscode, call.statusdetail, call.statusgroup,
                                        call.statustext, agent.firstName, agent.lastName));

                            //check if call already exist in database
                            string sqlStatement = @"SELECT COUNT(*) AS Qty
                                        FROM Tabi_data 
                                        WHERE Agent_ID = @Agent_ID
                                        AND Call_ID = @Call_ID";

                            using (SqlCommand sqlCmd = new SqlCommand(sqlStatement, sqlConn))
                            {
                                sqlCmd.Parameters.Add(new SqlParameter("@Agent_ID", agent.agentId));
                                sqlCmd.Parameters.Add(new SqlParameter("@Call_ID", call.callid));

                                using (SqlDataReader reader = sqlCmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        reader.Read();
                                        if (reader["Qty"].ToString() == "0")
                                        {
                                            string sqlStr = @"INSERT INTO tabi_data(Agent_ID,Ani,Call_Date,Call_ID,Campaign,
                                                Comment,Customer_ID,dnis,Duration,End_Reason,Memo,Queue,Status_Code,
                                                Status_Detail,Status_Group,Status_Text,Firstname,Lastname) 
                                            VALUES (@Agent_ID,@Ani,@Call_Date,@Call_ID,@Campaign,
                                                @Comment,@Customer_ID,@dnis,@Duration,@End_Reason,@Memo,@Queue,@Status_Code,
                                                @Status_Detail,@Status_Group,@Status_Text,@Firstname,@Lastname)";

                                            using (SqlCommand sqlCommand = new SqlCommand(sqlStr, sqlConn))
                                            {
                                                sqlCommand.Parameters.Add(new SqlParameter("@Agent_ID", agent.agentId));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Ani", call.ani));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Call_Date", call.calldate));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Call_ID", call.callid));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Campaign", call.campaign));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Comment", call.comments));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Customer_ID", call.customerid));
                                                sqlCommand.Parameters.Add(new SqlParameter("@dnis", call.dnis));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Duration", call.duration));
                                                sqlCommand.Parameters.Add(new SqlParameter("@End_Reason", call.endreason));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Memo", call.memo));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Queue", call.queue));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Status_Code", call.statuscode));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Status_Detail", call.statusdetail));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Status_Group", call.statusgroup));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Status_Text", call.statustext));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Firstname", agent.firstName));
                                                sqlCommand.Parameters.Add(new SqlParameter("@Lastname", agent.lastName));

                                                sqlCommand.ExecuteNonQuery();
                                            }
                                        }
                                        else
                                        {
                                            AddLog(String.Format("Call already exist: AgentID=[{0}] CallID=[{1}]", agent.agentId, call.callid));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                AddLog(String.Format("Data imported successfully StartDate:{0:yyyy/MM/dd HH:mm:ss.ffff} - EndDate:{1:yyyy/MM/dd HH:mm:ss.ffff}", _startDate, _endDate));
            }
            catch (Exception inExc)
            {
                AddLog(String.Format("ERROR - Listing Agents: MSG=[{0}] [{1}] [{2}]", inExc.Message, inExc.GetType().FullName, inExc.StackTrace));
            }
        }

        private string CreateSQLString()
        {
            if (Settings == null)
                LoadSettings();

            if (Settings.DBUserName == "")
                return String.Format("MultipleActiveResultSets=True;Trusted_Connection=True;server={0};database={1}", Settings.DBHost, Settings.DBName);
            else
                return String.Format("MultipleActiveResultSets=True;user id={0};password={1};server={2};database={3}", Settings.DBUserName, Settings.DBPassword, Settings.DBHost, Settings.DBName);
        }

        private static object GetConfigSetting(string Section, string Field, object Default)
        {
            object retval = ConfigurationManager.GetSection(string.Format("{0}/{1}", Section, Field));
            if (retval == null)
                return Default;
            return retval;
        }

        private void AddLog(string text)
        {
            if (LogEvent != null)
                LogEvent(text);
        }

        private void AddDataLog(string text)
        {
            string strLine = String.Format("{0:yyyy/MM/dd HH:mm:ss.ffff}] {1}\r\n", DateTime.Now, text.TrimEnd('\0'));
            Settings.DataLog.AddText(strLine);

            if (DataLogEvent != null)
                DataLogEvent(text);
        }
    }

    class AppSettings
    {
        public LogFile Log;
        public LogFile DataLog;
        public int DebugLevel { get; set; }
        public int MaxLogLength { get; set; }
        public string DBHost { get; set; }
        public string DBName { get; set; }
        public string DBUserName { get; set; }
        public string DBPassword { get; set; }
        public string WSUsername { get; set; }
        public string WSPassword { get; set; }

        public AppSettings(string LogPath, string LogPrefix, int MaxLogLen,
                        string DataLogPath, string DataLogPrefix, int DebugLevel,
                        string dbHost, string dbName, string dbUserName, string dbPassword,
                        string wsUsername, string wsPassword)
        {
            Log = new LogFile(LogPrefix, LogPath);
            DataLog = new LogFile(DataLogPath, DataLogPrefix);
            this.MaxLogLength = MaxLogLen;
            this.DebugLevel = DebugLevel;
            this.DBHost = dbHost;
            this.DBName = dbName;
            this.DBUserName = dbUserName;
            this.DBPassword = dbPassword;
            this.WSUsername = wsUsername;
            this.WSPassword = wsPassword;
        }
    }

    class GeneralCSH : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            string Path = "", Prefix = "";
            string DataLogPath = "", DataLogPrefix = "";
            int MaxLogLength = 0, DebugLevel = 0;
            string DBHost = "", DBName = "", DBUserName = "", DBPassword = "";
            string WSUsername = "", WSPassword = "";

            foreach (XmlNode node in section.ChildNodes)
            {
                switch (node.Name)
                {
                    case "LogPath": Path = node.InnerText; break;
                    case "LogPrefix": Prefix = node.InnerText; break;
                    case "DataLogPath": DataLogPath = node.InnerText; break;
                    case "DataLogPrefix": DataLogPrefix = node.InnerText; break;
                    case "MaxLogLength": MaxLogLength = int.Parse(node.InnerText); break;
                    case "DebugLevel": DebugLevel = int.Parse(node.InnerText); break;
                    case "DBHost": DBHost = node.InnerText; break;
                    case "DBName": DBName = node.InnerText; break;
                    case "DBUserName": DBUserName = node.InnerText; break;
                    case "DBPassword": DBPassword = node.InnerText; break;
                    case "WSUsername": WSUsername = node.InnerText; break;
                    case "WSPassword": WSPassword = node.InnerText; break;
                }
            }
            AppSettings appsettings = new AppSettings(Path, Prefix, MaxLogLength, DataLogPath, DataLogPrefix, DebugLevel, DBHost, DBName, DBUserName, DBPassword, WSUsername, WSPassword);

            return appsettings;
        }
    }
}
