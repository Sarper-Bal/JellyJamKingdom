/*
 * BUILDING VISUAL CONTROLLER - FIXED (Scale Correction)
 * DÜZELTME:
 * - Artık binaları zorla (1,1,1) boyutuna getirmez.
 * - Oyun başında (Awake) senin ayarladığın orijinal boyutları (Initial Scale) hafızaya alır.
 * - Animasyon sırasında hedef boyut olarak bu hafızadaki değeri kullanır.
 */

using UnityEngine;
using System.Collections.Generic;
using DG.Tweening; 

namespace IndianOceanAssets.Engine2_5D
{
    public class BuildingVisualController : MonoBehaviour
    {
        [Header("Modeller")]
        [Tooltip("Sırasıyla seviye görünümleri (Child objeler). 0: Temel, 1: Seviye 2...")]
        [SerializeField] private List<GameObject> visualModels;

        [Header("Efektler")]
        [Tooltip("Seviye atlayınca patlayacak efekt (Opsiyonel).")]
        [SerializeField] private ParticleSystem upgradeFx;

        private int currentIndex = -1;
        
        // --- YENİ: Orijinal Boyut Hafızası ---
        private List<Vector3> originalScales = new List<Vector3>();

        private void Awake()
        {
            // Başlangıçta tüm modellerin senin ayarladığın boyutlarını kaydet
            foreach (var model in visualModels)
            {
                if (model != null)
                {
                    originalScales.Add(model.transform.localScale);
                    
                    // Başlangıçta çakışma olmasın diye hepsini kapatabiliriz
                    // Ama NpcHousing.Start zaten SetVisualIndex çağıracak, o yüzden gerek yok.
                }
                else
                {
                    originalScales.Add(Vector3.one); // Güvenlik (Null ise)
                }
            }
        }

        public void SetVisualIndex(int targetIndex, bool animate)
        {
            if (targetIndex < 0 || targetIndex >= visualModels.Count) return;
            if (currentIndex == targetIndex) return;

            if (!animate)
            {
                SwitchImmediate(targetIndex);
            }
            else
            {
                SwitchWithAnimation(targetIndex);
            }

            currentIndex = targetIndex;
        }

        private void SwitchImmediate(int index)
        {
            for (int i = 0; i < visualModels.Count; i++)
            {
                if (visualModels[i] != null)
                {
                    bool isActive = (i == index);
                    visualModels[i].SetActive(isActive);
                    
                    // Aktif olanın boyutunu orijinal haline getir (Bozulma varsa düzelt)
                    if (isActive)
                    {
                        visualModels[i].transform.localScale = originalScales[i];
                    }
                }
            }
        }

        private void SwitchWithAnimation(int nextIndex)
        {
            // 1. ESKİ BİNAYI KÜÇÜLT (Squash)
            if (currentIndex != -1 && visualModels[currentIndex] != null)
            {
                GameObject oldModel = visualModels[currentIndex];
                
                // DOTween ile mevcut boyutundan 0'a küçült
                oldModel.transform.DOScale(Vector3.zero, 0.25f)
                    .SetEase(Ease.InBack) 
                    .OnComplete(() => oldModel.SetActive(false));
            }

            // 2. YENİ BİNAYI BÜYÜT (Elastic Pop)
            GameObject newModel = visualModels[nextIndex];
            // Hedef boyutumuz artık (1,1,1) değil, kaydedilen orijinal boyut!
            Vector3 targetScale = originalScales[nextIndex]; 

            if (newModel != null)
            {
                newModel.SetActive(true);
                newModel.transform.localScale = Vector3.zero; // Sıfırdan başla

                DOVirtual.DelayedCall(0.2f, () =>
                {
                    if (upgradeFx != null) upgradeFx.Play();
                    
                    // Hedef boyuta doğru jöle gibi büyü
                    newModel.transform.DOScale(targetScale, 0.6f)
                        .SetEase(Ease.OutElastic, 1.2f);
                });
            }
        }
    }
}