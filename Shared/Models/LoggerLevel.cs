using System;
using System.Collections.Generic;
using System.Text;

using Serilog;
using Serilog.Events;

namespace Shared.Models
{
    public class LoggerLevel
    {
        public LogEventLevel Default { get; set; }

        public class OverrideConfig
        {
            public LogEventLevel Microsoft { get; set; }

            public LogEventLevel System { get; set; }
        }

        public OverrideConfig Override { get; set; }
        
        public void SetMinimumLevel(LoggerConfiguration logConfig)
        {
            switch (Default)
            {
                case LogEventLevel.Debug:
                    logConfig.MinimumLevel.Debug();
                    break;
                case LogEventLevel.Error:
                    logConfig.MinimumLevel.Error();
                    break;
                case LogEventLevel.Fatal:
                    logConfig.MinimumLevel.Fatal();
                    break;
                case LogEventLevel.Information:
                    logConfig.MinimumLevel.Information();
                    break;
                case LogEventLevel.Verbose:
                    logConfig.MinimumLevel.Verbose();
                    break;
                case LogEventLevel.Warning:
                    logConfig.MinimumLevel.Warning();
                    break;
            }
        }
    }
}
