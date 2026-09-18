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
            // Eğer oyun Hub modunda değilse sevmeyi durdur
            if (GameFlow.Instance != null && !GameFlow.Instance.InHub)
            {
                StopPetting();
                return;
            }
              // --- YENİ KORUMA: Eğer fare bir butonun veya arayüzün üzerindeyse sevmeyi engelle ---
    if (UnityEngine.EventSystems.EventSystem.current != null && 
        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
    {
        StopPetting();
        return;
    }
    // ---------------------------------------------------------------------------------
  
            

            // Fareye veya ekrana basılmıyorsa sevmeyi durdur
            if (!PointerHeld())
            {
                StopPetting();
                return;
            }

            // Farenin oyun dünyasındaki koordinatını hesapla
            Vector2 world = ScreenToWorld(PointerScreen());
            
            // Fare kedi alanının (Collider) üstünde mi kontrol et
            bool over = col != null ? col.OverlapPoint(world) : Vector2.Distance(world, transform.position) <= petRadius;
            
            // Eğer faren kedi üzerindeyse sevmeyi başlat
            if (over)
            {
                if (!petting)
                    BeginPetting();

                tick += Time.deltaTime;
                if (tick >= loveTickInterval)
                {
                    tick = 0f;
                    
                    // TEST UYARISI: Kodun çalıştığını Unity alt panelinden görebilmek için:
                    Debug.Log("🎯 Kediyi başarıyla okşuyorsunuz! Kalpler uçuşuyor olmalı.");
                    
                    if (CozyEconomyManager.Instance != null)
                        CozyEconomyManager.Instance.AddLove(lovePerTick, GameText.PettingLove(cat));
                    Vibrate();
                }
            }
            else
            {
                // Fare kedi alanından çıkarsa efekti durdur
                StopPetting();
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
