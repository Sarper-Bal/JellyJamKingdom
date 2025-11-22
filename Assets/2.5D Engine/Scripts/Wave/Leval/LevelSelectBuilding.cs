using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // Yeni Input Sistemi kütüphanesi

namespace IndianOceanAssets.Engine2_5D
{
    public class LevelSelectBuilding : MonoBehaviour
    {
        [Header("Hedef Bölüm")]
        public LevelData levelToLoad;

        private void Update()
        {
            // Yeni Input Sistemi: Pointer (Fare veya Parmak) basıldı mı?
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                // Basılan noktanın pozisyonunu al
                Vector2 touchPosition = Pointer.current.position.ReadValue();
                DetectClick(touchPosition);
            }
        }

        private void DetectClick(Vector2 screenPos)
        {
            // 1. UI Kontrolü (Yeni Input System ile uyumlu olmalı)
            if (IsPointerOverUI())
            {
                return;
            }

            // 2. Kameradan ışın yolla
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            RaycastHit hit;

            // 3. Işın bize çarptı mı?
            if (Physics.Raycast(ray, out hit, 100f))
            {
                if (hit.transform == this.transform)
                {
                    OnBuildingClicked();
                }
            }
        }

        // UI'ya tıklanıp tıklanmadığını kontrol eden yardımcı metot
        private bool IsPointerOverUI()
        {
            // EventSystem varsa ve pointer bir UI objesi üzerindeyse
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }
            return false;
        }

        private void OnBuildingClicked()
        {
            Debug.Log($"<color=green>BİNAYA TIKLANDI: {gameObject.name}</color>");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadLevel(levelToLoad);
            }
            else
            {
                Debug.LogError("HATA: GameManager sahnede bulunamadı!");
            }
        }
    }
}