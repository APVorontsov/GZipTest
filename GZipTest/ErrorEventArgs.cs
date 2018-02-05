using System;

namespace GZipTest
{
    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }

        public string Message { get; }

        public ErrorEventArgs(Exception exception, string message)
        {
            Exception = exception;
            Message = message;
        }
    }
}