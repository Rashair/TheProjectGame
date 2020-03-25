using System;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

namespace GameMaster.Tests.Mocks
{
    public class MockLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return state as IDisposable;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Debug.WriteLine(state.ToString());
        }
    }
}
