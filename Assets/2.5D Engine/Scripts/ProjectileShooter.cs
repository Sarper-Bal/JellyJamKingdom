using UnityEngine;

// YANLIŞ OLAN "Engine2_D" İSİMLENDİRMESİ DÜZELTİLDİ
namespace IndianOceanAssets.Engine2_5D
{
    public class ProjectileShooter : MonoBehaviour
    {
        [SerializeField] private GameObject projectilePrefab;

        public void FireAtPoint(Vector3 targetPoint)
        {
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            projectile.GetComponent<Projectile>().SetTarget(targetPoint);
            FlipTowards(targetPoint);
        }

        private void FlipTowards(Vector3 target)
        {
            if (target.x > transform.position.x)
                transform.localScale = new Vector3(1, 1, 1);
            else
                transform.localScale = new Vector3(-1, 1, 1);
        }
    }
}