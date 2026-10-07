using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class Lead
    {
        public int Id { get; set; }

        [StringLength(30)]
        public string? LeadCode { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(150)]
        public string? Company { get; set; }

        [StringLength(100)]
        public string? Source { get; set; }

        [StringLength(30)]
        public string Priority { get; set; } = "Medium";

        [Range(0, double.MaxValue)]
        public decimal? ExpectedValue { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "New";

        [StringLength(450)]
        public string? AssignedTo { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Set when the lead is converted to a customer.
        public int? CustomerId { get; set; }

        public Customer? Customer { get; set; }
    }
}
