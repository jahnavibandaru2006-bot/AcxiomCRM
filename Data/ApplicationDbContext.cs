using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<IdentityUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly HashSet<string>
            AuditedEntities =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    nameof(Customer),
                    nameof(Lead),
                    nameof(Opportunity),
                    nameof(FollowUp),
                    nameof(Activity)
                };

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor =
                httpContextAccessor;
        }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Lead> Leads { get; set; }

        public DbSet<Opportunity> Opportunities { get; set; }

        public DbSet<FollowUp> FollowUps { get; set; }

        public DbSet<Activity> Activities { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public override int SaveChanges()
        {
            var pendingAudits =
                PrepareAuditEntries();

            var result =
                base.SaveChanges();

            SaveAuditEntries(pendingAudits);

            return result;
        }

        public override int SaveChanges(
            bool acceptAllChangesOnSuccess)
        {
            var pendingAudits =
                PrepareAuditEntries();

            var result =
                base.SaveChanges(
                    acceptAllChangesOnSuccess);

            SaveAuditEntries(pendingAudits);

            return result;
        }

        public override async Task<int>
            SaveChangesAsync(
                CancellationToken cancellationToken = default)
        {
            var pendingAudits =
                PrepareAuditEntries();

            var result =
                await base.SaveChangesAsync(
                    cancellationToken);

            await SaveAuditEntriesAsync(
                pendingAudits,
                cancellationToken);

            return result;
        }

        public override async Task<int>
            SaveChangesAsync(
                bool acceptAllChangesOnSuccess,
                CancellationToken cancellationToken = default)
        {
            var pendingAudits =
                PrepareAuditEntries();

            var result =
                await base.SaveChangesAsync(
                    acceptAllChangesOnSuccess,
                    cancellationToken);

            await SaveAuditEntriesAsync(
                pendingAudits,
                cancellationToken);

            return result;
        }

        private List<PendingAudit> PrepareAuditEntries()
        {
            ChangeTracker.DetectChanges();

            var pendingAudits =
                new List<PendingAudit>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (!AuditedEntities.Contains(
                        entry.Metadata.ClrType.Name))
                {
                    continue;
                }

                if (entry.State != EntityState.Added &&
                    entry.State != EntityState.Modified &&
                    entry.State != EntityState.Deleted)
                {
                    continue;
                }

                var entityName =
                    entry.Metadata.ClrType.Name;

                var action =
                    entry.State switch
                    {
                        EntityState.Added =>
                            "Create",

                        EntityState.Modified =>
                            "Update",

                        EntityState.Deleted =>
                            "Delete",

                        _ => "Unknown"
                    };

                var oldValues =
                    new Dictionary<string, object?>();

                var newValues =
                    new Dictionary<string, object?>();

                foreach (var property
                    in entry.Properties)
                {
                    // Never store sensitive/security fields.
                    if (property.Metadata.IsShadowProperty())
                        continue;

                    if (entry.State == EntityState.Added)
                    {
                        newValues[property.Metadata.Name] =
                            property.CurrentValue;
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        oldValues[property.Metadata.Name] =
                            property.OriginalValue;
                    }
                    else if (entry.State == EntityState.Modified &&
                             property.IsModified)
                    {
                        oldValues[property.Metadata.Name] =
                            property.OriginalValue;

                        newValues[property.Metadata.Name] =
                            property.CurrentValue;
                    }
                }

                pendingAudits.Add(
                    new PendingAudit
                    {
                        Entry = entry,
                        EntityName = entityName,
                        Action = action,
                        OldValue =
                            oldValues.Count > 0
                                ? JsonSerializer.Serialize(
                                    oldValues)
                                : null,
                        NewValue =
                            newValues.Count > 0
                                ? JsonSerializer.Serialize(
                                    newValues)
                                : null
                    });
            }

            return pendingAudits;
        }

        private void SaveAuditEntries(
            List<PendingAudit> pendingAudits)
        {
            if (pendingAudits.Count == 0)
                return;

            var audits =
                BuildAuditLogs(pendingAudits);

            if (audits.Count == 0)
                return;

            AuditLogs.AddRange(audits);

            // Direct base call prevents the audit records
            // from auditing themselves.
            base.SaveChanges();
        }

        private async Task SaveAuditEntriesAsync(
            List<PendingAudit> pendingAudits,
            CancellationToken cancellationToken)
        {
            if (pendingAudits.Count == 0)
                return;

            var audits =
                BuildAuditLogs(pendingAudits);

            if (audits.Count == 0)
                return;

            AuditLogs.AddRange(audits);

            // Direct base call prevents recursive auditing.
            await base.SaveChangesAsync(
                cancellationToken);
        }

        private List<AuditLog> BuildAuditLogs(
            List<PendingAudit> pendingAudits)
        {
            var httpContext =
                _httpContextAccessor.HttpContext;

            var userId =
                httpContext?.User?.Identity?.IsAuthenticated == true
                    ? httpContext.User.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier)
                        ?.Value
                    : null;

            var ipAddress =
                httpContext?.Connection?.RemoteIpAddress
                    ?.ToString();

            return pendingAudits
                .Select(p =>
                {
                    var recordId =
                        p.Entry.Properties
                            .FirstOrDefault(
                                x => x.Metadata.Name == "Id")
                            ?.CurrentValue
                            ?.ToString();

                    return new AuditLog
                    {
                        UserId = userId,
                        Action = p.Action,
                        EntityName = p.EntityName,
                        RecordId = recordId,
                        OldValue = p.OldValue,
                        NewValue = p.NewValue,
                        CreatedDate = DateTime.UtcNow,
                        IpAddress = ipAddress
                    };
                })
                .ToList();
        }

        private sealed class PendingAudit
        {
            public Microsoft.EntityFrameworkCore.ChangeTracking
                .EntityEntry Entry { get; set; } = null!;

            public string EntityName { get; set; } = string.Empty;

            public string Action { get; set; } = string.Empty;

            public string? OldValue { get; set; }

            public string? NewValue { get; set; }
        }
    }
}
