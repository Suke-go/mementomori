// SpatialAudioManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// 立体音響を管理するクラス
/// 単一WAVファイルを使用し、音源の空間的な移動により立体音響を実現
/// </summary>
public class SpatialAudioManager : MonoBehaviour
{
    [Header("Main Audio")]
    [Tooltip("メインオーディオファイル（3分WAV）")]
    public AudioClip mainAudioClip;
    [Tooltip("再生開始時のフェードイン時間（秒）")]
    public float fadeInDuration = 2.0f;
    [Tooltip("再生終了時のフェードアウト時間（秒）")]
    public float fadeOutDuration = 10.0f;
    [Tooltip("マスターボリューム")]
    [Range(0f, 10f)]
    public float masterVolume = 1.0f;
    
    [Header("Spatial Settings")]
    [Tooltip("立体音響モードを有効にする")]
    public bool enableSpatialAudio = true;
    [Tooltip("音源の移動速度")]
    public float movementSpeed = 1.0f;
    [Tooltip("音源の最大移動半径")]
    public float maxRadius = 5.0f;
    [Tooltip("音源の初期位置")]
    public Vector3 initialPosition = new Vector3(0, 0, 5);
    [Tooltip("基本リバーブ量")]
    [Range(0f, 1f)]
    public float reverbAmount = 0.3f;
    
    [Header("Audio Processing")]
    [Tooltip("フェーズごとのピッチ変化を有効にする")]
    public bool enablePitchShifting = true;
    [Tooltip("ピッチの変化量")]
    [Range(0.5f, 1.5f)]
    public float pitchRange = 1.2f;
    [Tooltip("ローパスフィルター効果の強さ")]
    [Range(0f, 1f)]
    public float lowPassFilterAmount = 0.5f;
    
    [Header("Debug")]
    [Tooltip("音源の動きを可視化")]
    public bool visualizeAudioSource = true;
    [Tooltip("可視化時の色")]
    public Color visualizationColor = Color.cyan;
    
    // オーディオコンポーネント
    private AudioSource mainAudioSource;
    private AudioLowPassFilter lowPassFilter;
    private AudioHighPassFilter highPassFilter;
    private AudioReverbFilter reverbFilter;
    
    // 追加のオブジェクト
    private Transform audioSourceTransform;
    private GameObject visualizer;
    
    // 状態管理
    private bool isPlaying = false;
    private Coroutine currentMovementCoroutine;
    private Coroutine fadeCoroutine;
    
    // Unity ライフサイクル
    void Awake()
    {
        InitializeAudio();
    }
    
    void OnDestroy()
    {
        StopAllCoroutines();
    }
    
    /// <summary>
    /// オーディオシステムの初期化
    /// </summary>
    private void InitializeAudio()
    {
        // オーディオソース用のゲームオブジェクトを作成
        GameObject audioObj = new GameObject("SpatialAudioSource");
        audioObj.transform.SetParent(transform);
        audioObj.transform.localPosition = initialPosition;
        audioSourceTransform = audioObj.transform;
        
        // オーディオソースの設定
        mainAudioSource = audioObj.AddComponent<AudioSource>();
        mainAudioSource.clip = mainAudioClip;
        mainAudioSource.spatialBlend = enableSpatialAudio ? 1.0f : 0.0f; // 3D効果
        mainAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        mainAudioSource.minDistance = 1.0f;
        mainAudioSource.maxDistance = 50.0f;
        mainAudioSource.loop = false; // 3分で終了するので繰り返しなし
        mainAudioSource.volume = 0f; // 初期ボリュームは0
        mainAudioSource.playOnAwake = false;
        
        // エフェクトの追加
        lowPassFilter = audioObj.AddComponent<AudioLowPassFilter>();
        lowPassFilter.cutoffFrequency = 22000; // 初期は最大値（効果なし）
        lowPassFilter.enabled = false;
        
        highPassFilter = audioObj.AddComponent<AudioHighPassFilter>();
        highPassFilter.cutoffFrequency = 10; // 初期は最小値（効果なし）
        highPassFilter.enabled = false;
        
        reverbFilter = audioObj.AddComponent<AudioReverbFilter>();
        reverbFilter.reverbPreset = AudioReverbPreset.Off;
        reverbFilter.dryLevel = 0; // 原音はそのまま
        reverbFilter.room = -10000; // 効果なし（初期状態）
        reverbFilter.roomHF = -10000;
        reverbFilter.enabled = false;
        
        // 可視化オブジェクト（デバッグ用）
        if (visualizeAudioSource)
        {
            CreateVisualizer();
        }
    }
    
