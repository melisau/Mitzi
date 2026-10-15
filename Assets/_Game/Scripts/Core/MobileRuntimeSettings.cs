using UnityEngine;

namespace PawPath.Core
{
    /// <summary>
    /// Sahne yüklenmeden önce mobil çalışma zamanı ayarlarını uygular.
    /// Sahneye eklenmesi gerekmez; Unity otomatik çağırır.
    /// </summary>
    public static class MobileRuntimeSettings
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            // Mobilde varsayılan 30 FPS çizim ve kamera takibini kasıntılı gösterir.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            // Oyuncu çizgi planlarken ekran kararmasın.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Yayın derlemesinde log yazımı CPU/GC harcamasın; uyarı ve hatalar kalsın.
            if (!Debug.isDebugBuild)
                Debug.unityLogger.filterLogType = LogType.Warning;
        }
    }
}
