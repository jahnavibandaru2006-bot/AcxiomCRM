using System;
using System.Collections.Generic;

namespace AcxiomCRM.Models
{
    public class DashboardViewModel
    {
        public string Period { get; set; } = "ThisMonth";

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int TotalCustomers { get; set; }

        public int TotalLeads { get; set; }

        public int OpenLeads { get; set; }

        public int TotalOpportunities { get; set; }

        public int OpenOpportunities { get; set; }

        public int WonOpportunities { get; set; }

        public int LostOpportunities { get; set; }

        public int PendingFollowUps { get; set; }

        public decimal TotalPipelineValue { get; set; }

        public List<string> LeadStatusLabels { get; set; }
            = new();

        public List<int> LeadStatusValues { get; set; }
            = new();

        public List<string> OpportunityStageLabels { get; set; }
            = new();

        public List<int> OpportunityStageValues { get; set; }
            = new();

        public List<string> MonthlySalesLabels { get; set; }
            = new();

        public List<decimal> MonthlySalesValues { get; set; }
            = new();
    }
}
