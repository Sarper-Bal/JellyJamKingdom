using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    [CreateAssetMenu(fileName = "NewWaveSequence", menuName = "Engine 2.5D/Wave Sequence")]
    public class WaveSequence : ScriptableObject
    {
        [Header("Playlist")]
        [Tooltip("Sırasıyla oynatılacak dalga profilleri.")]
        public List<WaveProfile> waves;

        [Header("Ayarlar")]
        [Tooltip("Liste bittiğinde başa dönüp tekrar etsin mi?")]
        public bool loopSequence = true;

        [Tooltip("Her dalga bittiğinde bir sonraki başlamadan önce beklenecek süre (saniye).")]
        public float delayBetweenWaves = 5f;
    }
}