namespace IndianOceanAssets.Engine2_5D
{
    public interface ISaveable
    {
        // Veriyi paketle ve SaveManager'a teslim et
        object CaptureState();

        // SaveManager'dan gelen paketi aç ve kendine uygula
        void RestoreState(object state);
    }
}