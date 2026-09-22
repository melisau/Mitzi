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
        const string MusicMutedKey = "PawPath.Audio.MusicMuted";
        const string EffectsMutedKey = "PawPath.Audio.EffectsMuted";
        public static CozyAudioManager Instance { get; private set; }

        public bool MusicMuted { get; private set; }
        public bool EffectsMuted { get; private set; }

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
        AudioClip homeMusic;
        AudioClip streetMusic;
        AudioClip forestMusic;
        AudioClip cityMusic;
        AudioClip stepSfx;
        AudioClip[] meowVariants;
        AudioClip hurtSfx;
        AudioClip fallSfx;
        AudioClip rescueSfx;
        AudioClip errorSfx;
        AudioClip uiClickSfx;
        AudioClip uiConfirmSfx;
        AudioClip shopBuySfx;
        AudioClip shopSellSfx;

        void Awake()
        {
            Instance = this;
            music = Ensure(music, "Music", 0.28f, true);
            ambience = Ensure(ambience, "Ambience", 0.35f, true);
            purr = Ensure(purr, "Purr", 0.55f, true);
            sfx = Ensure(sfx, "Sfx", 0.8f, false);
            MusicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
            EffectsMuted = PlayerPrefs.GetInt(EffectsMutedKey, 0) == 1;
            ApplyMuteState();
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
        public void PlayStep() => PlayOne(stepSfx, 0.34f);
        public void PlayHurt() => PlayOne(hurtSfx);
        public void PlayFall() => PlayOne(fallSfx);
        public void PlayRescue() => PlayOne(rescueSfx);
        public void PlayError() => PlayOne(errorSfx);
        public void PlayUiClick() => PlayOne(uiClickSfx, 0.65f);
        public void PlayConfirm() => PlayOne(uiConfirmSfx);
        public void PlayShopBuy() => PlayOne(shopBuySfx);
        public void PlayShopSell() => PlayOne(shopSellSfx);

        public void PlayRandomMeow()
        {
            if (meowVariants == null || meowVariants.Length == 0)
            {
                PlayMeow();
                return;
            }
            PlayOne(meowVariants[Random.Range(0, meowVariants.Length)]);
        }

        public void ToggleMusic()
        {
            MusicMuted = !MusicMuted;
            PlayerPrefs.SetInt(MusicMutedKey, MusicMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMuteState();
        }

        public void ToggleEffects()
        {
            EffectsMuted = !EffectsMuted;
            PlayerPrefs.SetInt(EffectsMutedKey, EffectsMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMuteState();
        }

        void ApplyMuteState()
        {
            if (music != null) music.mute = MusicMuted;
            if (ambience != null) ambience.mute = MusicMuted;
            if (sfx != null) sfx.mute = EffectsMuted;
            if (purr != null) purr.mute = EffectsMuted;
        }

        public void PlayHomeMusic() => SwitchMusic(homeMusic);

        public void PlayThemeMusic(int theme)
        {
            AudioClip clip = theme == 2 || theme == 3 ? cityMusic : theme == 1 ? forestMusic : streetMusic;
            SwitchMusic(clip);
        }

        void SwitchMusic(AudioClip clip)
        {
            if (clip == null || music == null || music.clip == clip)
                return;
            music.clip = clip;
            music.Play();
        }

        void PlayOne(AudioClip clip)
        {
            if (clip == null || sfx == null)
                return;
            sfx.PlayOneShot(clip);
        }

        void PlayOne(AudioClip clip, float volumeScale)
        {
            if (clip == null || sfx == null)
                return;
            sfx.PlayOneShot(clip, volumeScale);
        }

        public void BindLibrary(PawPathCatalog catalog)
        {
            if (catalog == null)
                return;
            homeMusic = catalog.homeMusic;
            streetMusic = catalog.streetMusic;
            forestMusic = catalog.forestMusic;
            cityMusic = catalog.cityMusic;
            stepSfx = catalog.catStepSfx;
            meowVariants = catalog.catMeowSfx;
            hurtSfx = catalog.catHurtSfx;
            fallSfx = catalog.catFallSfx;
            rescueSfx = catalog.catRescueSfx;
            errorSfx = catalog.errorSfx;
            uiClickSfx = catalog.uiClickSfx;
            uiConfirmSfx = catalog.uiConfirmSfx;
            shopBuySfx = catalog.shopBuySfx;
            shopSellSfx = catalog.shopSellSfx;
            PlayHomeMusic();
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
