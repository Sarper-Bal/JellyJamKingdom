using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // This class makes the camera follow a target transform with a specified offset.
    public class CameraFollow : MonoBehaviour
    {
        // The target Transform that the camera will follow.
        [SerializeField] private Transform target;
        // The positional offset from the target.
        [SerializeField] private Vector3 offset;

        // Called once per frame.
        private void Update()
        {
            // If a target is assigned, smoothly move the camera towards the target's position plus the offset.
            if (target)
                transform.position = Vector3.Lerp(transform.position, target.position + offset, Time.deltaTime * 8f);
        }
    }
}