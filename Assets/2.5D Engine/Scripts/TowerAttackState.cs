/*
 * TOWER ATTACK STATE
 * Bu component, Savunma Kuleleri gibi sabit yapılara eklenir.
 * 'IAttackStateProvider' arayüzünü uygular ve 'AutoAttack' component'ine
 * her zaman saldırabileceği bilgisini verir.
 */

using UnityEngine;

namespace IndianOceanAssets.Engine2_5D
{
    // 'IAttackStateProvider' arayüzünü uyguluyoruz (implemente ediyoruz).
    public class TowerAttackState : MonoBehaviour, IAttackStateProvider
    {
        /// <summary>
        /// IAttackStateProvider arayüzünden gelen zorunlu metot.
        /// AutoAttack'a saldırıp saldıramayacağını söyler.
        /// </summary>
        /// <returns>Her zaman 'true'</returns>
        public bool CanAttack()
        {
            // Bir kule her zaman saldırabilir.
            // (Gelecekte buraya 'isStunned' (sersemlemiş) gibi bir kontrol eklenebilir)
            return true;
        }
    }
}