using System;

namespace ATMApplication.Exceptions
{
    public class NetworkConnectionException : Exception
    {
        public NetworkConnectionException() : base("No network connection.") { }
    }
}