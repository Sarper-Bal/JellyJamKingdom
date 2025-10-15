using UnityEngine;
using UnityEngine.InputSystem;

// Bu script'in çalışması için bir PlayerInput bileşenine ihtiyaç duyduğunu belirtiyoruz.
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{

    // Diğer script'lerin hareket verisini okuyabilmesi için bir "property" (özellik).
    // Dışarıdan sadece okunabilir (get), içeriden hem okunup hem yazılabilir (private set).
    public Vector2 MoveInput { get; private set; }

    // Diğer scriptlerin (PlayerController gibi) dinleyebileceği olaylar (events).
    // public event System.Action OnRollPerformed; // Sonraki adımlarda kullanacağız.
    // public event System.Action OnAttackPerformed; // Sonraki adımlarda kullanacağız.

    private PlayerInput playerInput;
    private InputAction moveAction;

    private void Awake()
    {
        // Player objesinin üzerindeki PlayerInput bileşenini bul ve referans al.
        playerInput = GetComponent<PlayerInput>();

        // PlayerInput bileşenine bağlı olan "InputSystem_Actions" asset'inden "Move" eylemini bul.
        moveAction = playerInput.actions["Move"];
    }

    private void Update()
    {
        // Her frame, "Move" eyleminden gelen Vector2 (x, y) verisini oku ve MoveInput özelliğine ata.
        // Sanal joystick'ten gelen veri buraya akacak.
        MoveInput = moveAction.ReadValue<Vector2>();
        {
            // Her frame, "Move" eyleminden gelen Vector2 (x, y) verisini oku ve MoveInput özelliğine ata.
            // Sanal joystick'ten gelen veri buraya akacak.
            MoveInput = moveAction.ReadValue<Vector2>();

            // YENİ EKLENEN SATIR: Joystick'ten gelen veriyi konsola yazdır.
            //Debug.Log("Joystick Verisi: " + MoveInput);
        }
    }
}