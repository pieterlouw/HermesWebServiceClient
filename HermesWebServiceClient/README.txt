This application import agent calls into a SQL database using Hermes API web service
(WSDL: http://196.29.140.173/webapps/api/Hermes_Api.svc?wsdl)

Data can be imported automatically by setting a timer (in minutes), or by doing it manually.
By doing it automatically the application check what the last call date is what has been imported
and use that as the StartDate for the web service call. The EndDate will be the  datetime stamp when the timer
is triggered. (the last call date is fetched by calling 'SELECT ISNULL(MAX(call_date), '1970-01-01 00:00:00') AS MaxCallDate FROM Tabi_data')
Manually the Start and EndDate can be set.

There are 2 kinds of output logs, data and normal logs which is grouped in daily files. 
The data logs are text representation of the data fetched from the API
The normal logs are normal application usage logging (this is what is displayed visually as well)

Settings can be changed in the xml formatted config file (HermesWebServiceClient.exe.config)
Many settings were auto generated, however the most important will be this section:

<Settings>   
    <General>
      <LogPath>(this is the path to the normal log file)</LogPath>
      <LogPrefix>(this is the prefix of the normal log file )</LogPrefix>
      <DataLogPath>(this is the path to the data log file)</DataLogPath>
      <DataLogPrefix>(this is the prefix of the data file )</DataLogPrefix>
      <MaxLogLength>(this is the maximum number of lines that will be remembered visually)</MaxLogLength>
      <DebugLevel>(not used at the moment)</DebugLevel>
      <DBHost>(details of the database server)</DBHost>
      <DBName>(database name that will be used)</DBName>
      <DBUserName>(if WindowsAuthentication is used leave blank, otherwise use database username)</DBUserName>
      <DBPassword>(if WindowsAuthentication is used leave blank, otherwise use database password)</DBPassword>
      <WSUsername>(username to use Hermes API)</WSUsername>
      <WSPassword>(password to use Hermes API)</WSPassword>
    </General>
  </Settings>
