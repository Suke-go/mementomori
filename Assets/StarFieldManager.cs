// StarFieldManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// 星フィールドを管理するクラス
/// </summary>
public class StarFieldManager : MonoBehaviour
{
    [Header("Star Settings")]
    [Tooltip("生成する星の数")]
    public int numberOfStars = 500;
    [Tooltip("星のプレハブ（nullの場合は内部で作成）")]
    public GameObject starPrefab;
    [Tooltip("星の生成範囲（球状）")]
    public float starFieldRadius = 100f;
    [Tooltip("星の最小サイズ")]
    public float minStarSize = 0.05f;
    [Tooltip("星の最大サイズ")]
    public float maxStarSize = 0.3f;
    [Tooltip("星の色のグラデーション")]
    public Gradient starColorGradient;
    
    [Header("Motion Settings")]
    [Tooltip("渦巻きの強さ")]
    public float vortexStrength = 0.5f;
    [Tooltip("中心への引力")]
    public float centerPull = 0.5f;
    [Tooltip("星の移動速度")]
    public float moveSpeed = 1.0f;
    [Tooltip("星の明滅速度")]
    public float twinkleSpeed = 0.5f;
    [Tooltip("星の明滅強度")]
    public float twinkleAmount = 0.3f;
    
    [Header("Visual Settings")]
    [Tooltip("中心の光源")]
    public Light centerLight;
    [Tooltip("背景色")]
    public Color backgroundColor = new Color(0.01f, 0.01f, 0.02f);
    [Tooltip("星の発光強度")]
    public float starEmissionIntensity = 1.5f;
    
    // 内部変数
    private List<Transform> stars = new List<Transform>();
    private List<Material> starMaterials = new List<Material>();
    private Vector3[] originalPositions;
    private float[] starSizes; // 元のサイズを保存
    private float[] starPhases; // 明滅のフェーズをずらす用
    private bool initialized = false;
    private Transform centerPoint;
    
    // 素材
    private Material defaultStarMaterial;
    
    // Unity ライフサイクル
    void Awake()
    {
        // センターポイントの作成
        GameObject center = new GameObject("StarFieldCenter");
        centerPoint = center.transform;
        centerPoint.SetParent(transform);
        centerPoint.localPosition = Vector3.zero;
        
        // 中心光源の作成（なければ）
        if (centerLight == null)
        {
            GameObject lightObj = new GameObject("CenterLight");
            lightObj.transform.SetParent(centerPoint);
            centerLight = lightObj.AddComponent<Light>();
            centerLight.type = LightType.Point;
            centerLight.color = Color.white;
            centerLight.intensity = 0f; // 初期は消灯
            centerLight.range = 15f;
        }
        
        // カラーグラデーションの設定（なければ）
        if (starColorGradient.colorKeys.Length == 0)
        {
            // 青白い星のグラデーション
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.8f, 0.9f, 1.0f), 0.0f); // 青白
            colorKeys[1] = new GradientColorKey(new Color(0.9f, 0.8f, 1.0f), 0.5f); // 紫がかった白
            colorKeys[2] = new GradientColorKey(new Color(1.0f, 0.9f, 0.8f), 1.0f); // 黄色がかった白
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
            
