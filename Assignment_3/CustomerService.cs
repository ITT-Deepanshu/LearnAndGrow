using System;

using System.Collections.Generic;

using System.Linq;

namespace Assignment3

{

    public class CustomerService

    {

        private readonly ICustomerRepository _customerRepository;

        public CustomerService(CustomerRepository customerRepository)

        {

            _customerRepository = customerRepository;

        }

        public List<Customer> GetCustomers(Func<Customer, bool> filter)

        {

            return _customerRepository.GetAllCustomers()

                      .Where(filter)

                      .OrderBy(c => c.ID)

                      .ToList();

        }

        public List<Customer> SearchByCountry(string country)

        {

            return GetCustomers(c => c.Country.Contains(country));

        }

        public List<Customer> SearchByCompanyName(string company)

        {

            return GetCustomers(c => c.Company.Contains(company));

        }

        public List<Customer> SearchByContact(string contact)

        {

            return GetCustomers(c => c.Contact.Contains(contact));

        }

    }

}

