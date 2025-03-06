// DeathExperienceManager.cs
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// 臨死体験全体を管理するメインマネージャークラス
/// </summary>
public class DeathExperienceManager : MonoBehaviour
{
    [Header("Experience Settings")]
    [Tooltip("体験の合計時間（秒）")]
    public float totalDuration = 180f; // 3分間
    [Tooltip("フェードイン時間（秒）")]
    public float fadeInDuration = 2.0f;
    [Tooltip("フェードアウト時間（秒）")]
    public float fadeOutDuration = 2.0f;
    [Tooltip("アプリ開始から体験開始までの遅延時間（秒）")]
    public float autoStartDelay = 5.0f;
    
    [Header("Debugging")]
    [Tooltip("デバッグモードを有効にする")]
    public bool debugMode = false;
    [Tooltip("特定フェーズからスタート（デバッグ用）")]
    [Range(1, 6)]
    public int debugStartPhase = 1;
    
    [Header("Components")]
    [Tooltip("星フィールドマネージャー")]
    public StarFieldManager starField;
    [Tooltip("ビジュアルエフェクトコントローラー")]
    public VisualEffectsController visualEffects;
    [Tooltip("立体音響マネージャー")]
    public SpatialAudioManager audioManager;
    [Tooltip("振動コントローラー")]
    public VibrationController vibrationController;
    [Tooltip("使用するカメラ")]
    public Camera mainCamera;
    
    // フェーズ管理
    private float startTime;
    private bool isExperienceRunning = false;
    private int currentPhase = 0;
    private float phaseProgress = 0f;
    private bool isInitialized = false;
    
    // カメラキャリブレーション用
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    
    // 各フェーズの時間配分（秒）
    private float[] phaseDurations = new float[] { 
        0f,   // フェーズ0: 準備
        30f,  // フェーズ1: 静寂の宇宙
        30f,  // フェーズ2: 変化の兆し
        30f,  // フェーズ3: 世界の崩壊
        30f,  // フェーズ4: 臨死ピーク
        30f,  // フェーズ5: ホワイトアウト
        30f   // フェーズ6: エンディング
    };
    
    // Unity ライフサイクル
    void Awake()
    {
        // 素早く初期化して視覚的な不具合を避ける
        InitializeComponents();
    }
    
    void Start()
    {
        // コンポーネントの完全初期化
        StartCoroutine(LateInitialize());
    }
    
    void Update()
    {
        if (isExperienceRunning && isInitialized)
        {
            UpdateExperience();
        }
        
        // デバッグコントロール
        if (debugMode)
        {
            HandleDebugControls();
        }
    }
    
    /// <summary>
    /// 遅延初期化（全コンポーネントが確実に存在するようにする）
    /// </summary>
    private IEnumerator LateInitialize()
    {
        yield return new WaitForSeconds(0.1f);
        
        // 全てのコンポーネントを初期化
        InitializeComponents();
        
        isInitialized = true;
        
        // デバッグモードの場合、自動的に開始
        if (debugMode)
        {
            StartExperienceAtPhase(debugStartPhase);
        }
        else
        {
            // 5秒後に自動的に体験開始
            StartCoroutine(AutoStartExperience());
        }
    }
    
    /// <summary>
    /// 遅延後に自動的に体験を開始するコルーチン
    /// </summary>
    private IEnumerator AutoStartExperience()
    {
        Debug.Log($"{autoStartDelay}秒後に体験を自動開始します...");
        yield return new WaitForSeconds(autoStartDelay);
        StartExperience();
    }
    
