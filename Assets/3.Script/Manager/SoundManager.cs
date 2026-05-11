using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [Header("Audio")]
    [SerializeField, KoreanLabel("BGM 오디오 소스")] private AudioSource bgmSource;
    [SerializeField, KoreanLabel("SFX 풀 크기")] private int sfxPoolSize = 12;
    [SerializeField, KoreanLabel("BGM 볼륨")] private float bgmVolume = 0.5f;
    [SerializeField, KoreanLabel("SFX 볼륨")] private float sfxVolume = 1f;

    private const string BgmResourcePath = "Sound/BGM";
    private const string SfxResourcePath = "Sound/SFX";
    private const string FootstepResourcePath = "Sound/Footstep";

    private readonly Dictionary<BGMType, AudioClip> bgmMap = new();
    private readonly Dictionary<SFXType, AudioClip> sfxMap = new();
    private readonly List<AudioClip> footstepClips = new();

    private AudioSource[] sfxSources;
    private int sfxIndex;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        InitAudioSources();
        LoadAllBGM();
        LoadAllSFX();
        LoadAllFootsteps();
    }

    private void InitAudioSources()
    {
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.spatialBlend = 0f;

        sfxSources = new AudioSource[Mathf.Max(1, sfxPoolSize)];
        Transform poolRoot = new GameObject("SFXPoolRoot").transform;
        poolRoot.SetParent(transform);
        poolRoot.localPosition = Vector3.zero;

        for (int i = 0; i < sfxSources.Length; i++)
        {
            GameObject child = new GameObject($"SFXSource_{i:00}");
            child.transform.SetParent(poolRoot);
            child.transform.localPosition = Vector3.zero;

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = sfxVolume;
            source.spatialBlend = 0f;
            sfxSources[i] = source;
        }
    }

    // Resources/Sound/BGM 안의 BGM_Combat 같은 이름을 enum과 자동 매칭한다.
    private void LoadAllBGM()
    {
        bgmMap.Clear();
        AudioClip[] clips = Resources.LoadAll<AudioClip>(BgmResourcePath);

        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];
            if (clip == null)
                continue;

            if (Enum.TryParse(RemovePrefix(clip.name, "BGM_"), out BGMType type) && !bgmMap.ContainsKey(type))
                bgmMap.Add(type, clip);
        }
    }

    // Resources/Sound/SFX 안의 SFX_Hit 같은 이름을 enum과 자동 매칭한다.
    private void LoadAllSFX()
    {
        sfxMap.Clear();
        AudioClip[] clips = Resources.LoadAll<AudioClip>(SfxResourcePath);

        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];
            if (clip == null)
                continue;

            if (Enum.TryParse(RemovePrefix(clip.name, "SFX_"), out SFXType type) && !sfxMap.ContainsKey(type))
                sfxMap.Add(type, clip);
        }
    }

    private void LoadAllFootsteps()
    {
        footstepClips.Clear();
        footstepClips.AddRange(Resources.LoadAll<AudioClip>(FootstepResourcePath));
    }

    public void PlayBGM(BGMType type)
    {
        if (!bgmMap.TryGetValue(type, out AudioClip clip) || clip == null)
            return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PlaySFX(SFXType type, float volumeScale = 1f)
    {
        if (!sfxMap.TryGetValue(type, out AudioClip clip) || clip == null)
            return;

        GetNextSFXSource().PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayFootstep(float volumeScale = 0.5f)
    {
        if (footstepClips.Count == 0)
            return;

        AudioClip clip = footstepClips[UnityEngine.Random.Range(0, footstepClips.Count)];
        GetNextSFXSource().PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    private AudioSource GetNextSFXSource()
    {
        AudioSource source = sfxSources[sfxIndex];
        sfxIndex = (sfxIndex + 1) % sfxSources.Length;
        return source;
    }

    private static string RemovePrefix(string source, string prefix)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(prefix))
            return source;

        return source.StartsWith(prefix, StringComparison.Ordinal)
            ? source.Substring(prefix.Length)
            : source;
    }
}
