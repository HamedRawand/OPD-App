using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.Services;

public static class AuditService
{
    public static void Log(string action, string? entityType = null, int? entityId = null, string? details = null)
    {
        try
        {
            var user = App.Auth?.CurrentUser;

            // Use a dedicated short-lived context so SaveChanges() here never
            // accidentally commits pending changes on the shared App.Db context.
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={App.DbPath}")
                .Options;

            using var auditDb = new AppDbContext(opts);
            auditDb.AuditLogs.Add(new AuditLog
            {
                UserId     = user?.Id,
                Username   = user?.Username ?? "system",
                Action     = action,
                EntityType = entityType,
                EntityId   = entityId,
                Details    = details,
                Timestamp  = DateTime.UtcNow
            });
            auditDb.SaveChanges();
        }
        catch { /* Audit must never crash the app */ }
    }
}