    /// <summary>
    /// 必要なコンポーネントを初期化
    /// </summary>
    private void InitializeComponents()
    {
        // コンポーネントが設定されていなければ検索
        if (starField == null) starField = GetComponent<StarFieldManager>();
        if (visualEffects == null) visualEffects = GetComponent<VisualEffectsController>();
        if (audioManager == null) audioManager = GetComponent<SpatialAudioManager>();
        if (vibrationController == null) vibrationController = GetComponent<VibrationController>();
        if (mainCamera == null) mainCamera = Camera.main;
        
        // 不足コンポーネントの警告
        if (starField == null) Debug.LogWarning("StarFieldManagerが見つかりません");
        if (visualEffects == null) Debug.LogWarning("VisualEffectsControllerが見つかりません");
        if (audioManager == null) Debug.LogWarning("SpatialAudioManagerが見つかりません");
        if (vibrationController == null) Debug.LogWarning("VibrationControllerが見つかりません");
        if (mainCamera == null) Debug.LogWarning("カメラが見つかりません");
        
        // カメラのキャリブレーションを実行
        CalibrateCamera();
    }
    
    /// <summary>
    /// カメラの位置と向きをキャリブレーション
    /// </summary>
    private void CalibrateCamera()
    {
        if (mainCamera == null)
        {
            Debug.LogError("カメラが見つからないため、キャリブレーションできません");
            return;
        }
        // 現在のカメラの位置を保存
        initialCameraPosition = mainCamera.transform.position;
            
        // カメラの向きを上方向に明示的に設定（必要に応じて調整）
        mainCamera.transform.rotation = Quaternion.Euler(45f, 0f, 0f); // 45度上を向く
        initialCameraRotation = mainCamera.transform.rotation;
        
        Debug.Log($"カメラをキャリブレーションしました - 位置: {initialCameraPosition}, 回転: {initialCameraRotation.eulerAngles}");
    }
    
    /// <summary>
    /// 体験の進行を更新
    /// </summary>
    private void UpdateExperience()
    {
        // 経過時間を計算
        float elapsedTime = Time.time - startTime;
        
        // 体験終了判定
        if (elapsedTime >= totalDuration)
        {
            EndExperience();
            return;
        }
        
        // 現在のフェーズと進行度を計算
        UpdatePhaseAndProgress(elapsedTime);
        
        // 各コンポーネントを更新
        UpdateAllSystems();
    }
    
    /// <summary>
    /// 現在のフェーズと進行度を計算
    /// </summary>
    private void UpdatePhaseAndProgress(float elapsedTime)
    {
        float accumulatedTime = 0f;
        
        for (int i = 1; i < phaseDurations.Length; i++)
        {
            accumulatedTime += phaseDurations[i];
            
            if (elapsedTime < accumulatedTime)
            {
                currentPhase = i;
                float phaseStartTime = accumulatedTime - phaseDurations[i];
                phaseProgress = (elapsedTime - phaseStartTime) / phaseDurations[i];
                return;
            }
        }
        
        // 最終フェーズ
        currentPhase = phaseDurations.Length - 1;
        phaseProgress = 1f;
    }
    
    /// <summary>
    /// 全システムを更新
    /// </summary>
    private void UpdateAllSystems()
    {
        // 星フィールドの更新
        if (starField != null)
        {
            starField.UpdateStarField(currentPhase, phaseProgress);
        }
        
        // ビジュアルエフェクトの更新
        if (visualEffects != null)
        {
            visualEffects.UpdateEffects(currentPhase, phaseProgress);
        }
        
        // 立体音響の更新
        if (audioManager != null)
        {
            audioManager.UpdateAudio(currentPhase, phaseProgress);
        }
        
        // 振動の更新
        if (vibrationController != null)
        {
            vibrationController.UpdateVibrationFromPhase(currentPhase, phaseProgress);
        }
        
        // デバッグ情報の表示
        if (debugMode)
        {
            DisplayDebugInfo();
        }
    }
    
