using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class FollowUp
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public int? LeadId { get; set; }

        public Lead? Lead { get; set; }

        public int? OpportunityId { get; set; }

        public Opportunity? Opportunity { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime FollowUpDate { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Planned";

        [StringLength(100)]
        public string Type { get; set; } = "Call";

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? AssignedTo { get; set; }
    }
}