using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [Header("Audio")]
    [SerializeField, KoreanLabel("BGM 오디오 소스")] private AudioSource bgmSource;
    [SerializeField, KoreanLabel("SFX 풀 크기")] private int sfxPoolSize = 12;
    [SerializeField, KoreanLabel("BGM 볼륨")] private float bgmVolume = 0.2f;
    [SerializeField, KoreanLabel("SFX 볼륨")] private float sfxVolume = 1f;

    private const string BgmResourcePath = "Sound/BGM";
    private const string SfxResourcePath = "Sound/SFX";
    private const string FootstepResourcePath = "Sound/Footstep";

    private readonly Dictionary<BGMType, AudioClip> bgmMap = new();
    private readonly Dictionary<SFXType, AudioClip> sfxMap = new();
    private readonly Dictionary<string, AudioClip> namedSfxMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<AudioClip> footstepClips = new();
    private readonly List<AudioClip> bladeHitClips = new();
    private readonly List<AudioClip> dashAttackHitClips = new();
    private readonly List<AudioClip> boxHitClips = new();

    public const string SfxWeaponSwing = "weapon-sound";
    public const string SfxDash = "etc-sound-dash";
    public const string SfxJump = "etc-sound-jump";
    public const string SfxDashAttackStart = "beatt-sound-dashAttack";
    public const string SfxSwordAreaAttack = "beatt-sound-SwordAreaAttack";
    public const string SfxSlamShockwave = "beatt-sound-SlamShockwave";
    public const string SfxRisingSlashStart = "beatt-sound2";
    public const string SfxFocusMode = "focusMode";
    public const string SfxGuard = "Guard";
    public const string SfxGuardParry = "Guard1";
    public const string SfxGuardBlock = "Guard2";
    public const string SfxBuffPickup = "buff_1";
    public const string SfxPlayerHit = "hitting";

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
        namedSfxMap.Clear();
        bladeHitClips.Clear();
        dashAttackHitClips.Clear();
        boxHitClips.Clear();

        AudioClip[] clips = Resources.LoadAll<AudioClip>(SfxResourcePath);

        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];
            if (clip == null)
                continue;

            CacheNamedSFX(clip);

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

    public void PlayWeaponSwing(float volumeScale = 1f)
    {
        PlayNamedSFX(SfxWeaponSwing, SFXType.Attack, volumeScale);
    }

    public void PlayRandomBladeHit(float volumeScale = 1f)
    {
        if (bladeHitClips.Count == 0)
        {
            PlaySFX(SFXType.Hit, volumeScale);
            return;
        }

        AudioClip clip = bladeHitClips[UnityEngine.Random.Range(0, bladeHitClips.Count)];
        GetNextSFXSource().PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayRandomDashAttackHit(float volumeScale = 1f)
    {
        if (dashAttackHitClips.Count == 0)
        {
            PlayRandomBladeHit(volumeScale);
            return;
        }

        AudioClip clip = dashAttackHitClips[UnityEngine.Random.Range(0, dashAttackHitClips.Count)];
        GetNextSFXSource().PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayRandomBoxHit(float volumeScale = 1f)
    {
        if (boxHitClips.Count == 0)
        {
            PlaySFX(SFXType.Break, volumeScale);
            return;
        }

        AudioClip clip = boxHitClips[UnityEngine.Random.Range(0, boxHitClips.Count)];
        GetNextSFXSource().PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayDash(float volumeScale = 1f)
    {
        PlayNamedSFX(SfxDash, SFXType.Dash, volumeScale);
    }

    public void PlayJump(float volumeScale = 1f)
    {
        PlayNamedSFX(SfxJump, SFXType.UI, volumeScale);
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

    // enum 이름으로 표현하기 어려운 하이픈 파일명 SFX를 직접 캐시한다.
    private void CacheNamedSFX(AudioClip clip)
    {
        namedSfxMap[clip.name] = clip;
        if (string.Equals(clip.name, SfxFocusMode, StringComparison.OrdinalIgnoreCase))
            namedSfxMap["foucsMode"] = clip;

        if (!string.Equals(clip.name, SfxDashAttackStart, StringComparison.OrdinalIgnoreCase) &&
            clip.name.StartsWith("beatt-sound-dashAttack", StringComparison.OrdinalIgnoreCase))
        {
            dashAttackHitClips.Add(clip);
            return;
        }

        if (string.Equals(clip.name, "beatt-sound1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(clip.name, "beatt-sound2", StringComparison.OrdinalIgnoreCase))
        {
            bladeHitClips.Add(clip);
            return;
        }

        if (clip.name.StartsWith("be-att-sound", StringComparison.OrdinalIgnoreCase))
        {
            boxHitClips.Add(clip);
            return;
        }

    }

    // 파일명이 enum 규칙을 따르지 않는 리소스는 이름으로 바로 재생한다.
    // 해당 파일이 없으면 fallback enum 사운드로 빠져서 누락 리소스 때문에 기능이 멈추지 않게 한다.
    public void PlayNamedSFX(string clipName, SFXType fallbackType, float volumeScale = 1f)
    {
        if (!string.IsNullOrWhiteSpace(clipName) && namedSfxMap.TryGetValue(clipName, out AudioClip clip) && clip != null)
        {
            GetNextSFXSource().PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            return;
        }

        PlaySFX(fallbackType, volumeScale);
    }
}
