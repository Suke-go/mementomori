// VibrationController.cs
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

/// <summary>
/// 振動コントローラー（OSC通信で外部システムと連携）
/// </summary>
public class VibrationController : MonoBehaviour
{
    [Header("OSC Settings")]
    [Tooltip("送信先ホスト")]
    public string destinationHost = "127.0.0.1";
    [Tooltip("OSC送信ポート")]
    public int sendPort = 8001;
    [Tooltip("通信ログを表示")]
    public bool debugLog = false;
    [Tooltip("通信間隔（秒）")]
    public float updateInterval = 0.1f;
    
    [Header("Vibration Parameters")]
    [Tooltip("振動強度の倍率")]
    [Range(0f, 2f)]
    public float intensityMultiplier = 1.0f;
    [Tooltip("振動周波数の倍率")]
    [Range(0f, 2f)]
    public float frequencyMultiplier = 1.0f;
    
    // 内部変数
    private UdpClient client;
    private float lastSendTime = 0f;
    private float currentIntensity = 0f;
    private float currentFrequency = 0f;
    
    // Unity ライフサイクル
    void Start()
    {
        InitializeOSC();
    }
    
    void OnDestroy()
    {
        CloseOSC();
    }
    
    /// <summary>
    /// OSC通信の初期化
    /// </summary>
    private void InitializeOSC()
    {
        try
        {
            client = new UdpClient();
            if (debugLog) Debug.Log($"OSC通信を初期化しました。送信先: {destinationHost}:{sendPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"OSC初期化エラー: {e.Message}");
        }
    }
    
    /// <summary>
    /// OSC通信の終了
    /// </summary>
    private void CloseOSC()
    {
        if (client != null)
        {
            try
            {
                // 振動停止コマンドを送信
                SendVibrationInfo(0f, 0f, true);
                client.Close();
                client = null;
                
                if (debugLog) Debug.Log("OSC通信を終了しました");
            }
            catch (Exception e)
            {
                Debug.LogError($"OSC終了エラー: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 振動情報を送信
    /// </summary>
    public void SendVibrationInfo(float intensity, float frequency, bool forceSend = false)
    {
        // 値の範囲制限
        intensity = Mathf.Clamp01(intensity) * intensityMultiplier;
        frequency = Mathf.Clamp(frequency, 0f, 1f) * frequencyMultiplier;
        
        // 前回の送信から一定時間が経過していない場合はスキップ（負荷軽減）
        if (!forceSend && Time.time - lastSendTime < updateInterval)
        {
            // 値を保存しておく
            currentIntensity = intensity;
            currentFrequency = frequency;
            return;
        }
        
        // クライアントが初期化されていなければスキップ
        if (client == null)
        {
            InitializeOSC();
            if (client == null) return;
        }
        
        try
        {
            // OSCメッセージのフォーマット: /vibration {intensity} {frequency}
            string message = $"/vibration {intensity.ToString("F2")} {frequency.ToString("F2")}";
            byte[] data = Encoding.ASCII.GetBytes(message);
            
            client.Send(data, data.Length, destinationHost, sendPort);
            lastSendTime = Time.time;
            
            if (debugLog && (forceSend || Time.frameCount % 60 == 0)) // 通常のログは60フレームごとに
            {
                Debug.Log($"振動情報を送信: 強度={intensity:F2}, 周波数={frequency:F2}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"OSC送信エラー: {e.Message}");
            
            // 送信に失敗した場合はクライアントを再初期化
            if (client != null)
            {
                client.Close();
                client = null;
                InitializeOSC();
            }
        }
    }
    
    /// <summary>
    /// 開始メッセージを送信
    /// </summary>
    public void SendStartMessage()
    {
        if (client == null)
        {
            InitializeOSC();
            if (client == null) return;
        }
        
        try
        {
            string message = "/start";
            byte[] data = Encoding.ASCII.GetBytes(message);
            
            client.Send(data, data.Length, destinationHost, sendPort);
            lastSendTime = Time.time;
            
            if (debugLog) Debug.Log("開始メッセージを送信");
        }
        catch (Exception e)
        {
            Debug.LogError($"OSC送信エラー: {e.Message}");
        }
    }
    
    /// <summary>
    /// 終了メッセージを送信
    /// </summary>
    public void SendEndMessage()
    {
        if (client == null)
        {
            InitializeOSC();
            if (client == null) return;
        }
        
        try
        {
            string message = "/end";
            byte[] data = Encoding.ASCII.GetBytes(message);
            
            client.Send(data, data.Length, destinationHost, sendPort);
            lastSendTime = Time.time;
            
            // 振動も停止
            SendVibrationInfo(0f, 0f, true);
            
            if (debugLog) Debug.Log("終了メッセージを送信");
        }
        catch (Exception e)
        {
            Debug.LogError($"OSC送信エラー: {e.Message}");
        }
    }
    
    /// <summary>
    /// 毎フレームの更新時に呼ばれる
    /// </summary>
    void Update()
    {
        // 前回の送信から一定時間経過した場合、保存しておいた値を送信
        if (Time.time - lastSendTime >= updateInterval)
        {
            SendVibrationInfo(currentIntensity, currentFrequency);
        }
    }
    
    /// <summary>
    /// フェーズと進行度から振動パラメータを計算して送信
    /// </summary>
    public void UpdateVibrationFromPhase(int phase, float phaseProgress)
    {
        float intensity = 0f;
        float frequency = 0f;
        
        // フェーズ別の振動パラメータ計算
        switch (phase)
        {
            case 1: // 静寂の宇宙 - わずかな振動
                intensity = Mathf.Min(0.1f, phaseProgress * 0.1f);
                frequency = 0.2f + phaseProgress * 0.1f;
                break;
                
            case 2: // 変化の兆し - 徐々に強まる
                intensity = 0.1f + phaseProgress * 0.1f;
                frequency = 0.3f + phaseProgress * 0.1f;
                break;
                
            case 3: // 世界の崩壊 - 不規則に強まる
                intensity = 0.2f + phaseProgress * 0.2f;
                frequency = 0.4f + phaseProgress * 0.3f;
                
                // 不規則な変動を追加
                float randomVariation = Mathf.PerlinNoise(Time.time * 2f, 0) * 0.1f * phaseProgress;
                intensity += randomVariation;
                break;
                
            case 4: // 臨死ピーク - 最大の強度に達する
                intensity = 0.4f + phaseProgress * 0.2f;
                frequency = 0.3f - phaseProgress * 0.1f; // 周波数は徐々に低く
                
                // ピーク付近で特に強く
                if (phaseProgress > 0.9f && phaseProgress < 0.95f)
                {
                    intensity = 0.7f;
                    frequency = 0.25f;
                }
                break;
                
            case 5: // ホワイトアウト - 静寂の後に波のような振動
                // 最初の一瞬は完全に静寂
                if (phaseProgress < 0.1f)
                {
                    intensity = 0f;
                    frequency = 0f;
                }
                else
                {
                    // 波のようなパターンで振動
                    float wavePattern = Mathf.Sin(phaseProgress * 10f) * 0.5f + 0.5f;
                    intensity = 0.15f + wavePattern * 0.1f;
                    frequency = 0.1f + wavePattern * 0.05f;
                }
                break;
                
            case 6: // エンディング - 徐々に消えていく
                intensity = Mathf.Max(0f, 0.1f - phaseProgress * 0.1f);
                frequency = Mathf.Max(0.05f, 0.08f - phaseProgress * 0.03f);
                break;
        }
        
        // 計算した振動パラメータを送信
        SendVibrationInfo(intensity, frequency);
    }
}