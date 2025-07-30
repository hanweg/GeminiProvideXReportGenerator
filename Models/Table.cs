namespace GeminiProvideXReportGenerator.Models
{
    public class Table
    {
        public required string Name { get; set; }
        public required List<Column> Columns { get; set; }
    }
}
