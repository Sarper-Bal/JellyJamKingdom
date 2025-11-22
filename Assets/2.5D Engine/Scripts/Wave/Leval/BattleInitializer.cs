using UnityEngine;
using System.Collections.Generic;
using IndianOceanAssets.Engine2_5D;

[DefaultExecutionOrder(-1)] 
public class BattleInitializer : MonoBehaviour
{
    [Header("Sahne Referansları")]
    [Tooltip("Sahnede duran Player objesini buraya sürükle.")]
    [SerializeField] private PlayerStats sceneHero; 
    
    [SerializeField] private WaveSequenceController waveController;

    [Header("Kuleler")]
    [Tooltip("Sahnede yerleştirdiğin kuleleri buraya sürükle (Sırası LevelData ile aynı olmalı).")]
    [SerializeField] private List<PlayerStats> sceneTowers; 

    private void Start()
    {
        // 1. Data Kontrolü
        if (GameManager.Instance == null || GameManager.Instance.PendingLevelData == null)
        {
            Debug.LogWarning("BattleInitializer: GameManager verisi yok! Test modunda çalışıyor olabilir.");
            // Test için sahnedeki player kendi varsayılan ayarlarıyla devam eder.
            return; 
        }

        LevelData data = GameManager.Instance.PendingLevelData;
        InitializeBattle(data);
    }

    private void InitializeBattle(LevelData data)
    {
        Debug.Log($"<color=green>BattleInitializer: {data.sceneName} verisi enjekte ediliyor...</color>");

        // --- ADIM A: HERO VERİSİNİ YÜKLE ---
        if (sceneHero != null)
        {
            // Dışarıdan gelen kahraman özelliklerini sahnedeki karaktere yükle
            sceneHero.Initialize(data.heroStats);
            
            // Kamerayı da garanti olsun diye tekrar bu karaktere kilitle
            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.SetTarget(sceneHero.transform);
            }
        }
        else
        {
            Debug.LogError("BattleInitializer: 'Scene Hero' atanmamış! Lütfen Inspector'dan sahnedeki Player'ı sürükleyin.");
        }

        // --- ADIM B: KULE VERİLERİNİ YÜKLE ---
        for (int i = 0; i < sceneTowers.Count; i++)
        {
            // Data listesinde bu kule için karşılık gelen bir veri var mı?
            if (data.towerStats != null && i < data.towerStats.Count)
            {
                if (sceneTowers[i] != null)
                {
                    sceneTowers[i].Initialize(data.towerStats[i]);
                }
            }
        }

        // --- ADIM C: DALGAYI BAŞLAT ---
        if (waveController != null && data.levelWaves != null)
        {
            waveController.InitializeFromExternal(data.levelWaves);
        }
    }
}