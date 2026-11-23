namespace Faradid.Core.Enums;

public enum PersonaView
{
    Specialist = 1,    // کارشناسان ارشد سئو
    Client = 2,        // مشتریان و کارفرمایان
    Executive = 3,     // مدیر کل و هیئت‌مدیره
    Developer = 4,     // توسعه‌دهندگان وب و زیرساخت
    Comprehensive = 5  // کارنامه جامع و کامل (شامل کلیه بخش‌ها)
}

public enum TrendDirection
{
    Up = 1,
    Down = 2,
    Stable = 3
}

public enum ThreatLevel
{
    Low = 1,
    Moderate = 2,
    High = 3,
    Critical = 4
}

public enum AuditSeverity
{
    Info = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Critical = 5
}

public enum DeviceType
{
    All = 0,
    Desktop = 1,
    Mobile = 2,
    Tablet = 3
}
