// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI要素を管理するクラス
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("スタート画面のキャンバス")]
    public Canvas startScreenCanvas;
    [Tooltip("エンド画面のキャンバス")]
    public Canvas endScreenCanvas;
    [Tooltip("エキストラ情報キャンバス（オプション）")]
    public Canvas infoScreenCanvas;
    
    [Header("Buttons")]
    [Tooltip("開始ボタン")]
    public Button startButton;
    [Tooltip("再体験ボタン")]
    public Button restartButton;
    [Tooltip("終了ボタン")]
    public Button quitButton;
    [Tooltip("情報表示ボタン（オプション）")]
    public Button infoButton;
    [Tooltip("情報を閉じるボタン（オプション）")]
    public Button infoCloseButton;
    
    [Header("Text Elements")]
    [Tooltip("タイトルテキスト")]
    public TextMeshProUGUI titleText;
    [Tooltip("サブタイトルテキスト")]
    public TextMeshProUGUI subtitleText;
    [Tooltip("エンド画面のメッセージ")]
    public TextMeshProUGUI endMessageText;
    [Tooltip("制作者情報テキスト（オプション）")]
    public TextMeshProUGUI creditsText;
    
    [Header("Transition Settings")]
    [Tooltip("画面表示時のフェードイン時間")]
    public float fadeInDuration = 1.0f;
    [Tooltip("画面非表示時のフェードアウト時間")]
    public float fadeOutDuration = 1.0f;
    
    [Header("UI Content")]
    [Tooltip("タイトルテキスト")]
    [TextArea(1, 2)]
    public string titleString = "臨死体験VR";
    [Tooltip("サブタイトルテキスト")]
    [TextArea(1, 3)]
    public string subtitleString = "「硬い→柔らかい」ジャミング転移による体験";
    [Tooltip("エンドメッセージ")]
    [TextArea(1, 3)]
    public string endMessage = "体験は終わりです。おつかれさまでした。";
    [Tooltip("制作者情報")]
    [TextArea(2, 5)]
    public string creditsString = "制作: ○○\n音楽: ○○\n技術支援: ○○";
    
    // 内部参照
    [HideInInspector]
    public DeathExperienceManager experienceManager;
    
    // 内部変数
    private CanvasGroup startScreenGroup;
    private CanvasGroup endScreenGroup;
    private CanvasGroup infoScreenGroup;
    private bool isInitialized = false;
    private Coroutine currentFadeCoroutine;
    
    // Unity ライフサイクル
    void Awake()
    {
        InitializeUI();
    }
    
    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        // キャンバスグループの取得または追加
        if (startScreenCanvas != null)
        {
            startScreenGroup = GetOrAddCanvasGroup(startScreenCanvas.gameObject);
            startScreenGroup.alpha = 0f; // 最初は非表示
            startScreenCanvas.gameObject.SetActive(true);
        }
        
        if (endScreenCanvas != null)
        {
            endScreenGroup = GetOrAddCanvasGroup(endScreenCanvas.gameObject);
            endScreenGroup.alpha = 0f; // 最初は非表示
            endScreenCanvas.gameObject.SetActive(false);
        }
        
        if (infoScreenCanvas != null)
        {
            infoScreenGroup = GetOrAddCanvasGroup(infoScreenCanvas.gameObject);
            infoScreenGroup.alpha = 0f; // 最初は非表示
            infoScreenCanvas.gameObject.SetActive(false);
        }
        
        // ボタンイベントの設定
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
        
        if (infoButton != null)
        {
            infoButton.onClick.AddListener(OnInfoButtonClicked);
        }
        
        if (infoCloseButton != null)
        {
            infoCloseButton.onClick.AddListener(OnInfoCloseButtonClicked);
        }
        
        // テキストの設定
        if (titleText != null)
        {
            titleText.text = titleString;
        }
        
        if (subtitleText != null)
        {
            subtitleText.text = subtitleString;
        }
        
        if (endMessageText != null)
        {
            endMessageText.text = endMessage;
        }
        
        if (creditsText != null)
        {
            creditsText.text = creditsString;
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// CanvasGroupコンポーネントを取得または追加
    /// </summary>
    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        CanvasGroup group = obj.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = obj.AddComponent<CanvasGroup>();
        }
        return group;
    }
    
    /// <summary>
    /// シーン準備完了時に呼ばれる
    /// </summary>
    public void OnSceneReady()
    {
        if (!isInitialized) InitializeUI();
        
        // スタート画面を表示
        ShowStartScreen();
    }
    
    /// <summary>
    /// スタートボタンクリック時
    /// </summary>
    public void OnStartButtonClicked()
    {
        HideStartScreen();
        
        if (experienceManager != null)
        {
            experienceManager.StartExperience();
        }
        else
        {
            Debug.LogWarning("ExperienceManagerが設定されていません");
        }
    }
    
    /// <summary>
    /// リスタートボタンクリック時
    /// </summary>
    public void OnRestartButtonClicked()
    {
        HideEndScreen();
        ShowStartScreen();
    }
    
    /// <summary>
    /// 終了ボタンクリック時
    /// </summary>
    public void OnQuitButtonClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// 情報ボタンクリック時
    /// </summary>
    public void OnInfoButtonClicked()
    {
        if (infoScreenCanvas != null)
        {
            infoScreenCanvas.gameObject.SetActive(true);
            FadeCanvasGroup(infoScreenGroup, 0f, 1f, fadeInDuration);
        }
    }
    
    /// <summary>
    /// 情報を閉じるボタンクリック時
    /// </summary>
    public void OnInfoCloseButtonClicked()
    {
        if (infoScreenCanvas != null)
        {
            StartCoroutine(FadeOutAndDisable(infoScreenGroup, fadeOutDuration, infoScreenCanvas.gameObject));
        }
    }
    
    /// <summary>
    /// スタート画面表示
    /// </summary>
    public void ShowStartScreen()
    {
        if (startScreenCanvas != null)
        {
            startScreenCanvas.gameObject.SetActive(true);
            FadeCanvasGroup(startScreenGroup, 0f, 1f, fadeInDuration);
            
            // VRカメラの前に配置
            PositionCanvasInFrontOfCamera(startScreenCanvas);
        }
    }
    
    /// <summary>
    /// スタート画面非表示
    /// </summary>
    public void HideStartScreen()
    {
        if (startScreenCanvas != null)
        {
            StartCoroutine(FadeOutAndDisable(startScreenGroup, fadeOutDuration, startScreenCanvas.gameObject));
        }
    }
    
    /// <summary>
    /// エンド画面表示
    /// </summary>
    public void ShowEndScreen()
    {
        if (endScreenCanvas != null)
        {
            endScreenCanvas.gameObject.SetActive(true);
            FadeCanvasGroup(endScreenGroup, 0f, 1f, fadeInDuration);
            
            // VRカメラの前に配置
            PositionCanvasInFrontOfCamera(endScreenCanvas);
        }
    }
    
    /// <summary>
    /// エンド画面非表示
    /// </summary>
    public void HideEndScreen()
    {
        if (endScreenCanvas != null)
        {
            StartCoroutine(FadeOutAndDisable(endScreenGroup, fadeOutDuration, endScreenCanvas.gameObject));
        }
    }
    
    /// <summary>
    /// 体験終了時に呼ばれるメソッド
    /// </summary>
    public void OnExperienceEnded()
    {
        // 少し遅延してエンド画面を表示
        StartCoroutine(ShowEndScreenDelayed(2.0f));
    }
    
    /// <summary>
    /// CanvasGroupのフェード処理
    /// </summary>
    private void FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        if (group == null) return;
        
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        
        currentFadeCoroutine = StartCoroutine(FadeCanvasGroupCoroutine(group, startAlpha, endAlpha, duration));
    }
    
    /// <summary>
    /// CanvasGroupのフェードコルーチン
    /// </summary>
    private IEnumerator FadeCanvasGroupCoroutine(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        // 初期値設定
        group.alpha = startAlpha;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // イーズイン・アウト補間
            float smoothT = Mathf.SmoothStep(0, 1, t);
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, smoothT);
            
            yield return null;
        }
        
        // 最終値を確実に設定
        group.alpha = endAlpha;
        
        currentFadeCoroutine = null;
    }
    
    /// <summary>
    /// フェードアウト後にオブジェクトを非アクティブにするコルーチン
    /// </summary>
    private IEnumerator FadeOutAndDisable(CanvasGroup group, float duration, GameObject targetObject)
    {
        yield return FadeCanvasGroupCoroutine(group, group.alpha, 0f, duration);
        targetObject.SetActive(false);
    }
    
    /// <summary>
    /// 遅延付きでエンド画面を表示するコルーチン
    /// </summary>
    private IEnumerator ShowEndScreenDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowEndScreen();
    }
    
    /// <summary>
    /// キャンバスをVRカメラの前に配置
    /// </summary>
    private void PositionCanvasInFrontOfCamera(Canvas canvas)
    {
        if (canvas == null) return;
        
        // メインカメラを取得
        Camera camera = Camera.main;
        if (camera == null) return;
        
        // ワールドスペースキャンバスに設定
        canvas.renderMode = RenderMode.WorldSpace;
        
        // カメラの前方2メートルに配置
        canvas.transform.position = camera.transform.position + camera.transform.forward * 2f;
        
        // カメラの向きに合わせる
        canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - camera.transform.position);
        
        // サイズ調整
        RectTransform rectTransform = canvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(2f, 1.2f);
        }
    }
    
    /// <summary>
    /// VRコントローラーでUIを操作するためのレイキャストを設定
    /// </summary>
    // public void SetupVRInteraction()
    // {
    //     // OVRRaycasterをキャンバスに追加
    //     if (startScreenCanvas != null && startScreenCanvas.gameObject.GetComponent<OVRRaycaster>() == null)
    //     {
    //         startScreenCanvas.gameObject.AddComponent<OVRRaycaster>();
    //     }
        
    //     if (endScreenCanvas != null && endScreenCanvas.gameObject.GetComponent<OVRRaycaster>() == null)
    //     {
    //         endScreenCanvas.gameObject.AddComponent<OVRRaycaster>();
    //     }
        
    //     if (infoScreenCanvas != null && infoScreenCanvas.gameObject.GetComponent<OVRRaycaster>() == null)
    //     {
    //         infoScreenCanvas.gameObject.AddComponent<OVRRaycaster>();
    //     }
        
    //     // OVRInputModuleをEventSystemに追加
    //     UnityEngine.EventSystems.EventSystem eventSystem = GetComponent<UnityEngine.EventSystems.EventSystem>();
    //     if (eventSystem != null)
    //     {
    //         UnityEngine.EventSystems.StandaloneInputModule inputModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    //         if (inputModule != null)
    //         {
    //             // 既存のInputModuleを無効化
    //             inputModule.enabled = false;
    //         }
            
    //         // OVRInputModuleがなければ追加
    //         OVRInput ovrInputModule = eventSystem.GetComponent<OVRInput>();
    //         if (ovrInputModule == null)
    //         {
    //             ovrInputModule = eventSystem.gameObject.AddComponent<OVRInputModule>();
    //         }
            
    //         OVRInput.enabled = true;
    //     }
    // }
}