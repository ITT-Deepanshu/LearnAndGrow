using static Enums;

namespace Online_Banking_System.Models
{
    public class LoanRequest
    {
        public string LoanId { get; set; }
        public decimal PrincipalAmount { get; set; }
        public int TermInMonths { get; set; }
        public LoanType LoanType { get; set; }
    }
}
