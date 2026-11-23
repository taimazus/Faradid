using System;

namespace Faradid.Core.Models;

public sealed class ProjectProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string? LocalSourcePath { get; set; }
    public string? TargetReportPath { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string OwnerUsername { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
