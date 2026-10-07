using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class Opportunity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public int? LeadId { get; set; }

        public Lead? Lead { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpectedCloseDate { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Open";

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? AssignedTo { get; set; }

        // Weighted pipeline = Amount × Probability / 100
        public decimal WeightedValue =>
            Amount * Probability / 100;
    }
}