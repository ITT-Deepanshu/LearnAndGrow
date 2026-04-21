namespace Core.Models
{
    public class Record
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
        public DateTime? Date { get; set; }

        public double DoubledValue => Value * 2;
        public double SquaredValue => Value * Value;
    }
}