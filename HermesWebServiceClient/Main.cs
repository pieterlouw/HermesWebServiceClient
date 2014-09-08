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
            Settings = (AppSettings)GetConfigSetting("Settings", "General", new AppSettings("Logs", "wsClient", 500, 0, "", "", "", "", "", ""));
            LogEvent("Settings loaded successfully");
        }

        public void Login()
        {
            if (Settings == null)
                LoadSettings();

            Thread thread = new Thread(doLoginThread);
            thread.Start();
        }

        private void doLoginThread()
        {
            try
            {
                LoginRequestMessage login = new LoginRequestMessage();
                login.username = Settings.WSUsername;
                login.password = Settings.WSPassword;

                LogEvent("Logging in..");

                _lastSID = 0; //reset to 0
                LoginResponseMessage loginResponse = _wsClient.login(login);
                _lastSID = loginResponse.sid;

                LogEvent(String.Format("Login Successful -  SID: {0}", loginResponse.sid));
            }
            catch (Exception inExc)
            {
                LogEvent(String.Format("ERR: Logging in: MSG=[{0}] [{1}] [{2}]", inExc.Message, inExc.GetType().FullName, inExc.StackTrace));
            }
        }

        public void ListAgents(DateTime startDate, DateTime endDate)
        {
            if (Settings == null)
                LoadSettings();

            if (_lastSID == 0)
            {
                LogEvent("Login first");
                return;
            }

            _startDate = startDate;
            _endDate = endDate;

            Thread thread = new Thread(doListAgentsThread);
            thread.Start();
        }

        private void doListAgentsThread()
        {
            try
            {
                ListAgentsRequestMessage listAgentsRequest = new ListAgentsRequestMessage();
                listAgentsRequest.sid = _lastSID;
                listAgentsRequest.startDate = _startDate;// new DateTime(2014, 9, 5);
                listAgentsRequest.endDate = _endDate; // DateTime.Now;

                LogEvent(String.Format("Requesting Data: StartDate:{0:yyyy/MM/dd HH:mm:ss.ffff} - EndDate:{1:yyyy/MM/dd HH:mm:ss.ffff}", _startDate, _endDate));

                ListAgentsResponseMessage listAgentsRsp = _wsClient.listAgents(listAgentsRequest);

                LogEvent(String.Format("Data Received: StartDate:{0:yyyy/MM/dd HH:mm:ss.ffff} - EndDate:{1:yyyy/MM/dd HH:mm:ss.ffff}", _startDate, _endDate));
                LogEvent(String.Format("Importing Data: StartDate:{0:yyyy/MM/dd HH:mm:ss.ffff} - EndDate:{1:yyyy/MM/dd HH:mm:ss.ffff}", _startDate, _endDate));

                using (SqlConnection sqlConn = new SqlConnection(CreateSQLString()))
                {
                    sqlConn.Open();

                    foreach (AgentInfoDataContract agent in listAgentsRsp.agentInfos)
                    {
                        //LogEvent(String.Format("Agent ID:{0} {1} {2}", agent.agentId, agent.firstName, agent.lastName));
                        foreach (ServiceReference1.CallInfoDataContract call in agent.calls)
                        {
                            //check if exist
                            /*string sqlStatement = @"SELECT TOP 1 *
                                        FROM DBNotificationsInbox 
                                        WHERE Status = @Status
                                        ORDER BY Inserted";

                            using (SqlCommand sqlCmd = new SqlCommand(sqlStatement, sqlConn))
                            {
                                sqlCmd.Parameters.Add(new SqlParameter("Status", 'N'));

                                using (SqlDataReader reader = sqlCmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                    }
                                }
                            }*/

                            /*[Agent_ID] [int] NULL,
	[Ani] [varchar](50) NULL,
	[Call_Date] [datetime] NULL,
	[Call_ID] [varchar](50) NULL,
	[Campaign] [varchar](50) NULL,
	[Comment] [varchar](900) NULL,
	[Customer_ID] [int] NULL,
	[dnis] [int] NULL,
	[Duration] [int] NULL,
	[End_Reason] [varchar](50) NULL,
	[Memo] [varchar](50) NULL,
	[Queue] [int] NULL,
	[Status_Code] [int] NULL,
	[Status_Detail] [varchar](50) NULL,
	[Status_Group] [varchar](50) NULL,
	[Status_Text] [varchar](50) NULL,
	[Firstname] [varchar](250) NULL,
	[Lastname] [varchar](250) NULL,*/

                            LogEvent(String.Format(@"--> [Agent_ID]=[{0}],[Ani]=[{1}],[Call_Date]=[{2}],[Call_ID]=[{3}],
                                [Campaign]=[{4}],[Comment]=[{5}],[Customer_ID]=[{6}],[dnis]=[{7}],[Duration]=[{8}],
                                [End_Reason]=[{9}],[Memo]=[{10}],[Queue]=[{11}],[Status_Code]=[{12}],[Status_Detail]=[{13}],
                                [Status_Group]=[{14}],[Status_Text]=[{15}],[Firstname]=[{16}],[Lastname]=[{17}]",
                                        agent.agentId, call.ani, call.calldate, call.callid, call.campaign,
                                        call.comments, call.customerid, call.dnis, call.duration, call.endreason,
                                        call.memo, call.queue, call.statuscode, call.statusdetail, call.statusgroup,
                                        call.statustext, agent.firstName, agent.lastName));

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
                    }
                }

                LogEvent(String.Format("Data imported successfully StartDate:{0:yyyy/MM/dd HH:mm:ss.ffff} - EndDate:{1:yyyy/MM/dd HH:mm:ss.ffff}", _startDate, _endDate));
            }
            catch (Exception inExc)
            {
                LogEvent(String.Format("ERR: Listing Agents: MSG=[{0}] [{1}] [{2}]", inExc.Message, inExc.GetType().FullName, inExc.StackTrace));
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
    }

    class AppSettings
    {
        public LogFile Log;
        public int DebugLevel { get; set; }
        public int MaxLogLength { get; set; }
        public string DBHost { get; set; }
        public string DBName { get; set; }
        public string DBUserName { get; set; }
        public string DBPassword { get; set; }
        public string WSUsername { get; set; }
        public string WSPassword { get; set; }

        public AppSettings(string LogPath, string LogPrefix, int MaxLogLen, int DebugLevel,
                        string dbHost, string dbName, string dbUserName, string dbPassword,
                        string wsUsername, string wsPassword)
        {
            Log = new LogFile(LogPrefix, LogPath);
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
            int MaxLogLength = 0, DebugLevel = 0;
            string DBHost = "", DBName = "", DBUserName = "", DBPassword = "";
            string WSUsername = "", WSPassword = "";

            foreach (XmlNode node in section.ChildNodes)
            {
                switch (node.Name)
                {
                    case "LogPath": Path = node.InnerText; break;
                    case "LogPrefix": Prefix = node.InnerText; break;
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
            AppSettings appsettings = new AppSettings(Path, Prefix, MaxLogLength, DebugLevel, DBHost, DBName, DBUserName, DBPassword, WSUsername, WSPassword);

            return appsettings;
        }
    }
}
