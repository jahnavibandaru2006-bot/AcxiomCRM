using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class ApiLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class ApiCustomerDto
    {
        public int Id { get; set; }
        public string? CustomerCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = "Active";
        public string? AssignedTo { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ApiCreateCustomerRequest
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Company { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Active";

        public string? AssignedTo { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class ApiLeadDto
    {
        public int Id { get; set; }
        public string? LeadCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Source { get; set; }
        public string Priority { get; set; } = "Medium";
        public decimal? ExpectedValue { get; set; }
        public string Status { get; set; } = "New";
        public string? AssignedTo { get; set; }
        public int? CustomerId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ApiCreateLeadRequest
    {
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

        public string? AssignedTo { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class ApiOpportunityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public int? LeadId { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = "Open";
        public string? AssignedTo { get; set; }
        public decimal WeightedValue { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ApiCreateOpportunityRequest
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public int? LeadId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100)]
        public int Probability { get; set; }

        [Required]
        public DateTime ExpectedCloseDate { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Open";

        public string? AssignedTo { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class ApiFollowUpDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string Status { get; set; } = "Planned";
        public string Type { get; set; } = "Call";
        public string? AssignedTo { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ApiCreateFollowUpRequest
    {
        [Required]
        public int CustomerId { get; set; }

        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }

        [Required]
        public DateTime FollowUpDate { get; set; }

        [Required]
        public string Status { get; set; } = "Planned";

        [Required]
        public string Type { get; set; } = "Call";

        public string? AssignedTo { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
