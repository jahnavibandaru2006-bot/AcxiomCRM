using System;
using System.Collections.Generic;

namespace AcxiomCRM.Models
{
    public class ReportsViewModel
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }

        public int TotalLeads { get; set; }
        public int NewLeads { get; set; }
        public int QualifiedLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int LostLeads { get; set; }

        public int TotalFollowUps { get; set; }
        public int PlannedFollowUps { get; set; }
        public int CompletedFollowUps { get; set; }
        public int MissedFollowUps { get; set; }
        public int OverdueFollowUps { get; set; }

        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }

        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
        public decimal WonRevenue { get; set; }

        public decimal ConversionRate { get; set; }

        public List<ReportRow> LeadStatusReport { get; set; } = new();
        public List<ReportRow> OpportunityStageReport { get; set; } = new();
        public List<ReportRow> FollowUpStatusReport { get; set; } = new();
        public List<ReportRow> CustomerStatusReport { get; set; } = new();

        public List<AuditReportRow> UserActivityReport { get; set; } = new();
        public List<AuditReportRow> AuditReport { get; set; } = new();
    }

    public class ReportRow
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Value { get; set; }
    }

    public class AuditReportRow
    {
        public DateTime Date { get; set; }
        public string? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? RecordId { get; set; }
        public string? IpAddress { get; set; }
    }
}
