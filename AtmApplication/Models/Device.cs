using System;

namespace ATMApplication.Models
{
    public class Device
    {
        public bool IsLocked { get; set; }
        public bool IsConnected { get; set; }

        public void DispenseCash(double amount)
        {
            Console.WriteLine($"Dispensing {amount} cash");
        }
    }
}