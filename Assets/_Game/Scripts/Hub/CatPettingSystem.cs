using UnityEngine;
using PawPath.Audio;
using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;
using PawPath.Localization;

namespace PawPath.Hub
{
    /// <summary>
    /// Hub'da kedi üzerine parmak gezdirince mırıldama, kalp partikülü, titreşim ve Love Point üretir.
    /// </summary>
    public class CatPettingSystem : MonoBehaviour
    {
        [SerializeField] CatDefinition cat;
        [SerializeField] ParticleSystem hearts;
        [SerializeField] float petRadius = 1.15f;
        [SerializeField] float loveTickInterval = 0.55f;
        [SerializeField] int lovePerTick = 1;
        [SerializeField] Camera worldCamera;

        float tick;
        bool petting;
        Collider2D col;
        Vector3 lastPointerPosition;
   
        public CatDefinition Cat => cat;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        public void Bind(CatDefinition definition, ParticleSystem heartFx)
        {
            cat = definition;
            hearts = heartFx;
        }

            void Update()
    {
        if (GameFlow.Instance != null && !GameFlow.Instance.InHub)
        {
            StopPetting();
            return;
        }

        if (!PointerHeld())
        {
            StopPetting();
            return;
        }

        Vector2 world = ScreenToWorld(PointerScreen());
        bool over = col != null ? col.OverlapPoint(world) : Vector2.Distance(world, transform.position) <= petRadius;
        
        // --- YENİ KONTROL: Parmak kedi üzerinde hareket ediyor mu? ---
        Vector3 currentPointerPos = PointerScreen();
        float movementDistance = Vector3.Distance(currentPointerPos, lastPointerPosition);
        lastPointerPosition = currentPointerPos; // Bir sonraki kare için pozisyonu güncelle

        // Eğer oyuncu kedi üzerinde değilse VEYA elini yeterince oynatmıyorsa sevmeyi durdur
        // (Ekran çözünürlüğüne göre 1 pikselden az hareket varsa sabit duruyor demektir)
        if (!over || movementDistance < 1f)
        {
            StopPetting();
            return;
        }
        // --------------------------------------------------------------

        if (!petting)
            BeginPetting();

        tick += Time.deltaTime;
        if (tick >= loveTickInterval)
        {
            tick = 0f;
            if (CozyEconomyManager.Instance != null)
                CozyEconomyManager.Instance.AddLove(lovePerTick, GameText.PettingLove(cat));
            Vibrate();
        }
    }


        void BeginPetting()
        {
            petting = true;
            tick = loveTickInterval;
            if (hearts != null && !hearts.isPlaying)
                hearts.Play();
            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.StartPurr(cat != null ? cat.purrClip : null);
        }

        void StopPetting()
        {
            if (!petting)
                return;
            petting = false;
            tick = 0f;
            if (hearts != null && hearts.isPlaying)
                hearts.Stop();
            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.StopPurr();
        }

        static void Vibrate()
        {
#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }

        static Vector3 PointerScreen()
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
        }

        static bool PointerHeld()
        {
            if (Input.touchCount > 0)
                return true;
            return Input.GetMouseButton(0);
        }

        Vector2 ScreenToWorld(Vector3 screen)
        {
            var cam = worldCamera != null ? worldCamera : Camera.main;
            screen.z = Mathf.Abs(cam.transform.position.z);
            var p = cam.ScreenToWorldPoint(screen);
            p.z = 0f;
            return p;
        }
    }
}
