using System;
using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    // Serilog sinks
    public class SuccessSink : ILogEventSink
    {
        private readonly List<string> _buffer;

        public SuccessSink(List<string> buffer)
        {
            _buffer = buffer;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            _buffer.Add($"{logEvent.MessageTemplate} [{logEvent.Level}] {message}");
        }
    }

    public class ErrorSink : ILogEventSink
    {
        private readonly List<string> _buffer;

        public ErrorSink(List<string> buffer)
        {
            _buffer = buffer;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            _buffer.Add($"{logEvent.MessageTemplate} [{logEvent.Level}] {message}");
        }
    }

    public class TechnicalSuccessSink : ILogEventSink
    {
        private readonly List<string> _buffer;

        public TechnicalSuccessSink(List<string> buffer)
        {
            _buffer = buffer;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            _buffer.Add($"{logEvent.MessageTemplate} [{logEvent.Level}] {message}");
        }
    }

    public class TechnicalErrorSink : ILogEventSink
    {
        private readonly List<string> _buffer;

        public TechnicalErrorSink(List<string> buffer)
        {
            _buffer = buffer;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            _buffer.Add($"{logEvent.MessageTemplate} [{logEvent.Level}] {message}");
        }
    }
}