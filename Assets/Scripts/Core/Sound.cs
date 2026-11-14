using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PatiDostum
{
    /// <summary>
    /// Sesler ve müzik. Dosyalar Resources/Audio altında (tools/sentez_sesler.py üretir; dost sesleri
    /// SES_LISANSLARI.md). Sahneler arasında yaşayan tek nesne: efektler için küçük bir kaynak havuzu, müzik için
    /// döngüde çalan tek kaynak. Ses ve müzik ayrı ayrı kapatılabilir (cihaz ayarı, kayda girmez).
    /// Müzik sahneye göre seçilir: mini oyunda oyun müziği, diğerlerinde gündüz müziği; dost uyurken ninni
    /// (PetController ister).
    /// </summary>
    public class Sound : MonoBehaviour
    {
        private const string SfxKey = "sound_on", MusicKey = "music_on";
        private const float MusicVolume = 0.45f;
        private const int Voices = 8;

        private static Sound instance;
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource[] sources;
        private AudioSource music;
        private int next;
        private string musicName;
        private Coroutine fade;

        public static bool SfxOn
        {
            get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
            set { PlayerPrefs.SetInt(SfxKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
                PlayerPrefs.Save();
                if (instance != null) instance.music.volume = value ? MusicVolume : 0f;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("Sound");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Sound>();
            instance.sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                instance.sources[i] = go.AddComponent<AudioSource>();
                instance.sources[i].playOnAwake = false;
            }
            instance.music = go.AddComponent<AudioSource>();
            instance.music.loop = true;
            instance.music.playOnAwake = false;
            instance.music.volume = MusicOn ? MusicVolume : 0f;
            SceneManager.sceneLoaded += (scene, mode) => Music(scene.name == MiniGameHost.MiniGameScene ? "music_game" : "music_day");
        }

        /// <summary>Efekt çalar; pitch &lt; 0 ise hafif rastgele perde (aynı ses tekrarlanınca sıkmasın).</summary>
        public static void Play(string name, float volume = 1f, float pitch = -1f)
        {
            if (instance == null || !SfxOn) return;
            AudioClip clip = instance.Clip(name);
            if (clip == null) return;
            AudioSource source = instance.sources[instance.next];
            instance.next = (instance.next + 1) % Voices;
            source.pitch = pitch > 0f ? pitch : Random.Range(0.94f, 1.06f);
            source.PlayOneShot(clip, volume);
        }

        /// <summary>Dostun kendi sesi (miyav, hav, vak...).</summary>
        public static void PetVoice(string species, float volume = 0.9f) => Play("pet_" + species, volume, Random.Range(0.97f, 1.08f));

        /// <summary>Müziği değiştirir (aynısı çalıyorsa dokunmaz); kısa bir geçişle.</summary>
        public static void Music(string name)
        {
            if (instance == null || instance.musicName == name) return;
            instance.musicName = name;
            if (instance.fade != null) instance.StopCoroutine(instance.fade);
            instance.fade = instance.StartCoroutine(instance.Switch(instance.Clip(name)));
        }

        private IEnumerator Switch(AudioClip clip)
        {
            float target = MusicOn ? MusicVolume : 0f;
            for (float v = music.volume; music.isPlaying && v > 0f; v -= Time.unscaledDeltaTime * 2f)
            {
                music.volume = v;
                yield return null;
            }
            music.clip = clip;
            if (clip == null) yield break;
            music.Play();
            for (float v = 0f; v < target; v += Time.unscaledDeltaTime * 1.5f)
            {
                music.volume = v;
                yield return null;
            }
            music.volume = MusicOn ? MusicVolume : 0f;
        }

        private AudioClip Clip(string name)
        {
            if (!clips.TryGetValue(name, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>("Audio/" + name);
                clips[name] = clip;
            }
            return clip;
        }
    }
}