    /// <summary>
    /// オーディオ可視化オブジェクトの作成
    /// </summary>
    private void CreateVisualizer()
    {
        visualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualizer.name = "AudioVisualizer";
        visualizer.transform.SetParent(audioSourceTransform);
        visualizer.transform.localPosition = Vector3.zero;
        visualizer.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        // コライダーは不要
        Destroy(visualizer.GetComponent<Collider>());
        
        // 可視化用マテリアル
        Renderer renderer = visualizer.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", visualizationColor);
            material.SetColor("_BaseColor", visualizationColor);
            renderer.material = material;
        }
        
        visualizer.SetActive(visualizeAudioSource);
    }
    
    /// <summary>
    /// オーディオ再生開始
    /// </summary>
    public void StartAudio()
    {
        if (mainAudioSource == null || mainAudioClip == null)
        {
            Debug.LogError("オーディオソースまたはクリップが設定されていません");
            return;
        }
        
        if (isPlaying)
        {
            StopAudio(0.2f); // 既に再生中なら急速に停止
        }
        
        // 初期化
        audioSourceTransform.localPosition = initialPosition;
        mainAudioSource.time = 0f; // 最初から再生
        mainAudioSource.pitch = 1.0f;
        
        // エフェクト初期化
        lowPassFilter.cutoffFrequency = 22000;
        lowPassFilter.enabled = false;
        highPassFilter.cutoffFrequency = 10;
        highPassFilter.enabled = false;
        reverbFilter.reverbPreset = AudioReverbPreset.Off;
        reverbFilter.enabled = false;
        
        // 再生開始
        mainAudioSource.Play();
        isPlaying = true;
        
        // フェードイン
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeInAudio(fadeInDuration));
        
        Debug.Log("オーディオ再生を開始しました");
    }
    
    /// <summary>
    /// オーディオ再生停止
    /// </summary>
    public void StopAudio(float customFadeDuration = -1f)
    {
        if (!isPlaying) return;
        
        // 使用するフェードアウト時間を決定
        float useFadeDuration = customFadeDuration > 0f ? customFadeDuration : fadeOutDuration;
        
        // フェードアウト開始
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeOutAudio(useFadeDuration));
        
        Debug.Log("オーディオ再生を停止します");
    }
    
    /// <summary>
    /// フェーズと進行度に応じてオーディオを更新
    /// </summary>
    public void UpdateAudio(int phase, float phaseProgress)
    {
        if (!isPlaying) return;
        
        // フェーズに応じて音源の動きとエフェクトを変更
        switch (phase)
        {
            case 1: // 静寂の宇宙
                UpdatePhase1Audio(phaseProgress);
                break;
                
            case 2: // 変化の兆し
                UpdatePhase2Audio(phaseProgress);
                break;
                
            case 3: // 世界の崩壊
                UpdatePhase3Audio(phaseProgress);
                break;
                
            case 4: // 臨死ピーク
                UpdatePhase4Audio(phaseProgress);
                break;
                
            case 5: // ホワイトアウト
                UpdatePhase5Audio(phaseProgress);
                break;
                
            case 6: // エンディング
                UpdatePhase6Audio(phaseProgress);
                break;
        }
    }
    
    /// <summary>
    /// フェードインのコルーチン
    /// </summary>
    private IEnumerator FadeInAudio(float duration)
    {
        float startTime = Time.time;
        float startVolume = mainAudioSource.volume;
        
        while (Time.time < startTime + duration)
        {
            float elapsed = Time.time - startTime;
            float t = elapsed / duration;
            
            // イーズイン
            float smoothT = Mathf.SmoothStep(0, 1, t);
            mainAudioSource.volume = Mathf.Lerp(startVolume, masterVolume, smoothT);
            
            yield return null;
        }
        
        // 最終ボリュームに設定
        mainAudioSource.volume = masterVolume;
        fadeCoroutine = null;
    }
    
    /// <summary>
    /// フェードアウトのコルーチン
    /// </summary>
    private IEnumerator FadeOutAudio(float duration)
    {
        float startTime = Time.time;
        float startVolume = mainAudioSource.volume;
        
        while (Time.time < startTime + duration)
        {
            float elapsed = Time.time - startTime;
            float t = elapsed / duration;
            
            // イーズアウト
            float smoothT = Mathf.SmoothStep(0, 1, t);
            mainAudioSource.volume = Mathf.Lerp(startVolume, 0f, smoothT);
            
            yield return null;
        }
        
        // 完全に停止
        mainAudioSource.volume = 0f;
        mainAudioSource.Stop();
        isPlaying = false;
        
        // 音源移動もキャンセル
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
            currentMovementCoroutine = null;
        }
        
        fadeCoroutine = null;
    }
    
    /// <summary>
    /// フェーズ1のオーディオ更新（静寂の宇宙）
    /// </summary>
    private void UpdatePhase1Audio(float progress)
    {
        // 音源ポジション - 前方でほぼ静止
        Vector3 targetPosition = new Vector3(0, 0, 5);
        audioSourceTransform.localPosition = Vector3.Lerp(initialPosition, targetPosition, progress);
        
        // ピッチ - 標準
        if (enablePitchShifting)
        {
            mainAudioSource.pitch = 1.0f;
        }
        
        // エフェクト
        lowPassFilter.enabled = false;
        highPassFilter.enabled = false;
        
        // リバーブ - 広い空間
        if (progress > 0.2f)
        {
            reverbFilter.enabled = true;
            reverbFilter.reverbPreset = AudioReverbPreset.Mountains;
            reverbFilter.dryLevel = 0;
            reverbFilter.room = Mathf.Lerp(-10000, -1000, progress);
            reverbFilter.roomHF = Mathf.Lerp(-10000, -800, progress);
        }
    }
    
    /// <summary>
    /// フェーズ2のオーディオ更新（変化の兆し）
    /// </summary>
    private void UpdatePhase2Audio(float progress)
    {
        // 音源の動き - ゆっくりと周囲を移動
        StopCurrentMovement();
        currentMovementCoroutine = StartCoroutine(MoveInCirclePattern(
            center: Vector3.zero,
            radius: 2.0f + progress * 1.0f,
            height: 0f,
            duration: 5f,
            cycles: 0.5f
        ));
        
        // ピッチの微小変化
        if (enablePitchShifting)
        {
            float pitchModulation = 1.0f + Mathf.Sin(Time.time * 0.2f) * 0.05f * progress;
            mainAudioSource.pitch = pitchModulation;
        }
        
        // フィルター効果
        lowPassFilter.enabled = true;
        lowPassFilter.cutoffFrequency = Mathf.Lerp(22000, 10000, progress * lowPassFilterAmount);
        
        // リバーブ - 徐々に変化
        reverbFilter.enabled = true;
        reverbFilter.reverbPreset = AudioReverbPreset.Arena;
        reverbFilter.room = -1000;
        reverbFilter.roomHF = -500;
    }
    
    /// <summary>
    /// フェーズ3のオーディオ更新（世界の崩壊）
    /// </summary>
    private void UpdatePhase3Audio(float progress)
    {
        // 音源の動き - 不規則で複雑に
        StopCurrentMovement();
        currentMovementCoroutine = StartCoroutine(MoveInRandomPattern(
            center: Vector3.zero,
            maxDistance: 3.0f + progress * 2.0f,
            duration: 3f,
            intensity: 0.5f + progress * 0.5f
        ));
        
        // ピッチの変調（やや低く）
        if (enablePitchShifting)
        {
            float pitchModulation = Mathf.Lerp(1.0f, 0.9f, progress * 0.5f);
            // うねるような変化
            pitchModulation += Mathf.Sin(Time.time * 0.5f) * 0.1f * progress;
            mainAudioSource.pitch = pitchModulation;
        }
        
        // フィルター効果 - 強めのローパス
        lowPassFilter.enabled = true;
        lowPassFilter.cutoffFrequency = Mathf.Lerp(10000, 5000, progress * lowPassFilterAmount);
        
        // ハイパスも少し
        highPassFilter.enabled = true;
        highPassFilter.cutoffFrequency = Mathf.Lerp(10, 150, progress * 0.3f);
        
        // リバーブ - 閉じた空間
        reverbFilter.enabled = true;
        reverbFilter.reverbPreset = AudioReverbPreset.PaddedCell;
        reverbFilter.room = Mathf.Lerp(-1000, -2000, progress);
        reverbFilter.roomHF = -500;
    }
    
    /// <summary>
    /// フェーズ4のオーディオ更新（臨死ピーク）
    /// </summary>
    private void UpdatePhase4Audio(float progress)
    {
        // 音源の動き - 中心に向かう渦
        StopCurrentMovement();
        currentMovementCoroutine = StartCoroutine(MoveInSpiralPattern(
            center: Vector3.zero,
            startRadius: 5.0f,
            endRadius: 0.5f,
            duration: 5f,
            rotations: 3f,
            startHeight: 1f,
            endHeight: 0f
        ));
        
        // ピッチ - うねりと共に低く
        if (enablePitchShifting)
        {
            float pitchBase = Mathf.Lerp(0.9f, 0.8f, progress); // 徐々に低く
            float pitchWave = Mathf.Sin(Time.time * 1.0f) * 0.1f * progress; // うねり
            mainAudioSource.pitch = pitchBase + pitchWave;
        }
        
        // フィルター効果 - ピーク付近で激しく変化
        lowPassFilter.enabled = true;
        if (progress < 0.8f)
        {
            lowPassFilter.cutoffFrequency = Mathf.Lerp(5000, 1000, progress);
        }
        else
        {
            // 急激な変化
            float t = (progress - 0.8f) / 0.2f; // 0～1に正規化
            lowPassFilter.cutoffFrequency = Mathf.Lerp(1000, 10000, t);
        }
        
        highPassFilter.enabled = true;
        highPassFilter.cutoffFrequency = Mathf.Lerp(150, 50, progress);
        
        // リバーブ - 徐々に大きな空間へ
        reverbFilter.enabled = true;
        if (progress < 0.9f)
        {
            reverbFilter.reverbPreset = AudioReverbPreset.Cave;
            reverbFilter.room = Mathf.Lerp(-2000, 0, progress);
        }
        else
        {
            // 最後の瞬間に変化
            reverbFilter.reverbPreset = AudioReverbPreset.Auditorium;
            reverbFilter.room = 0;
        }
    }
    
    /// <summary>
    /// フェーズ5のオーディオ更新（ホワイトアウト）
    /// </summary>
    private void UpdatePhase5Audio(float progress)
    {
        // 静寂の瞬間（0.1秒）
        if (progress < 0.05f)
        {
            mainAudioSource.volume = 0f;
            return;
        }
        else if (progress < 0.1f)
        {
            // 急速に戻す
            mainAudioSource.volume = Mathf.Lerp(0f, masterVolume, (progress - 0.05f) * 20f);
        }
        else
        {
            mainAudioSource.volume = masterVolume;
        }
        
        // 音源の動き - 全方位から聞こえるように
        StopCurrentMovement();
        currentMovementCoroutine = StartCoroutine(MoveSurroundListener(
            radius: 3.0f,
            height: Mathf.Lerp(0f, 2f, progress),
            duration: 8f,
            rotations: 2f
        ));
        
        // ピッチ - 高く明るく
        if (enablePitchShifting)
        {
            float pitchBase = Mathf.Lerp(1.0f, 1.1f, progress);
            mainAudioSource.pitch = pitchBase;
        }
        
        // フィルター - フルスペクトラム
        lowPassFilter.enabled = true;
        lowPassFilter.cutoffFrequency = Mathf.Lerp(10000, 22000, progress);
        
        highPassFilter.enabled = false;
        
        // リバーブ - 巨大空間
        reverbFilter.enabled = true;
        reverbFilter.reverbPreset = AudioReverbPreset.Auditorium;
        reverbFilter.room = 0;
        reverbFilter.roomHF = 0;
    }
    
    /// <summary>
    /// フェーズ6のオーディオ更新（エンディング）
    /// </summary>
    private void UpdatePhase6Audio(float progress)
    {
        // 音源の動き - 前方、そして徐々に遠ざかる
        Vector3 targetPosition = new Vector3(0, 0, 5 + progress * 10);
        audioSourceTransform.localPosition = Vector3.Lerp(
            new Vector3(0, 2f, 3f),
            targetPosition,
            progress
        );
        
        // ピッチ - 通常に戻る
        if (enablePitchShifting)
        {
            mainAudioSource.pitch = Mathf.Lerp(1.1f, 1.0f, progress);
        }
        
        // フィルター効果 - 徐々に滑らかに
        lowPassFilter.enabled = true;
        lowPassFilter.cutoffFrequency = 22000; // フル
        
        highPassFilter.enabled = false;
        
        // リバーブ - 徐々に消える
        reverbFilter.enabled = true;
        reverbFilter.reverbPreset = AudioReverbPreset.Mountains;
        reverbFilter.room = Mathf.Lerp(0, -10000, progress);
        reverbFilter.roomHF = Mathf.Lerp(0, -10000, progress);
    }
    
    /// <summary>
    /// 現在の音源移動コルーチンを停止
    /// </summary>
    private void StopCurrentMovement()
    {
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
            currentMovementCoroutine = null;
        }
    }
    
    /// <summary>
    /// 円形パターンで音源を移動するコルーチン
    /// </summary>
    private IEnumerator MoveInCirclePattern(Vector3 center, float radius, float height, float duration, float cycles)
    {
        float startTime = Time.time;
        Vector3 startPos = audioSourceTransform.localPosition;
        
        while (Time.time < startTime + duration)
        {
            float elapsed = Time.time - startTime;
            float progress = elapsed / duration; // 0～1
            
            // 円運動の計算
            float angle = progress * cycles * Mathf.PI * 2;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            Vector3 targetPos = new Vector3(x, height, z);
            
            // 現在位置から目標位置へ滑らかに移動
            audioSourceTransform.localPosition = Vector3.Lerp(startPos, targetPos, progress);
            
            yield return null;
        }
    }
    
    /// <summary>
    /// ランダムなパターンで音源を移動するコルーチン
    /// </summary>
    private IEnumerator MoveInRandomPattern(Vector3 center, float maxDistance, float duration, float intensity)
    {
        Vector3 startPos = audioSourceTransform.localPosition;
        Vector3 currentTarget = startPos;
        float changeInterval = 0.5f;
        float lastChangeTime = Time.time;
        
        float endTime = Time.time + duration;
        
        while (Time.time < endTime)
        {
            // 一定間隔で新しい目標位置を設定
            if (Time.time - lastChangeTime > changeInterval)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(-1f, 1f)
                ).normalized * maxDistance * intensity;
                
                currentTarget = center + randomOffset;
                lastChangeTime = Time.time;
                changeInterval = Random.Range(0.3f, 0.8f);
            }
            
            // 現在位置から目標位置へ移動
            audioSourceTransform.localPosition = Vector3.Lerp(
                audioSourceTransform.localPosition,
                currentTarget,
                Time.deltaTime * 3.0f
            );
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 渦巻きパターンで音源を移動するコルーチン
    /// </summary>
    private IEnumerator MoveInSpiralPattern(Vector3 center, float startRadius, float endRadius, float duration, float rotations, float startHeight, float endHeight)
    {
        float startTime = Time.time;
        Vector3 startPos = audioSourceTransform.localPosition;
        
        while (Time.time < startTime + duration)
        {
            float elapsed = Time.time - startTime;
            float progress = elapsed / duration; // 0～1
            
            // 螺旋運動の計算
            float radius = Mathf.Lerp(startRadius, endRadius, progress);
            float angle = progress * rotations * Mathf.PI * 2;
            float height = Mathf.Lerp(startHeight, endHeight, progress);
            
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            Vector3 targetPos = new Vector3(x, height, z);
            
            // イーズイン・アウト
            float smoothProgress = Mathf.SmoothStep(0, 1, progress);
            audioSourceTransform.localPosition = Vector3.Lerp(startPos, targetPos, smoothProgress);
            
            yield return null;
        }
    }
    
    /// <summary>
    /// リスナーを囲むように音源を移動するコルーチン
    /// </summary>
    private IEnumerator MoveSurroundListener(float radius, float height, float duration, float rotations)
    {
        float startTime = Time.time;
        Vector3 startPos = audioSourceTransform.localPosition;
        
        while (Time.time < startTime + duration)
        {
            float elapsed = Time.time - startTime;
            float progress = elapsed / duration; // 0～1
            
            // 周回運動と高さの変化
            float angle = progress * rotations * Mathf.PI * 2;
            float currentHeight = height * Mathf.Sin(progress * Mathf.PI); // 上昇して下降
            
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            Vector3 targetPos = new Vector3(x, currentHeight, z);
            
            // 滑らかな移動（startPosからの補間はしない）
            audioSourceTransform.localPosition = targetPos;
            
            yield return null;
        }
    }
}