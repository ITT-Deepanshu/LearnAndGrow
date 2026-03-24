using System;

namespace ATMApplication.Exceptions
{
    public class DeviceLockedException : Exception
    {
        public DeviceLockedException() : base("ATM device is locked.") { }
    }
}