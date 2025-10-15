using UnityEngine;

// Bu script'in çalışması için bir ParticleSystem bileşenine ihtiyaç duyduğunu belirtiyoruz.
[RequireComponent(typeof(ParticleSystem))]
public class AutoDestroyParticle : MonoBehaviour
{
    private ParticleSystem ps;

    private void Start()
    {
        // ParticleSystem bileşenini al.
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

        // Eğer particle sistemi artık "canlı" değilse, bu objeyi yok et.
        Destroy(gameObject);
    }
}