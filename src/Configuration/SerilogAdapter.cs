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
          
            config.WriteTo.Console(logLevel);
            
            if (null != logDirectory)
            {
                if (!logDirectory.Exists) logDirectory.Create();
                
                config.WriteTo.File($"{logDirectory.FullName}/log_{ DateTime.Now.ToString("MMddyyyy_hh_mm_ss")}.txt").MinimumLevel.Verbose();
                
            }
            
            return config.CreateLogger();
        }
    }
}
