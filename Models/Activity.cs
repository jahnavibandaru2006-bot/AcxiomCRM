using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class Activity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Type { get; set; } = "Call";

        [Required]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public DateTime ActivityDate { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Planned";

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Customer relationship
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Lead relationship
        public int? LeadId { get; set; }
        public Lead? Lead { get; set; }

        // Opportunity relationship
        public int? OpportunityId { get; set; }
        public Opportunity? Opportunity { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? AssignedTo { get; set; }
    }
}