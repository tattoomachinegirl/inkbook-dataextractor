using System;
using System.Data;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks;
namespace TattooMachineGirl.Inkbook.Data.Extract.Configuration
{
    public class SerilogAdapter
    {
        public static Logger GetLogger(LogEventLevel logLevel = LogEventLevel.Information, DirectoryInfo logDirectory = null)
        {
            var config = new LoggerConfiguration();
            var loglevel = new LoggingLevelSwitch(logLevel);
            config.WriteTo.Console().MinimumLevel.ControlledBy(loglevel);
            
            if (null != logDirectory)
            {
                if (!logDirectory.Exists) logDirectory.Create();
                
                config.WriteTo.RollingFile(logDirectory.FullName, LogEventLevel.Verbose);
            }
            return config.CreateLogger();
        }
    }
}