            starColorGradient.SetKeys(colorKeys, alphaKeys);
        }
        
        // カメラ背景色を設定
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = backgroundColor;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }
    }
    
    void Start()
    {
        // 自動的に初期化
        if (!initialized)
        {
            InitializeStars();
        }
    }
    
    /// <summary>
    /// 星フィールドを初期化
    /// </summary>
    public void InitializeStars()
    {
        // 既存の星をクリア
        ClearStars();
        
        // スターシェーダーマテリアルを準備
        if (defaultStarMaterial == null)
        {
            CreateDefaultStarMaterial();
        }
        
        // 星プレハブの準備
        if (starPrefab == null)
        {
            CreateDefaultStarPrefab();
        }
        
        // 保存用配列の初期化
        originalPositions = new Vector3[numberOfStars];
        starSizes = new float[numberOfStars];
        starPhases = new float[numberOfStars];
        
        // 星を生成
        for (int i = 0; i < numberOfStars; i++)
        {
            // ランダムな位置を生成（球状）
            Vector3 randomDir = Random.onUnitSphere;
            float randomDistance = Random.Range(starFieldRadius * 0.3f, starFieldRadius);
            Vector3 position = randomDir * randomDistance;
            
            // 星のインスタンス化
            GameObject starInstance = Instantiate(starPrefab, position, Quaternion.identity, transform);
            starInstance.name = $"Star_{i}";
            
            // サイズのランダム化
            float sizeScale = Random.Range(minStarSize, maxStarSize);
            starSizes[i] = sizeScale;
            starInstance.transform.localScale = new Vector3(sizeScale, sizeScale, sizeScale);
            
            // 明滅のフェーズをずらす
            starPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            
            // 色のランダム化
            Material starMat = new Material(defaultStarMaterial);
            float colorPos = Random.Range(0f, 1f);
            Color starColor = starColorGradient.Evaluate(colorPos);
            starMat.SetColor("_Color", starColor);
            starMat.SetColor("_EmissionColor", starColor * starEmissionIntensity);
            
            // マテリアルを適用
            Renderer renderer = starInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = starMat;
                starMaterials.Add(starMat);
            }
            
            // リストと配列に追加
            stars.Add(starInstance.transform);
            originalPositions[i] = position;
        }
        
        // 中心の光は最初は消しておく
        if (centerLight != null)
        {
            centerLight.intensity = 0f;
        }
        
        initialized = true;
    }
    
    /// <summary>
    /// 既存の星をすべて削除
    /// </summary>
    public void ClearStars()
    {
        foreach (Transform star in stars)
        {
            if (star != null)
            {
                Destroy(star.gameObject);
            }
        }
        
        stars.Clear();
        starMaterials.Clear();
    }
    
    /// <summary>
    /// 星をフェードアウト
    /// </summary>
    public void FadeOutStars(float duration)
    {
        StartCoroutine(FadeOutStarsCoroutine(duration));
    }
    
    /// <summary>
    /// 星フィールドを更新
    /// </summary>
    public void UpdateStarField(int phase, float phaseProgress)
    {
        if (!initialized || stars.Count == 0) return;
        
        // フェーズ別の星の動きを適用
        switch (phase)
        {
            case 1: // 静寂の宇宙
                UpdatePhase1Stars(phaseProgress);
                break;
                
            case 2: // 変化の兆し
                UpdatePhase2Stars(phaseProgress);
                break;
                
            case 3: // 世界の崩壊
                UpdatePhase3Stars(phaseProgress);
                break;
                
            case 4: // 臨死ピーク
                UpdatePhase4Stars(phaseProgress);
                break;
                
            case 5: // ホワイトアウト
                UpdatePhase5Stars(phaseProgress);
                break;
                
            case 6: // エンディング
                UpdatePhase6Stars(phaseProgress);
                break;
        }
        
        // 光源の更新
        UpdateCenterLight(phase, phaseProgress);
    }
    
    /// <summary>
    /// フェーズ1の星更新（静寂の宇宙）
    /// </summary>
    private void UpdatePhase1Stars(float progress)
    {
        for (int i = 0; i < stars.Count; i++)
        {
            Transform star = stars[i];
            if (star == null) continue;
            
            // オリジナルの位置に保持
            Vector3 originalPos = originalPositions[i];
            
            // わずかなふわふわした動き
            float timeOffset = starPhases[i];
            Vector3 wobble = new Vector3(
                Mathf.Sin(Time.time * 0.1f + timeOffset) * 0.05f,
                Mathf.Cos(Time.time * 0.15f + timeOffset) * 0.05f,
                Mathf.Sin(Time.time * 0.2f + timeOffset + 1f) * 0.05f
            );
            
            // 位置の更新
            star.position = originalPos + wobble;
            
            // 明滅効果
            UpdateStarTwinkle(i, 0.5f);
        }
    }
    
    /// <summary>
    /// フェーズ2の星更新（変化の兆し）
    /// </summary>
    private void UpdatePhase2Stars(float progress)
    {
        for (int i = 0; i < stars.Count; i++)
        {
            Transform star = stars[i];
            if (star == null) continue;
            
            // オリジナルの位置からプログレスに応じてわずかに動かす
            Vector3 originalPos = originalPositions[i];
            Vector3 dirToCenter = (centerPoint.position - originalPos).normalized;
            
            // 風に吹かれるような揺れ
            float timeOffset = starPhases[i];
            Vector3 wobble = new Vector3(
                Mathf.Sin(Time.time * 0.2f + timeOffset) * 0.1f,
                Mathf.Cos(Time.time * 0.3f + timeOffset) * 0.1f,
                Mathf.Sin(Time.time * 0.4f + timeOffset + 1f) * 0.1f
            ) * progress;
            
            // 中心への微小な引力
            float pullFactor = 0.01f * progress;
            Vector3 centerPull = dirToCenter * pullFactor;
            
            // 位置の更新
            star.position = originalPos + wobble + centerPull;
            
            // 明滅効果（やや強く）
            UpdateStarTwinkle(i, 0.7f);
        }
    }
    
    /// <summary>
    /// フェーズ3の星更新（世界の崩壊）
    /// </summary>
    private void UpdatePhase3Stars(float progress)
    {
        for (int i = 0; i < stars.Count; i++)
        {
            Transform star = stars[i];
            if (star == null) continue;
            
            // オリジナル位置
            Vector3 originalPos = originalPositions[i];
            
            // 中心からの距離
            float distanceToCenter = Vector3.Distance(originalPos, centerPoint.position);
            float normalizedDistance = Mathf.Clamp01(distanceToCenter / starFieldRadius);
            
            // 渦を巻き始める回転
            float rotationSpeed = (1f - normalizedDistance) * 0.5f * vortexStrength * progress;
            Quaternion rotation = Quaternion.AngleAxis(rotationSpeed * Time.deltaTime * 100f, Vector3.up);
            Vector3 rotatedPosition = rotation * (originalPos - centerPoint.position) + centerPoint.position;
            
            // 少しずつ中心に引き寄せられる
            Vector3 dirToCenter = (centerPoint.position - rotatedPosition).normalized;
            float pullFactor = progress * centerPull * 0.001f * (1f - normalizedDistance);
            Vector3 pullOffset = dirToCenter * pullFactor * Time.deltaTime * 50f;
            
            // 不規則な揺れ（不安定さを表現）
            float timeOffset = starPhases[i];
            Vector3 instability = new Vector3(
                Mathf.PerlinNoise(Time.time * 0.5f, i * 0.1f) - 0.5f,
                Mathf.PerlinNoise(Time.time * 0.5f, i * 0.1f + 10f) - 0.5f,
                Mathf.PerlinNoise(Time.time * 0.5f, i * 0.1f + 20f) - 0.5f
            ) * 0.2f * progress;
            
            // 最終位置の計算
            Vector3 finalPosition = rotatedPosition + pullOffset + instability;
            
            // 星のサイズも少し変化
            float sizeMultiplier = 1f + (Mathf.Sin(Time.time * 0.5f + timeOffset) * 0.2f * progress);
            float originalSize = starSizes[i];
            Vector3 newScale = new Vector3(
                originalSize * sizeMultiplier, 
                originalSize * sizeMultiplier, 
                originalSize * sizeMultiplier
            );
            
            // 位置とスケールの更新
            star.position = finalPosition;
            star.localScale = newScale;
            
            // 明滅効果（不規則に）
            UpdateStarTwinkle(i, 1.0f + progress);
        }
    }
    
    /// <summary>
    /// フェーズ4の星更新（臨死ピーク）
    /// </summary>
    private void UpdatePhase4Stars(float progress)
    {
        for (int i = 0; i < stars.Count; i++)
        {
            Transform star = stars[i];
            if (star == null) continue;
            
            // 現在位置（フェーズ3からの継続的な動き）
            Vector3 currentPos = star.position;
            
            // 中心からの距離
            float distanceToCenter = Vector3.Distance(currentPos, centerPoint.position);
            float normalizedDistance = Mathf.Clamp01(distanceToCenter / starFieldRadius);
            
            // 渦を巻く回転（より強く）
            float rotationSpeed = (1f - normalizedDistance) * vortexStrength * (1f + progress);
            Quaternion rotation = Quaternion.AngleAxis(rotationSpeed * Time.deltaTime * 150f, Vector3.up);
            Vector3 rotatedPosition = rotation * (currentPos - centerPoint.position) + centerPoint.position;
            
            // 強力な中心への引力
            Vector3 dirToCenter = (centerPoint.position - rotatedPosition).normalized;
            float pullFactor = centerPull * 0.003f * (1f - normalizedDistance) * (1f + progress * 2f);
            Vector3 pullOffset = dirToCenter * pullFactor * Time.deltaTime * 50f;
            
            // 最終位置の計算
            Vector3 finalPosition = rotatedPosition + pullOffset;
            
            // 中心に近づくほど大きく輝く
            float proximityFactor = 1f - Mathf.Clamp01(distanceToCenter / (starFieldRadius * 0.5f));
            float sizeMultiplier = 1f + proximityFactor * progress;
            float originalSize = starSizes[i];
            
            // サイズの更新
            Vector3 newScale = new Vector3(
                originalSize * sizeMultiplier, 
                originalSize * sizeMultiplier, 
                originalSize * sizeMultiplier
            );
            
            // 位置とスケールの更新
            star.position = finalPosition;
            star.localScale = newScale;
            
            // 明滅効果（中心に近いほど強く輝く）
            UpdateStarTwinkle(i, 1.0f + proximityFactor * 2f * progress);
            
            // 色の変化（中心に近いほど白く）
            if (i < starMaterials.Count && starMaterials[i] != null)
            {
                Color originalColor = starColorGradient.Evaluate(i % 10 / 10f);
                Color whiteColor = Color.white;
                Color finalColor = Color.Lerp(originalColor, whiteColor, proximityFactor * progress);
                
                starMaterials[i].SetColor("_Color", finalColor);
                starMaterials[i].SetColor("_EmissionColor", finalColor * (starEmissionIntensity * (1f + proximityFactor * progress)));
            }
        }
    }
    
    /// <summary>
    /// フェーズ5の星更新（ホワイトアウト）
    /// </summary>
    private void UpdatePhase5Stars(float progress)
    {
        for (int i = 0; i < stars.Count; i++)
        {
            Transform star = stars[i];
            if (star == null) continue;
            
            // フェーズ5前半: 星が強く輝いて大きくなる
            if (progress < 0.5f)
            {
                float normalizedProgress = progress * 2f; // 0～1に正規化
                
                // 大きく輝く
                float sizeFactor = 1f + normalizedProgress * 2f;
                float originalSize = starSizes[i];
                
                Vector3 newScale = new Vector3(
                    originalSize * sizeFactor, 
                    originalSize * sizeFactor, 
                    originalSize * sizeFactor
                );
                
                star.localScale = newScale;
                
                // すべての星を白く
                if (i < starMaterials.Count && starMaterials[i] != null)
                {
                    starMaterials[i].SetColor("_Color", Color.white);
                    starMaterials[i].SetColor("_EmissionColor", Color.white * (starEmissionIntensity * (2f + normalizedProgress * 3f)));
                }
            }
            // フェーズ5後半: 徐々に消えていく
            else
            {
                float normalizedProgress = (progress - 0.5f) * 2f; // 0～1に正規化
                
                // 徐々に透明に
                if (i < starMaterials.Count && starMaterials[i] != null)
                {
                    Color colorWithAlpha = new Color(1f, 1f, 1f, 1f - normalizedProgress);
                    starMaterials[i].SetColor("_Color", colorWithAlpha);
                    
                    // 発光も弱まる
                    float emissionFade = Mathf.Lerp(5f, 0f, normalizedProgress);
                    starMaterials[i].SetColor("_EmissionColor", Color.white * emissionFade);
                }
                
                // サイズも徐々に小さく
                float fadeSizeFactor = Mathf.Lerp(3f, 0.5f, normalizedProgress);
                float originalSize = starSizes[i];
                
                Vector3 fadeScale = new Vector3(
                    originalSize * fadeSizeFactor, 
                    originalSize * fadeSizeFactor, 
                    originalSize * fadeSizeFactor
                );
                
                star.localScale = fadeScale;
            }
            
            // 中心に向かってゆっくり移動
            Vector3 dirToCenter = (centerPoint.position - star.position).normalized;
            float moveSpeed = 0.02f * progress;
            star.position += dirToCenter * moveSpeed;
        }
    }
    
    /// <summary>
    /// フェーズ6の星更新（エンディング）
    /// </summary>
    private void UpdatePhase6Stars(float progress)
    {
        for (int i = 0; i < stars.Count; i++)
        {
            Transform star = stars[i];
            if (star == null) continue;
            
            // フェードアウト
            if (i < starMaterials.Count && starMaterials[i] != null)
            {
                // 完全に透明に
                Color fadeColor = new Color(1f, 1f, 1f, 1f - progress);
                starMaterials[i].SetColor("_Color", fadeColor);
                
                // 発光も消す
                float emissionFade = (1f - progress) * 0.5f;
                starMaterials[i].SetColor("_EmissionColor", Color.white * emissionFade);
            }
            
            // 最終的に元の位置に戻り始める（新たな宇宙の創生を暗示）
            Vector3 originalPos = originalPositions[i];
            Vector3 currentPos = star.position;
            
            star.position = Vector3.Lerp(currentPos, originalPos, progress * 0.1f);
            
            // サイズを元に戻す
            float originalSize = starSizes[i];
            float currentSize = star.localScale.x;
            float newSize = Mathf.Lerp(currentSize, originalSize, progress * 0.1f);
            
            star.localScale = new Vector3(newSize, newSize, newSize);
        }
    }
    
    /// <summary>
    /// 中心の光源を更新
    /// </summary>
    private void UpdateCenterLight(int phase, float progress)
    {
        if (centerLight == null) return;
        
        // フェーズに応じた光の強さと色
        switch (phase)
        {
            case 1: // 静寂の宇宙 - 光はほぼなし
                centerLight.intensity = Mathf.Lerp(0f, 0.2f, progress);
                centerLight.color = Color.white;
                break;
                
            case 2: // 変化の兆し - わずかな光
                centerLight.intensity = Mathf.Lerp(0.2f, 1f, progress);
                centerLight.color = Color.white;
                break;
                
            case 3: // 世界の崩壊 - 強まる光
                centerLight.intensity = Mathf.Lerp(1f, 3f, progress);
                centerLight.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.8f), progress);
                break;
                
            case 4: // 臨死ピーク - 最も強い光
                centerLight.intensity = Mathf.Lerp(3f, 8f, progress);
                centerLight.color = Color.Lerp(new Color(1f, 0.9f, 0.8f), new Color(1f, 0.95f, 0.9f), progress);
                break;
                
            case 5: // ホワイトアウト - すべてを包む光
                centerLight.intensity = Mathf.Lerp(8f, 12f, progress * 0.5f);
                // フェーズ5後半で徐々に弱まる
                if (progress > 0.5f)
                {
                    centerLight.intensity = Mathf.Lerp(12f, 6f, (progress - 0.5f) * 2f);
                }
                centerLight.color = Color.white;
                // 光の範囲を徐々に広げる
                centerLight.range = Mathf.Lerp(15f, 50f, progress);
                break;
                
            case 6: // エンディング - 消えていく光
                centerLight.intensity = Mathf.Lerp(6f, 0f, progress);
                centerLight.color = Color.white;
                // 光の範囲を徐々に戻す
                centerLight.range = Mathf.Lerp(50f, 15f, progress);
                break;
        }
    }
    
    /// <summary>
    /// 星の明滅効果を更新
    /// </summary>
    private void UpdateStarTwinkle(int starIndex, float intensityMultiplier)
    {
        if (starIndex >= starMaterials.Count || starMaterials[starIndex] == null) return;
        
        // 時間とフェーズオフセットから明滅を計算
        float timeOffset = starPhases[starIndex];
        float twinkle = Mathf.Sin(Time.time * twinkleSpeed + timeOffset) * 0.5f + 0.5f;
        
        // 現在の色を取得
        Color currentColor = starMaterials[starIndex].GetColor("_Color");
        
        // 明滅効果を発光色に適用
        float emissionMultiplier = 1f + twinkle * twinkleAmount * intensityMultiplier;
        starMaterials[starIndex].SetColor("_EmissionColor", currentColor * starEmissionIntensity * emissionMultiplier);
    }
    
    /// <summary>
    /// 星をフェードアウトするコルーチン
    /// </summary>
    private IEnumerator FadeOutStarsCoroutine(float duration)
    {
        float startTime = Time.time;
        float elapsedTime = 0f;
        
        // 各星の初期色と発光色を保存
        Color[] initialColors = new Color[starMaterials.Count];
        Color[] initialEmissions = new Color[starMaterials.Count];
        
        for (int i = 0; i < starMaterials.Count; i++)
        {
            if (starMaterials[i] != null)
            {
                initialColors[i] = starMaterials[i].GetColor("_Color");
                initialEmissions[i] = starMaterials[i].GetColor("_EmissionColor");
            }
        }
        
        // 中心光源の初期値を保存
        float initialLightIntensity = 0f;
        if (centerLight != null)
        {
            initialLightIntensity = centerLight.intensity;
        }
        
        // 徐々にフェードアウト
        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            float t = elapsedTime / duration; // 0～1
            
            // 各星のフェードアウト
            for (int i = 0; i < starMaterials.Count; i++)
            {
                if (starMaterials[i] != null)
                {
                    // 色のアルファ値を下げる
                    Color fadeColor = initialColors[i];
                    fadeColor.a = Mathf.Lerp(initialColors[i].a, 0f, t);
                    starMaterials[i].SetColor("_Color", fadeColor);
                    
                    // 発光も弱める
                    Color fadeEmission = Color.Lerp(initialEmissions[i], Color.black, t);
                    starMaterials[i].SetColor("_EmissionColor", fadeEmission);
                }
            }
            
            // 中心光源のフェードアウト
            if (centerLight != null)
            {
                centerLight.intensity = Mathf.Lerp(initialLightIntensity, 0f, t);
            }
            
            yield return null;
        }
        
        // 完全に消す
        for (int i = 0; i < starMaterials.Count; i++)
        {
            if (starMaterials[i] != null)
            {
                Color transparent = new Color(0f, 0f, 0f, 0f);
                starMaterials[i].SetColor("_Color", transparent);
                starMaterials[i].SetColor("_EmissionColor", Color.black);
            }
        }
        
        if (centerLight != null)
        {
            centerLight.intensity = 0f;
        }
    }
    
    /// <summary>
    /// デフォルトの星マテリアルを作成
    /// </summary>
    private void CreateDefaultStarMaterial()
    {
        // スターシェーダーのロード（なければ）
        Shader starShader = Shader.Find("Custom/StarShader");
        if (starShader == null)
        {
            // シェーダーが見つからない場合は標準シェーダーを使用
            defaultStarMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultStarMaterial.EnableKeyword("_EMISSION");
            Debug.LogWarning("Custom/StarShaderが見つかりません。標準シェーダーを使用します。");
        }
        else
        {
            // カスタムスターシェーダーを使用
            defaultStarMaterial = new Material(starShader);
            defaultStarMaterial.SetFloat("_EmissionIntensity", starEmissionIntensity);
            defaultStarMaterial.SetFloat("_PulseSpeed", twinkleSpeed);
            defaultStarMaterial.SetFloat("_PulseAmount", twinkleAmount);
        }
    }
    
    /// <summary>
    /// デフォルトの星プレハブを作成
    /// </summary>
    private void CreateDefaultStarPrefab()
    {
        GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        star.name = "StarPrefab";
        
        // コライダーは不要
        DestroyImmediate(star.GetComponent<SphereCollider>());
        
        // マテリアルの設定
        MeshRenderer renderer = star.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = defaultStarMaterial;
        }
        
        starPrefab = star;
        // プレハブなのでシーンには表示しない
        star.SetActive(false);
    }
}