    /// <summary>
    /// 体験開始
    /// </summary>
    public void StartExperience()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("システムが初期化されていないため、体験を開始できません");
            return;
        }
        
        if (isExperienceRunning)
        {
            EndExperience(); // 実行中なら一度終了
        }
        
        // パラメータ初期化
        startTime = Time.time;
        isExperienceRunning = true;
        currentPhase = 1;
        phaseProgress = 0f;
        
        // 各システムを開始
        InitializeAllSystems();
        
        Debug.Log("臨死体験を開始しました");
    }
    
    /// <summary>
    /// 特定のフェーズから体験開始（デバッグ用）
    /// </summary>
    public void StartExperienceAtPhase(int phase)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("システムが初期化されていないため、体験を開始できません");
            return;
        }
        
        if (isExperienceRunning)
        {
            EndExperience(); // 実行中なら一度終了
        }
        
        // 有効なフェーズを確保
        phase = Mathf.Clamp(phase, 1, phaseDurations.Length - 1);
        
        // 経過時間を調整してフェーズ開始位置に設定
        float timeOffset = 0f;
        for (int i = 1; i < phase; i++)
        {
            timeOffset += phaseDurations[i];
        }
        
        // パラメータ初期化
        startTime = Time.time - timeOffset;
        isExperienceRunning = true;
        currentPhase = phase;
        phaseProgress = 0f;
        
        // 各システムを開始
        InitializeAllSystems();
        
        Debug.Log($"臨死体験をフェーズ{phase}から開始しました");
    }
    
    /// <summary>
    /// 全システムを初期化
    /// </summary>
    private void InitializeAllSystems()
    {
        // 星フィールドの初期化
        if (starField != null)
        {
            starField.InitializeStars();
        }
        
        // ビジュアルエフェクトの初期化
        if (visualEffects != null)
        {
            visualEffects.InitializeEffects();
        }
        
        // 立体音響の開始
        if (audioManager != null)
        {
            audioManager.StartAudio();
        }
        
        // 振動の開始通知
        if (vibrationController != null)
        {
            vibrationController.SendStartMessage();
        }
    }
    
    /// <summary>
    /// 体験終了
    /// </summary>
    public void EndExperience()
    {
        if (!isExperienceRunning) return;
        
        isExperienceRunning = false;
        
        // 各システムを終了
        if (starField != null)
        {
            starField.FadeOutStars(fadeOutDuration);
        }
        
        if (visualEffects != null)
        {
            visualEffects.FadeOutAll(fadeOutDuration);
        }
        
        if (audioManager != null)
        {
            audioManager.StopAudio(fadeOutDuration);
        }
        
        if (vibrationController != null)
        {
            vibrationController.SendEndMessage();
        }
        
        Debug.Log("臨死体験が終了しました");
    }
    
    /// <summary>
    /// デバッグコントロールの処理
    /// </summary>
    private void HandleDebugControls()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        // フェーズジャンプ
        if (keyboard.digit1Key.wasPressedThisFrame) StartExperienceAtPhase(1);
        if (keyboard.digit2Key.wasPressedThisFrame) StartExperienceAtPhase(2);
        if (keyboard.digit3Key.wasPressedThisFrame) StartExperienceAtPhase(3);
        if (keyboard.digit4Key.wasPressedThisFrame) StartExperienceAtPhase(4);
        if (keyboard.digit5Key.wasPressedThisFrame) StartExperienceAtPhase(5);
        if (keyboard.digit6Key.wasPressedThisFrame) StartExperienceAtPhase(6);
        
        // 再生制御
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            if (isExperienceRunning) EndExperience();
            else StartExperience();
        }
    }
    
    /// <summary>
    /// デバッグ情報の表示
    /// </summary>
    private void DisplayDebugInfo()
    {
        float elapsedTime = Time.time - startTime;
        string message = $"フェーズ: {currentPhase} | 進行度: {phaseProgress:F2} | 経過時間: {elapsedTime:F1}秒 / {totalDuration}秒";
        Debug.Log(message);
    }
    
    /// <summary>
    /// 現在のフェーズを取得
    /// </summary>
    public int GetCurrentPhase()
    {
        return currentPhase;
    }
    
    /// <summary>
    /// 現在の進行度を取得
    /// </summary>
    public float GetPhaseProgress()
    {
        return phaseProgress;
    }
    
    /// <summary>
    /// 体験実行中かどうかを取得
    /// </summary>
    public bool IsRunning()
    {
        return isExperienceRunning;
    }
}