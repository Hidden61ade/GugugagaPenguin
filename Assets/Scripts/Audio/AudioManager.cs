using UnityEngine;

/// <summary>
/// 全局音频管理器：BGM 切换 + 道具音效轮播 + 标靶命中音效。
/// 不使用 DontDestroyOnLoad，场景重载时自动重建。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioClip menuBGM;
    [SerializeField] private AudioClip gameBGM;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;

    [Header("道具音效（交替播放）")]
    [SerializeField] private AudioClip itemSfx1;
    [SerializeField] private AudioClip itemSfx2;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    [Header("标靶命中音效")]
    [SerializeField] private AudioClip dartHitSfx;
    [SerializeField, Range(0f, 1f)] private float dartHitVolume = 1f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private bool playFirstSfx = true;

    private void Awake()
    {
        Instance = this;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
    }

    private void Start()
    {
        if (FlowControlClean.Instance != null)
            FlowControlClean.Instance.OnStateChanged += OnFlowStateChanged;
        PlayBGM(menuBGM);
    }

    private void OnFlowStateChanged(FlowControlClean.FlowState state)
    {
        switch (state)
        {
            case FlowControlClean.FlowState.Title:
                PlayBGM(menuBGM);
                playFirstSfx = true;
                break;
            case FlowControlClean.FlowState.Playing:
                PlayBGM(gameBGM);
                break;
            case FlowControlClean.FlowState.Victory:
                if (bgmSource != null && bgmSource.isPlaying)
                    bgmSource.volume = bgmVolume * 0.3f;
                break;
        }
    }

    public void PlayItemSfx()
    {
        AudioClip clip = playFirstSfx ? itemSfx1 : itemSfx2;
        playFirstSfx = !playFirstSfx;
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayDartHitSfx()
    {
        if (dartHitSfx != null) sfxSource.PlayOneShot(dartHitSfx, dartHitVolume);
    }

    public void PlayOneShotSfx(AudioClip clip, float volume = -1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume < 0 ? sfxVolume : volume);
    }

    private void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null) return;
        if (clip == null) { bgmSource.Stop(); return; }
        if (bgmSource.clip == clip && bgmSource.isPlaying) { bgmSource.volume = bgmVolume; return; }
        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }
}
