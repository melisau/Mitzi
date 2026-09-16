using UnityEngine;
using PawPath.Data;

namespace PawPath.Audio
{
    /// <summary>
    /// Mırıldama loop, mevsim ambiyansı, fırça ve baloncuk SFX.
    /// Klipler Inspector'dan atanır; yoksa sessizce atlanır.
    /// </summary>
    public class CozyAudioManager : MonoBehaviour
    {
        public static CozyAudioManager Instance { get; private set; }

        [SerializeField] AudioSource music;
        [SerializeField] AudioSource ambience;
        [SerializeField] AudioSource purr;
        [SerializeField] AudioSource sfx;

        [SerializeField] AudioClip musicLoop;
        [SerializeField] AudioClip summerAmbience;
        [SerializeField] AudioClip autumnAmbience;
        [SerializeField] AudioClip winterAmbience;
        [SerializeField] AudioClip defaultPurr;
        [SerializeField] AudioClip brushSfx;
        [SerializeField] AudioClip bubbleSfx;
        [SerializeField] AudioClip bowlSfx;
        [SerializeField] AudioClip meowSfx;

        void Awake()
        {
            Instance = this;
            music = Ensure(music, "Music", 0.28f, true);
            ambience = Ensure(ambience, "Ambience", 0.35f, true);
            purr = Ensure(purr, "Purr", 0.55f, true);
            sfx = Ensure(sfx, "Sfx", 0.8f, false);
            if (musicLoop != null)
            {
                music.clip = musicLoop;
                music.Play();
            }
        }

        AudioSource Ensure(AudioSource existing, string name, float volume, bool loop)
        {
            if (existing != null)
            {
                existing.loop = loop;
                existing.playOnAwake = false;
                existing.volume = volume;
                return existing;
            }

            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop = loop;
            src.playOnAwake = false;
            src.volume = volume;
            src.spatialBlend = 0f;
            return src;
        }

        public void PlaySeason(SeasonId season)
        {
            AudioClip clip = season switch
            {
                SeasonId.Winter => winterAmbience,
                SeasonId.Autumn => autumnAmbience,
                _ => summerAmbience
            };
            if (clip == null)
                return;
            ambience.clip = clip;
            ambience.Play();
        }

        public void StartPurr(AudioClip overrideClip)
        {
            purr.clip = overrideClip != null ? overrideClip : defaultPurr;
            if (purr.clip == null)
                return;
            if (!purr.isPlaying)
                purr.Play();
        }

        public void StopPurr()
        {
            if (purr.isPlaying)
                purr.Stop();
        }

        public void PlayBrush() => PlayOne(brushSfx);
        public void PlayBubble() => PlayOne(bubbleSfx);
        public void PlayBowl() => PlayOne(bowlSfx);
        public void PlayMeow() => PlayOne(meowSfx);

        void PlayOne(AudioClip clip)
        {
            if (clip == null || sfx == null)
                return;
            sfx.PlayOneShot(clip);
        }

        public void BindClips(AudioClip musicClip, AudioClip purrClip, AudioClip brush, AudioClip bubble)
        {
            musicLoop = musicClip;
            defaultPurr = purrClip;
            brushSfx = brush;
            bubbleSfx = bubble;
        }
    }
}
