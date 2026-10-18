using System;
using System.Collections.Generic;

namespace BerberApp.Domain.Entities;

public enum BroadcastKind
{
    Customer = 0,
    Staff    = 1,
}

public enum BroadcastStatus
{
    Queued    = 0,
    Running   = 1,
    Completed = 2,
    Failed    = 3,
}

public enum BroadcastRecipientStatus
{
    Pending = 0,
    Sent    = 1,
    Failed  = 2,
    /// <summary>Telefon numarası yok — gönderim denenmedi. "Başarısız" ile karıştırılmamalı.</summary>
    Skipped = 3,
}

/// <summary>
/// Bir toplu mesaj gönderiminin kaydı. Gönderim HTTP isteğinin içinde değil,
/// Hangfire işinde yürür; bu kayıt da ilerlemeyi ve alıcı bazında sonucu taşır.
/// </summary>
public class BroadcastJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    public BroadcastKind Kind { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public BroadcastStatus Status { get; set; } = BroadcastStatus.Queued;

    /// <summary>İş tamamen başarısız olduğunda sebebi (ör. oturum kopuk).</summary>
    public string? Error { get; set; }

    public int Total { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<BroadcastRecipient> Recipients { get; set; } = new List<BroadcastRecipient>();
}

/// <summary>
/// Toplu mesajın tek bir alıcısı ve sonucu. Başarısızlık sebebi burada
/// saklanıyor; eskiden handler'daki çıplak catch yüzünden tamamen kayboluyordu.
/// </summary>
public class BroadcastRecipient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BroadcastJobId { get; set; }

    public string? Name { get; set; }
    public string? Phone { get; set; }

    public BroadcastRecipientStatus Status { get; set; } = BroadcastRecipientStatus.Pending;
    public string? Error { get; set; }
    public DateTime? SentAt { get; set; }

    public BroadcastJob? BroadcastJob { get; set; }
}
