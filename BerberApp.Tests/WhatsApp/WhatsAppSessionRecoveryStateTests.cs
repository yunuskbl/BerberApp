using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Services;
using FluentAssertions;
using Xunit;

namespace BerberApp.Tests.WhatsApp;

/// <summary>Sessiz kurtarmanın karar kuralları — saat elle ilerletilerek.</summary>
public class WhatsAppSessionRecoveryStateTests
{
    private const string Session = "t-salon-123";

    private DateTime _now = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);
    private readonly WhatsAppSessionRecoveryState _state;

    public WhatsAppSessionRecoveryStateTests() => _state = new WhatsAppSessionRecoveryState(() => _now);

    private void Advance(TimeSpan by) => _now += by;

    [Theory]
    [InlineData("CONNECTED", true, SessionCondition.Healthy)]
    [InlineData("inChat", true, SessionCondition.Healthy)]
    [InlineData("QRCODE", true, SessionCondition.AwaitingQr)]
    [InlineData("notLogged", true, SessionCondition.AwaitingQr)]
    [InlineData("CLOSED", true, SessionCondition.Closed)]
    [InlineData("desconnectedMobile", true, SessionCondition.Closed)]
    [InlineData("INITIALIZING", true, SessionCondition.Starting)]
    [InlineData("UNKNOWN", true, SessionCondition.Unknown)]
    [InlineData("UNREACHABLE", false, SessionCondition.Unknown)]
    [InlineData("HTTP_401", false, SessionCondition.Unknown)]
    public void Classify_maps_wppconnect_states(string value, bool reachable, SessionCondition expected)
        => WhatsAppSessionRecoveryState.Classify(new WppConnectStatus(value, reachable)).Should().Be(expected);

    [Fact]
    public void Unknown_session_may_be_attempted()
        => _state.ShouldAttempt(Session).Should().BeTrue();

    [Fact]
    public void Failed_attempts_back_off_5_then_10_minutes()
    {
        _state.Record(Session, RecoveryOutcome.StillStarting);
        _state.ShouldAttempt(Session).Should().BeFalse();
        Advance(TimeSpan.FromMinutes(5));
        _state.ShouldAttempt(Session).Should().BeTrue();

        _state.Record(Session, RecoveryOutcome.StartFailed);
        Advance(TimeSpan.FromMinutes(9));
        _state.ShouldAttempt(Session).Should().BeFalse();
        Advance(TimeSpan.FromMinutes(1));
        _state.ShouldAttempt(Session).Should().BeTrue();
    }

    [Fact]
    public void Backoff_is_capped_at_two_hours()
    {
        for (var i = 0; i < 10; i++) _state.Record(Session, RecoveryOutcome.StartFailed);

        Advance(TimeSpan.FromMinutes(119));
        _state.ShouldAttempt(Session).Should().BeFalse();
        Advance(TimeSpan.FromMinutes(1));
        _state.ShouldAttempt(Session).Should().BeTrue();
    }

    [Fact]
    public void Needs_qr_holds_off_retries_for_twelve_hours()
    {
        _state.Record(Session, RecoveryOutcome.NeedsQr);

        Advance(TimeSpan.FromHours(11));
        _state.ShouldAttempt(Session).Should().BeFalse();
        Advance(TimeSpan.FromHours(1));
        _state.ShouldAttempt(Session).Should().BeTrue();
    }

    [Fact]
    public void Healthy_session_clears_failures_and_needs_qr()
    {
        _state.Record(Session, RecoveryOutcome.NeedsQr);
        _state.MarkHealthy(Session);   // ör. merkezi hat aynı adla QR okutularak bağlandı

        _state.ShouldAttempt(Session).Should().BeTrue();
    }

    [Fact]
    public void Manual_action_suppresses_for_ten_minutes_and_forgets_failures()
    {
        _state.Record(Session, RecoveryOutcome.NeedsQr);
        _state.Suppress(Session);

        _state.IsSuppressed(Session).Should().BeTrue();
        _state.ShouldAttempt(Session).Should().BeFalse();

        Advance(TimeSpan.FromMinutes(10));
        _state.IsSuppressed(Session).Should().BeFalse();
        _state.ShouldAttempt(Session).Should().BeTrue();
    }

    [Fact]
    public void Suppress_ignores_empty_session_names()
    {
        _state.Suppress(null);
        _state.Suppress("  ");
        _state.Snapshot().Should().BeEmpty();
    }

    [Fact]
    public void Qr_after_own_unfinished_attempt_counts_as_started_recently()
    {
        _state.StartedRecently(Session).Should().BeFalse();

        _state.Record(Session, RecoveryOutcome.StillStarting);
        _state.StartedRecently(Session).Should().BeTrue();

        Advance(TimeSpan.FromMinutes(16));
        _state.StartedRecently(Session).Should().BeFalse();
    }

    [Fact]
    public void Successful_recovery_is_not_started_recently()
    {
        _state.Record(Session, RecoveryOutcome.Recovered);
        _state.StartedRecently(Session).Should().BeFalse();
    }

    [Fact]
    public void Starting_duration_accumulates_until_cleared()
    {
        _state.NoteStarting(Session).Should().Be(TimeSpan.Zero);
        Advance(TimeSpan.FromMinutes(16));
        _state.NoteStarting(Session).Should().BeGreaterThanOrEqualTo(WhatsAppSessionRecoveryState.StuckStartingAfter);

        _state.ClearStarting(Session);
        _state.NoteStarting(Session).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Forget_drops_sessions_no_longer_in_use()
    {
        _state.Record("old", RecoveryOutcome.StartFailed);
        _state.Record(Session, RecoveryOutcome.StartFailed);

        _state.Forget([Session]);

        _state.Snapshot().Should().ContainSingle().Which.Session.Should().Be(Session);
    }
}
