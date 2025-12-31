using System.Collections.Generic;
namespace Week2.Assignment3
{
    public class CustomerRepository
    {
        private List<Customer> dbCustomers = new List<Customer>();
        public List<Customer> GetAllCustomers()
        {
            return dbCustomers;
        }
    }
}