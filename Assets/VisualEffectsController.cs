// VisualEffectsController.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// 視覚効果を制御するクラス
/// </summary>
public class VisualEffectsController : MonoBehaviour
{
    [Header("Post-Processing")]
    [Tooltip("ポストプロセッシングボリューム")]
    public Volume postProcessVolume;
    [Tooltip("VRではポストプロセッシングを控えめにする")]
    public bool reduceEffectsForVR = true;
    
    [Header("Overlay Effects")]
    [Tooltip("ホワイトアウト用オーバーレイ")]
    public MeshRenderer whiteOverlay;
    [Tooltip("暗転用オーバーレイ")]
    public MeshRenderer blackOverlay;
    
    [Header("Custom Effects")]
    [Tooltip("エフェクト強度")]
    [Range(0f, 1f)]
    public float effectIntensity = 0.8f;
    [Tooltip("エフェクト速度")]
    [Range(0.1f, 2f)]
    public float effectSpeed = 1.0f;
    
    // ポストプロセスエフェクト
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private Bloom bloom;
    private DepthOfField depthOfField;
    private LensDistortion lensDistortion;
    
    // オーバーレイマテリアル
    private Material whiteOverlayMaterial;
    private Material blackOverlayMaterial;
    
    // 状態管理
    private bool initialized = false;
    private Coroutine fadeCoroutine;
    
    // Unity ライフサイクル
    void Awake()
    {
        InitializeEffects();
    }
    
    /// <summary>
    /// エフェクトの初期化
    /// </summary>
    public void InitializeEffects()
    {
        // ポストプロセスボリュームの初期化
        InitializePostProcessing();
        
        // オーバーレイの初期化
        InitializeOverlays();
        
        initialized = true;
    }
    
    /// <summary>
    /// ポストプロセッシングの初期化
    /// </summary>
    private void InitializePostProcessing()
    {
        // ボリュームがなければ作成
        if (postProcessVolume == null)
        {
            GameObject volumeObj = new GameObject("PostProcessVolume");
            volumeObj.transform.SetParent(transform);
            
            postProcessVolume = volumeObj.AddComponent<Volume>();
            postProcessVolume.isGlobal = true;
            postProcessVolume.priority = 1;
            
            // プロファイルの作成
            postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }
        
        // 必要なエフェクトを追加
        if (!postProcessVolume.profile.Has<Vignette>())
            postProcessVolume.profile.Add<Vignette>();
        
        if (!postProcessVolume.profile.Has<ChromaticAberration>())
            postProcessVolume.profile.Add<ChromaticAberration>();
        
        if (!postProcessVolume.profile.Has<Bloom>())
            postProcessVolume.profile.Add<Bloom>();
        
        if (!postProcessVolume.profile.Has<DepthOfField>())
            postProcessVolume.profile.Add<DepthOfField>();
        
        if (!postProcessVolume.profile.Has<LensDistortion>())
            postProcessVolume.profile.Add<LensDistortion>();
        
        // 参照を取得
        postProcessVolume.profile.TryGet(out vignette);
        postProcessVolume.profile.TryGet(out chromaticAberration);
        postProcessVolume.profile.TryGet(out bloom);
        postProcessVolume.profile.TryGet(out depthOfField);
        postProcessVolume.profile.TryGet(out lensDistortion);
        
        // 初期設定
        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.value = 0.2f;
            vignette.color.value = Color.black;
        }
        
        if (chromaticAberration != null)
        {
            chromaticAberration.active = true;
            chromaticAberration.intensity.value = 0f;
        }
        
        if (bloom != null)
        {
            bloom.active = true;
            bloom.intensity.value = 0f;
            bloom.threshold.value = 0.8f;
        }
        
        if (depthOfField != null)
        {
            depthOfField.active = false;
        }
        
