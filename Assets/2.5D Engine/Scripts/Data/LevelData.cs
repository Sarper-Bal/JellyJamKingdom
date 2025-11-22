using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.Engine2_5D
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Engine 2.5D/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Sahne Ayarı")]
        [Tooltip("Build Settings'deki sahne adı ile birebir aynı olmalı.")]
        public string sceneName; 

        [Header("Kahraman Verisi")]
        [Tooltip("Bu levelda oyuncunun başlangıç statları.")]
        public PlayerStatsData heroStats; 

        [Header("Kule Verileri")]
        [Tooltip("Sahnede yerleştirdiğin kulelerin sırasıyla statları. (1. Data -> 1. Kule vb.)")]
        public List<PlayerStatsData> towerStats; 

        [Header("Düşman Dalgası")]
        [Tooltip("Bu levelda oynatılacak dalga listesi (Playlist).")]
        public WaveSequence levelWaves; 
    }
}