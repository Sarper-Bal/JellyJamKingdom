namespace IndianOceanAssets.Engine2_5D
{
    public interface IResourceProvider
    {
        /// <summary>
        /// Depoda bu kaynaktan kaç tane var?
        /// </summary>
        int GetStoredAmount(ResourceData resource);

        /// <summary>
        /// Depodan belirtilen miktarı düşer ve düşülen miktarı döner.
        /// </summary>
        int TakeResource(ResourceData resource, int amountToTake);
    }
}