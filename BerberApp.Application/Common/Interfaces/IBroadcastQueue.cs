namespace BerberApp.Application.Common.Interfaces;

/// <summary>
/// Toplu mesaj gönderimini arka plana devreder.
///
/// Application katmanı Hangfire'ı doğrudan tanımasın diye araya konan ince bir
/// soyutlama; gerçek kuyruğa alma Infrastructure tarafında yapılıyor.
/// </summary>
public interface IBroadcastQueue
{
    void Enqueue(Guid broadcastJobId);
}
