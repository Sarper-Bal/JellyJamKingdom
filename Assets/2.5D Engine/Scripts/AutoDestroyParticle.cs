using UnityEngine;

// Bu script'in çalışması için bir ParticleSystem bileşenine ihtiyaç duyduğunu belirtiyoruz.
// YENİ: IPooledObject arayüzünü uyguluyoruz.
[RequireComponent(typeof(ParticleSystem))]
public class AutoDestroyParticle : MonoBehaviour, IPooledObject
{
    private ParticleSystem ps;

    // YENİ: IPooledObject'ten gelen özellikler.
    public string PoolTag { get; set; }
    public void OnObjectSpawn()
    {
        // Efekt her spawn olduğunda particle sistemini yeniden başlatmak iyi bir fikirdir.
        // Bu, önceden kalmış partikülleri temizler.
        if (ps == null) ps = GetComponent<ParticleSystem>();
        ps.Play();
    }

    private void Awake() // Start yerine Awake kullanmak daha güvenlidir.
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        // Eğer particle sistemi hala hayattaysa (çalıyorsa veya partikülleri hala aktifse)
        // hiçbir şey yapma.
        if (ps.IsAlive())
        {
            return;
        }

        // YENİ: Eğer particle sistemi artık "canlı" değilse, bu objeyi yok etmek yerine
        // kendi etiketiyle havuza geri gönder.
        ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
    }
}