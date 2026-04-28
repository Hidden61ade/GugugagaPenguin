using UnityEngine;

/// <summary>
/// 全局音频管理器：
/// - 主菜单 BGM 和游戏 BGM 自动切换
/// - 道具音效两个轮播（碰第一个道具播 sfx1，碰第二个播 sfx2，如此交替）
/// 
/// Inspector 中拖入对应 AudioClip 即可，留空则不播放。
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

    // ── 内部 ──
    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private bool playFirstSfx = true; // 下一次播放 sfx1？

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 创建两个 AudioSource：一个循环播 BGM，一个播一次性音效
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
        // 订阅状态变化
        if (FlowControlClean.Instance != null)
        {
            FlowControlClean.Instance.OnStateChanged += OnFlowStateChanged;
        }

        // 初始播放菜单 BGM
        PlayBGM(menuBGM);
    }

    private void OnDestroy()
    {
        if (FlowControlClean.Instance != null)
        {
            FlowControlClean.Instance.OnStateChanged -= OnFlowStateChanged;
        }
    }

    private void OnFlowStateChanged(FlowControlClean.FlowState state)
    {
        switch (state)
        {
            case FlowControlClean.FlowState.Title:
                PlayBGM(menuBGM);
                playFirstSfx = true; // 重新开始轮播
                break;

            case FlowControlClean.FlowState.Playing:
                PlayBGM(gameBGM);
                break;

            case FlowControlClean.FlowState.Victory:
                // Victory 时可以停 BGM 或保持，这里淡出
                if (bgmSource.isPlaying)
                    bgmSource.volume = bgmVolume * 0.3f;
                break;
        }
    }

    // ── 公开接口 ──

    /// <summary>
    /// 播放道具拾取音效（两个音效交替）。
    /// 由企鹅碰撞逻辑调用。
    /// </summary>
    public void PlayItemSfx()
    {
        AudioClip clip = playFirstSfx ? itemSfx1 : itemSfx2;
        playFirstSfx = !playFirstSfx;

        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    /// <summary>
    /// 播放标靶命中音效。
    /// </summary>
    public void PlayDartHitSfx()
    {
        if (dartHitSfx != null)
            sfxSource.PlayOneShot(dartHitSfx, dartHitVolume);
    }

    /// <summary>
    /// 播放任意一次性音效。
    /// </summary>
    public void PlayOneShotSfx(AudioClip clip, float volume = -1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume < 0 ? sfxVolume : volume);
    }

    // ── 内部 ──

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            bgmSource.Stop();
            return;
        }

        // 已经在播同一首就不重复
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            bgmSource.volume = bgmVolume;
            return;
        }

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }
}