        if (lensDistortion != null)
        {
            lensDistortion.active = true;
            lensDistortion.intensity.value = 0f;
        }
    }
    
    /// <summary>
    /// オーバーレイの初期化
    /// </summary>
    private void InitializeOverlays()
    {
        // メインカメラ取得
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("メインカメラが見つかりません");
            return;
        }
        
        // ホワイトオーバーレイの作成
        if (whiteOverlay == null)
        {
            GameObject whiteObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            whiteObj.name = "WhiteOverlay";
            whiteObj.transform.SetParent(mainCamera.transform);
            whiteObj.transform.localPosition = new Vector3(0, 0, 0.3f);
            whiteObj.transform.localRotation = Quaternion.identity;
            whiteObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // コライダーは不要
            Destroy(whiteObj.GetComponent<Collider>());
            
            whiteOverlay = whiteObj.GetComponent<MeshRenderer>();
            
            // マテリアル設定
            Shader overlayShader = Shader.Find("Custom/OverlayShader");
            if (overlayShader == null)
            {
                // 標準シェーダーで代用
                whiteOverlayMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                whiteOverlayMaterial.color = new Color(1, 1, 1, 0);
                Debug.LogWarning("Custom/OverlayShaderが見つかりません。標準シェーダーを使用します。");
            }
            else
            {
                whiteOverlayMaterial = new Material(overlayShader);
                whiteOverlayMaterial.SetColor("_Color", new Color(1, 1, 1, 0));
                whiteOverlayMaterial.SetFloat("_PulseSpeed", 0.5f);
                whiteOverlayMaterial.SetFloat("_PulseAmount", 0.1f);
            }
            
            whiteOverlay.material = whiteOverlayMaterial;
        }
        else
        {
            // 既存のオーバーレイのマテリアル取得
            whiteOverlayMaterial = whiteOverlay.material;
        }
        
        // ブラックオーバーレイの作成
        if (blackOverlay == null)
        {
            GameObject blackObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            blackObj.name = "BlackOverlay";
            blackObj.transform.SetParent(mainCamera.transform);
            blackObj.transform.localPosition = new Vector3(0, 0, 0.31f); // 白より少し手前
            blackObj.transform.localRotation = Quaternion.identity;
            blackObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // コライダーは不要
            Destroy(blackObj.GetComponent<Collider>());
            
            blackOverlay = blackObj.GetComponent<MeshRenderer>();
            
            // マテリアル設定
            Shader overlayShader = Shader.Find("Custom/OverlayShader");
            if (overlayShader == null)
            {
                // 標準シェーダーで代用
                blackOverlayMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                blackOverlayMaterial.color = new Color(0, 0, 0, 0);
            }
            else
            {
                blackOverlayMaterial = new Material(overlayShader);
                blackOverlayMaterial.SetColor("_Color", new Color(0, 0, 0, 0));
                blackOverlayMaterial.SetFloat("_PulseSpeed", 0.3f);
                blackOverlayMaterial.SetFloat("_PulseAmount", 0.05f);
            }
            
            blackOverlay.material = blackOverlayMaterial;
        }
        else
        {
            // 既存のオーバーレイのマテリアル取得
            blackOverlayMaterial = blackOverlay.material;
        }
        
        // レイヤー設定（カメラのCullingMaskで制御可能に）
        int overlayLayer = LayerMask.NameToLayer("UI");
        if (overlayLayer >= 0)
        {
            if (whiteOverlay != null) whiteOverlay.gameObject.layer = overlayLayer;
            if (blackOverlay != null) blackOverlay.gameObject.layer = overlayLayer;
        }
    }
    
