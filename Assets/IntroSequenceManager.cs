// IntroSequenceManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class IntroSequenceManager : MonoBehaviour
{
    [Header("Integration")]
    [SerializeField] private DeathExperienceManager deathExperienceManager;
    
    [Header("UI Elements")]
    [SerializeField] private Canvas introCanvas;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI explanationText;
    
    [Header("Dramatic Intro")]
    [SerializeField] private AudioClip heartbeatSound;
    [SerializeField] private AudioClip flatlineSound;
    [SerializeField] private AudioClip ascensionSound;
    [SerializeField] private float heartbeatSlowdownDuration = 5.0f;
    [SerializeField] private float flatlineDuration = 3.0f;
    [SerializeField] private float blackoutDuration = 4.0f;
    [SerializeField] private float ascensionDuration = 8.0f;
    
    [Header("Components Reference")]
    [SerializeField] private StarFieldManager starFieldManager;
    [SerializeField] private VisualEffectsController visualEffects;
    [SerializeField] private SpatialAudioManager audioManager;
    
    private AudioSource heartbeatSource;
    private AudioSource effectsSource;
    private bool experienceStarted = false;
    
    void Start()
    {
        // オーディオソースのセットアップ
        SetupAudioSources();
        
        // UIの初期設定
        if (introCanvas != null)
        {
            introCanvas.gameObject.SetActive(true);
            PositionCanvasInFrontOfCamera(introCanvas);
        }
        
        // テキストの設定
        if (titleText != null)
        {
            titleText.text = "Near-Death Experience";
        }
        
        if (explanationText != null)
        {
            explanationText.text = "Experience a journey beyond the physical world. Press Start when you're ready.";
        }
        
        // スタートボタンの設定
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartDramaticSequence);
        }
    }
    
    private void SetupAudioSources()
    {
        // 心拍音用のオーディオソース
        GameObject heartbeatObj = new GameObject("HeartbeatSource");
        heartbeatObj.transform.SetParent(transform);
        heartbeatSource = heartbeatObj.AddComponent<AudioSource>();
        heartbeatSource.loop = true;
        heartbeatSource.spatialBlend = 0f; // 2D音響
        heartbeatSource.clip = heartbeatSound;
        
        // 効果音用のオーディオソース
        GameObject effectsObj = new GameObject("EffectsSource");
        effectsObj.transform.SetParent(transform);
        effectsSource = effectsObj.AddComponent<AudioSource>();
        effectsSource.loop = false;
        effectsSource.spatialBlend = 0f; // 2D音響
    }
    
    public void StartDramaticSequence()
    {
        if (experienceStarted) return;
        experienceStarted = true;
        
        // スタート画面を非表示
        if (introCanvas != null)
        {
            introCanvas.gameObject.SetActive(false);
        }
        
        // 導入シーケンスを開始
        StartCoroutine(DramaticIntroSequence());
    }
    
    private IEnumerator DramaticIntroSequence()
    {
        // カメラの準備
        Camera mainCamera = Camera.main;
        
        // DeathExperienceManager側の処理を一時停止
        if (deathExperienceManager != null)
        {
            // 既に実行中の場合は停止
            if (deathExperienceManager.IsRunning())
            {
                deathExperienceManager.EndExperience();
            }
        }
        
        // 視覚効果の初期化
        if (visualEffects != null)
        {
            visualEffects.InitializeEffects();
        }
        
        // 1. 心拍音の低下
        if (heartbeatSound != null && heartbeatSource != null)
        {
            heartbeatSource.clip = heartbeatSound;
            heartbeatSource.pitch = 1.0f;
            heartbeatSource.volume = 0.8f;
            heartbeatSource.Play();
            
            // 徐々に心拍音を遅くする
            float startTime = Time.time;
            while (Time.time < startTime + heartbeatSlowdownDuration)
            {
                float progress = (Time.time - startTime) / heartbeatSlowdownDuration;
                heartbeatSource.pitch = Mathf.Lerp(1.0f, 0.4f, progress);
                yield return null;
            }
            
            heartbeatSource.Stop();
        }
        
        // 2. フラットライン
        if (flatlineSound != null && effectsSource != null)
        {
            effectsSource.clip = flatlineSound;
            effectsSource.Play();
            
            // フラットライン効果を視覚的に表現
            if (visualEffects != null)
            {
                visualEffects.FlashScreen(Color.white, 0.5f);
            }
            
            yield return new WaitForSeconds(flatlineDuration);
        }
        
        // 3. 暗転
        if (visualEffects != null)
        {
            visualEffects.FadeToBlack(1.5f);
        }
        
        yield return new WaitForSeconds(blackoutDuration);
        
        // 4. 光への浮遊
        if (ascensionSound != null && effectsSource != null)
        {
            effectsSource.clip = ascensionSound;
            effectsSource.Play();
        }
        
        // 星空の初期化
        if (starFieldManager != null)
        {
            starFieldManager.InitializeStars();
        }
        
        // 暗闇から光への移行
        if (visualEffects != null)
        {
            visualEffects.FadeFromBlackToWhite(3.0f);
        }
        
        // 上昇感の演出
        float startAscensionTime = Time.time;
        if (mainCamera != null)
        {
            // カメラの元の向きを保存
            Quaternion originalRotation = mainCamera.transform.rotation;
            
            // 星が下から上へ流れる効果
            while (Time.time < startAscensionTime + ascensionDuration)
            {
                float progress = (Time.time - startAscensionTime) / ascensionDuration;
                
                // 少しずつ上を向かせる
                float upTilt = Mathf.Lerp(0, 30, progress);
                mainCamera.transform.rotation = Quaternion.Euler(upTilt, 0, 0);
                
                yield return null;
            }
        }
        
        // 完了後、DeathExperienceManagerのフェーズ5（ホワイトアウト）を開始
        if (deathExperienceManager != null)
        {
            deathExperienceManager.StartExperienceAtPhase(5);
        }
    }
    
    private void PositionCanvasInFrontOfCamera(Canvas canvas)
    {
        if (canvas == null) return;
        
        // メインカメラを取得
        Camera camera = Camera.main;
        if (camera == null) return;
        
        // ワールドスペースキャンバスに設定
        canvas.renderMode = RenderMode.WorldSpace;
        
        // カメラの前方1.5メートルに配置
        canvas.transform.position = camera.transform.position + camera.transform.forward * 1.5f;
        
        // カメラの向きに合わせる
        canvas.transform.rotation = Quaternion.LookRotation(
            canvas.transform.position - camera.transform.position
        );
        
        // サイズ調整
        RectTransform rectTransform = canvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(1.6f, 0.9f);
        }
    }
    
    void Update()
    {
        // 必要に応じてUIの位置を更新（ユーザーが移動した時のため）
        if (!experienceStarted && introCanvas != null)
        {
            PositionCanvasInFrontOfCamera(introCanvas);
        }
    }
    
    void OnDestroy()
    {
        // ボタンイベントの解除
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartDramaticSequence);
        }
    }
}