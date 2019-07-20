using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ServiceWrapper
{
    public partial class MainService : ServiceBase
    {
        public static Process process;
        public static string mainServerName = null;

        public MainService()
        {
            InitializeComponent();
            eventLog = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists(this.ServiceName))
            {
                System.Diagnostics.EventLog.CreateEventSource(this.ServiceName, "ServiceWrapper");
            }
            eventLog.Source = this.ServiceName;
            eventLog.Log = "ServiceWrapper";
        }

        private void Log(string message, EventLogEntryType eventLogEntryType)
        {
            if (MainService.mainServerName == null)
            {
                try
                {
                    int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service where ProcessId = " + processId);
                    ManagementObjectCollection collection = searcher.Get();
                    var serviceName = (string)collection.Cast<ManagementBaseObject>().First()["Name"];
                    if (!System.Diagnostics.EventLog.SourceExists(serviceName))
                    {
                        System.Diagnostics.EventLog.CreateEventSource(serviceName, "ServiceWrapper");
                    }
                    eventLog.Source = serviceName;
                    MainService.mainServerName = serviceName;
                }
                catch (Exception e)
                {
                    eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                }
            }
            eventLog.WriteEntry(message, eventLogEntryType);
        }

        private void Log(string message)
        {
            this.Log(message, EventLogEntryType.Information);
        }

        protected override void OnStart(string[] args)
        {
            args = Environment.GetCommandLineArgs();
            Log("Starting service: " + string.Join(" ", args));
            if (args.Length < 3)
            {
                Log("No enough arguments", EventLogEntryType.Error);
                throw new ArgumentException("No enough arguments");
            }
            try
            {
                Directory.SetCurrentDirectory(args[1]);
                MainService.process = new Process();
                MainService.process.StartInfo.FileName = args[2];
                MainService.process.StartInfo.Arguments = string.Join(" ", args.Skip(3).ToArray());
                MainService.process.StartInfo.CreateNoWindow = true;
                MainService.process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                MainService.process.StartInfo.UseShellExecute = false;
                MainService.process.EnableRaisingEvents = true;
                MainService.process.Exited += new EventHandler(ProcessExited);
                MainService.process.Start();
            } catch (Exception e)
            {
                Log(e.Message, EventLogEntryType.Error);
                throw e;
            }
        }

        protected override void OnStop()
        {
            Log("Stopping service");
            try
            {
                MainService.process.Kill();
                MainService.process.WaitForExit();
                MainService.process.Close();
                MainService.process.Dispose();
            }
            catch (Exception e)
            {
                Log(e.Message, EventLogEntryType.Error);
            }
        }

        // Handle Exited event and display process information.
        private void ProcessExited(object sender, System.EventArgs e)
        {
            Log($"Process exited: Exit time: {MainService.process.ExitTime}, Exit code: {MainService.process.ExitCode}");
            process.Close();
            process.Dispose();
            throw new Exception("Process exited");
        }
    }
}