// VisualEffectsController.cs (続き)
    /// <summary>
    /// フェーズと進行度に基づいてエフェクトを更新
    /// </summary>
    public void UpdateEffects(int phase, float phaseProgress)
    {
        if (!initialized) return;
        
        // フェーズごとのエフェクト更新
        switch (phase)
        {
            case 1: // 静寂の宇宙
                UpdatePhase1Effects(phaseProgress);
                break;
                
            case 2: // 変化の兆し
                UpdatePhase2Effects(phaseProgress);
                break;
                
            case 3: // 世界の崩壊
                UpdatePhase3Effects(phaseProgress);
                break;
                
            case 4: // 臨死ピーク
                UpdatePhase4Effects(phaseProgress);
                break;
                
            case 5: // ホワイトアウト
                UpdatePhase5Effects(phaseProgress);
                break;
                
            case 6: // エンディング
                UpdatePhase6Effects(phaseProgress);
                break;
        }
    }
    
    /// <summary>
    /// フェーズ1のエフェクト更新（静寂の宇宙）
    /// </summary>
    private void UpdatePhase1Effects(float progress)
    {
        // 基本的なビネット効果のみ
        if (vignette != null)
        {
            vignette.intensity.value = 0.2f + progress * 0.1f;
            vignette.color.value = Color.black;
        }
        
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = progress * 0.05f * effectIntensity;
        }
        
        if (bloom != null)
        {
            bloom.intensity.value = 0f;
        }
        
        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = 0f;
        }
        
        // オーバーレイは透明
        if (whiteOverlayMaterial != null)
        {
            whiteOverlayMaterial.color = new Color(1, 1, 1, 0);
            // またはシェーダーパラメーター設定
            if (whiteOverlayMaterial.HasProperty("_Alpha"))
            {
                whiteOverlayMaterial.SetFloat("_Alpha", 0);
            }
        }
        
        if (blackOverlayMaterial != null)
        {
            blackOverlayMaterial.color = new Color(0, 0, 0, 0);
            // またはシェーダーパラメーター設定
            if (blackOverlayMaterial.HasProperty("_Alpha"))
            {
                blackOverlayMaterial.SetFloat("_Alpha", 0);
            }
        }
    }
    
    /// <summary>
    /// フェーズ2のエフェクト更新（変化の兆し）
    /// </summary>
    private void UpdatePhase2Effects(float progress)
    {
        // ビネット効果を強める
        if (vignette != null)
        {
            vignette.intensity.value = 0.3f + progress * 0.2f;
            // 色を少し青紫に
            vignette.color.value = Color.Lerp(Color.black, new Color(0.1f, 0.05f, 0.2f), progress);
        }
        
        // 色収差の導入
        if (chromaticAberration != null)
        {
            float aberrationIntensity = reduceEffectsForVR ? 0.2f : 0.4f;
            chromaticAberration.intensity.value = progress * aberrationIntensity * effectIntensity;
        }
        
        // わずかなブルーム
        if (bloom != null)
        {
            bloom.intensity.value = progress * 0.3f * effectIntensity;
            bloom.threshold.value = Mathf.Lerp(0.8f, 0.7f, progress);
        }
        
        // わずかなレンズ歪み
        if (lensDistortion != null)
        {
            float distortionIntensity = reduceEffectsForVR ? 0.1f : 0.2f;
            lensDistortion.intensity.value = progress * distortionIntensity * effectIntensity;
        }
        
        // オーバーレイは依然として透明
        if (whiteOverlayMaterial != null)
        {
            whiteOverlayMaterial.color = new Color(1, 1, 1, 0);
            if (whiteOverlayMaterial.HasProperty("_Alpha"))
            {
                whiteOverlayMaterial.SetFloat("_Alpha", 0);
            }
        }
        
        if (blackOverlayMaterial != null)
        {
            blackOverlayMaterial.color = new Color(0, 0, 0, 0);
            if (blackOverlayMaterial.HasProperty("_Alpha"))
            {
                blackOverlayMaterial.SetFloat("_Alpha", 0);
            }
        }
    }
    
    /// <summary>
    /// フェーズ3のエフェクト更新（世界の崩壊）
    /// </summary>
    private void UpdatePhase3Effects(float progress)
    {
        // ビネットが強まる
        if (vignette != null)
        {
            vignette.intensity.value = 0.5f + progress * 0.2f;
            vignette.color.value = new Color(0.1f, 0.05f, 0.2f);
        }
        
        // 色収差を強める
        if (chromaticAberration != null)
        {
            float aberrationIntensity = reduceEffectsForVR ? 0.4f : 0.6f;
            chromaticAberration.intensity.value = (0.2f + progress * 0.4f) * aberrationIntensity * effectIntensity;
        }
        
        // ブルームの増加
        if (bloom != null)
        {
            bloom.intensity.value = (0.3f + progress * 0.5f) * effectIntensity;
            bloom.threshold.value = Mathf.Lerp(0.7f, 0.5f, progress);
        }
        
        // レンズ歪みの増加
        if (lensDistortion != null)
        {
            float distortionIntensity = reduceEffectsForVR ? 0.2f : 0.3f;
            float distortionValue = (0.2f + progress * 0.3f) * distortionIntensity * effectIntensity;
            
            // 歪みの周期的な変動を追加
            distortionValue += Mathf.Sin(Time.time * 1.0f * effectSpeed) * 0.05f * progress;
            
            lensDistortion.intensity.value = distortionValue;
        }
        
        // ブラックオーバーレイのわずかな導入（周辺を暗く）
        if (blackOverlayMaterial != null)
        {
            float fadeValue = progress * 0.1f;
            blackOverlayMaterial.color = new Color(0, 0, 0, fadeValue);
            if (blackOverlayMaterial.HasProperty("_Alpha"))
            {
                blackOverlayMaterial.SetFloat("_Alpha", fadeValue);
            }
        }
    }
    
    /// <summary>
    /// フェーズ4のエフェクト更新（臨死ピーク）
    /// </summary>
    private void UpdatePhase4Effects(float progress)
    {
        // ビネットが変化（明るい中心に）
        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(0.7f, 0.5f, progress);
            vignette.color.value = Color.Lerp(new Color(0.1f, 0.05f, 0.2f), Color.white, progress);
        }
        
        // 色収差のピーク
        if (chromaticAberration != null)
        {
            float aberrationBase = reduceEffectsForVR ? 0.5f : 0.8f;
            
            // 4.5付近でピークに達し、その後急速に減少
            float aberrationCurve;
            if (progress < 0.8f)
            {
                aberrationCurve = Mathf.Lerp(0.6f, 1.0f, progress / 0.8f);
            }
            else
            {
                aberrationCurve = Mathf.Lerp(1.0f, 0.3f, (progress - 0.8f) / 0.2f);
            }
            
            chromaticAberration.intensity.value = aberrationBase * aberrationCurve * effectIntensity;
        }
        
        // ブルームが強くなる
        if (bloom != null)
        {
            bloom.intensity.value = Mathf.Lerp(0.8f, 2.0f, progress) * effectIntensity;
            bloom.threshold.value = Mathf.Lerp(0.5f, 0.3f, progress);
        }
        
        // レンズ歪みのピーク
        if (lensDistortion != null)
        {
            float distortionBase = reduceEffectsForVR ? 0.3f : 0.5f;
            
            // 非線形な変化（急に強まり、急に弱まる）
            float time = progress * 2f * Mathf.PI;
            float distortionCurve = Mathf.Sin(time) * 0.5f + 0.5f;
            
            lensDistortion.intensity.value = distortionBase * distortionCurve * effectIntensity;
        }
        
        // 焦点ぼけ効果（オプション）
        if (depthOfField != null && !reduceEffectsForVR) // VRでは無効化
        {
            if (progress > 0.7f)
            {
                float dofIntensity = (progress - 0.7f) / 0.3f;
                depthOfField.active = true;
                depthOfField.focusDistance.value = Mathf.Lerp(10f, 0.1f, dofIntensity);
                depthOfField.aperture.value = Mathf.Lerp(1f, 5f, dofIntensity);
            }
            else
            {
                depthOfField.active = false;
            }
        }
        
        // ホワイトオーバーレイを徐々に導入
        if (whiteOverlayMaterial != null)
        {
            // 進行度80%以降で徐々に白くなる
            float fadeValue = progress > 0.8f ? (progress - 0.8f) / 0.2f * 0.3f : 0f;
            whiteOverlayMaterial.color = new Color(1, 1, 1, fadeValue);
            if (whiteOverlayMaterial.HasProperty("_Alpha"))
            {
                whiteOverlayMaterial.SetFloat("_Alpha", fadeValue);
                
                // パルス効果も設定
                if (whiteOverlayMaterial.HasProperty("_PulseSpeed"))
                {
                    whiteOverlayMaterial.SetFloat("_PulseSpeed", 1.0f * effectSpeed);
                    whiteOverlayMaterial.SetFloat("_PulseAmount", 0.2f * effectIntensity);
                }
            }
        }
    }
    
    /// <summary>
    /// フェーズ5のエフェクト更新（ホワイトアウト）
    /// </summary>
    private void UpdatePhase5Effects(float progress)
    {
        // 最初の一瞬（0.1秒）で静寂
        if (progress < 0.05f)
        {
            // すべてのエフェクトを一時的に消す
            if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
            if (bloom != null) bloom.intensity.value = 0f;
            if (lensDistortion != null) lensDistortion.intensity.value = 0f;
            if (vignette != null) vignette.intensity.value = 0f;
            if (depthOfField != null) depthOfField.active = false;
            
            // オーバーレイも消す
            if (whiteOverlayMaterial != null)
            {
                whiteOverlayMaterial.color = new Color(1, 1, 1, 0);
                if (whiteOverlayMaterial.HasProperty("_Alpha"))
                {
                    whiteOverlayMaterial.SetFloat("_Alpha", 0);
                }
            }
            
            if (blackOverlayMaterial != null)
            {
                blackOverlayMaterial.color = new Color(0, 0, 0, 0);
                if (blackOverlayMaterial.HasProperty("_Alpha"))
                {
                    blackOverlayMaterial.SetFloat("_Alpha", 0);
                }
            }
            
            return;
        }
        
        // ビネット効果は消える
        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(0.5f, 0f, progress);
            vignette.color.value = Color.white;
        }
        
        // 色収差も徐々に消える
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(0.3f, 0f, progress);
        }
        
        // ブルームは最大化
        if (bloom != null)
        {
            bloom.intensity.value = Mathf.Lerp(2.0f, 5.0f, progress * 0.5f);
            if (progress > 0.5f)
            {
                bloom.intensity.value = Mathf.Lerp(5.0f, 3.0f, (progress - 0.5f) * 2f);
            }
            bloom.threshold.value = Mathf.Lerp(0.3f, 0.1f, progress);
        }
        
        // レンズ歪みは消える
        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = Mathf.Lerp(0.2f, 0f, progress);
        }
        
        // ホワイトアウト
        if (whiteOverlayMaterial != null)
        {
            // 進行度に応じて徐々に白くなる
            float fadeValue = Mathf.SmoothStep(0.3f, 1.0f, progress);
            whiteOverlayMaterial.color = new Color(1, 1, 1, fadeValue);
            if (whiteOverlayMaterial.HasProperty("_Alpha"))
            {
                whiteOverlayMaterial.SetFloat("_Alpha", fadeValue);
                
                // パルス効果
                if (whiteOverlayMaterial.HasProperty("_PulseSpeed"))
                {
                    whiteOverlayMaterial.SetFloat("_PulseSpeed", Mathf.Lerp(1.0f, 0.5f, progress) * effectSpeed);
                    whiteOverlayMaterial.SetFloat("_PulseAmount", Mathf.Lerp(0.2f, 0.1f, progress) * effectIntensity);
                }
            }
        }
    }
    
    /// <summary>
    /// フェーズ6のエフェクト更新（エンディング）
    /// </summary>
    private void UpdatePhase6Effects(float progress)
    {
        // ビネットなし
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
        }
        
        // 色収差なし
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = 0f;
        }
        
        // ブルームは徐々に消える
        if (bloom != null)
        {
            bloom.intensity.value = Mathf.Lerp(3.0f, 0f, progress);
            bloom.threshold.value = Mathf.Lerp(0.1f, 0.8f, progress);
        }
        
        // レンズ歪みなし
        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = 0f;
        }
        
        // 焦点ぼけなし
        if (depthOfField != null)
        {
            depthOfField.active = false;
        }
        
        // ホワイトアウトが徐々に消える
        if (whiteOverlayMaterial != null)
        {
            float fadeValue = Mathf.Lerp(1.0f, 0f, progress);
            whiteOverlayMaterial.color = new Color(1, 1, 1, fadeValue);
            if (whiteOverlayMaterial.HasProperty("_Alpha"))
            {
                whiteOverlayMaterial.SetFloat("_Alpha", fadeValue);
            }
        }
    }
    
    /// <summary>
    /// すべてのエフェクトをリセット
    /// </summary>
    public void ResetAllEffects()
    {
        // ポストプロセスエフェクトのリセット
        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.value = 0.2f;
            vignette.color.value = Color.black;
        }
        
        if (chromaticAberration != null)
        {
            chromaticAberration.active = true;
            chromaticAberration.intensity.value = 0f;
        }
        
        if (bloom != null)
        {
            bloom.active = true;
            bloom.intensity.value = 0f;
            bloom.threshold.value = 0.8f;
        }
        
        if (depthOfField != null)
        {
            depthOfField.active = false;
        }
        
        if (lensDistortion != null)
        {
            lensDistortion.active = true;
            lensDistortion.intensity.value = 0f;
        }
        
        // オーバーレイのリセット
        if (whiteOverlayMaterial != null)
        {
            whiteOverlayMaterial.color = new Color(1, 1, 1, 0);
            if (whiteOverlayMaterial.HasProperty("_Alpha"))
            {
                whiteOverlayMaterial.SetFloat("_Alpha", 0);
            }
        }
        
        if (blackOverlayMaterial != null)
        {
            blackOverlayMaterial.color = new Color(0, 0, 0, 0);
            if (blackOverlayMaterial.HasProperty("_Alpha"))
            {
                blackOverlayMaterial.SetFloat("_Alpha", 0);
            }
        }
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }
    
    /// <summary>
    /// すべてのエフェクトをフェードアウト
    /// </summary>
    public void FadeOutAll(float duration)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(FadeOutAllCoroutine(duration));
    }
    
    /// <summary>
    /// すべてのエフェクトをフェードアウトするコルーチン
    /// </summary>
    private IEnumerator FadeOutAllCoroutine(float duration)
    {
        float startTime = Time.time;
        
        // 初期値を保存
        float initialVignetteIntensity = vignette != null ? vignette.intensity.value : 0f;
        float initialChromaticIntensity = chromaticAberration != null ? chromaticAberration.intensity.value : 0f;
        float initialBloomIntensity = bloom != null ? bloom.intensity.value : 0f;
        float initialDistortionIntensity = lensDistortion != null ? lensDistortion.intensity.value : 0f;
        float initialWhiteOverlayAlpha = 0f;
        float initialBlackOverlayAlpha = 0f;
        
        if (whiteOverlayMaterial != null)
        {
            initialWhiteOverlayAlpha = whiteOverlayMaterial.color.a;
        }
        
        if (blackOverlayMaterial != null)
        {
            initialBlackOverlayAlpha = blackOverlayMaterial.color.a;
        }
        
        while (Time.time < startTime + duration)
        {
            float elapsed = Time.time - startTime;
            float t = elapsed / duration;
            
            // イーズアウト補間
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // ポストプロセスの徐々にフェードアウト
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(initialVignetteIntensity, 0f, smoothT);
            }
            
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(initialChromaticIntensity, 0f, smoothT);
            }
            
            if (bloom != null)
            {
                bloom.intensity.value = Mathf.Lerp(initialBloomIntensity, 0f, smoothT);
            }
            
            if (lensDistortion != null)
            {
                lensDistortion.intensity.value = Mathf.Lerp(initialDistortionIntensity, 0f, smoothT);
            }
            
            if (depthOfField != null)
            {
                depthOfField.active = false;
            }
            
            // オーバーレイのフェードアウト
            if (whiteOverlayMaterial != null)
            {
                Color color = whiteOverlayMaterial.color;
                color.a = Mathf.Lerp(initialWhiteOverlayAlpha, 0f, smoothT);
                whiteOverlayMaterial.color = color;
                
                if (whiteOverlayMaterial.HasProperty("_Alpha"))
                {
                    whiteOverlayMaterial.SetFloat("_Alpha", color.a);
                }
            }
            
            if (blackOverlayMaterial != null)
            {
                Color color = blackOverlayMaterial.color;
                color.a = Mathf.Lerp(initialBlackOverlayAlpha, 0f, smoothT);
                blackOverlayMaterial.color = color;
                
                if (blackOverlayMaterial.HasProperty("_Alpha"))
                {
                    blackOverlayMaterial.SetFloat("_Alpha", color.a);
                }
            }
            
            yield return null;
        }
        
        // すべてのエフェクトを完全に無効化
        ResetAllEffects();
        
        fadeCoroutine = null;
    }
}