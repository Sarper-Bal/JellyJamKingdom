/*
 * IAttackStateProvider (ARAYÜZ)
 * Bu bir component değildir, bir 'kontrat'tır.
 * AutoAttack script'i, PlayerController'a veya Tower'a değil,
 * bu arayüzü "implemente eden" (uygulayan) herhangi bir component'e bağlanır.
 */

namespace IndianOceanAssets.Engine2_5D
{
    /// <summary>
    /// AutoAttack component'ine, saldırının yapılıp yapılamayacağı
    /// (örn: hareket ediyor, stun yemiş vb.) bilgisini sağlayan arayüz.
    /// </summary>
    public interface IAttackStateProvider
    {
        /// <summary>
        /// AutoAttack'ın hedef aramasına ve ateş etmesine izin verilip verilmediğini döndürür.
        /// </summary>
        /// <returns>Eğer 'true' ise saldırı devam eder, 'false' ise durur.</returns>
        bool CanAttack();
    }
}