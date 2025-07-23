using UnityEngine;
using System.IO;
using System;

public class UnityLogger : MonoBehaviour
{
    private static string logFilePath;
    private static StreamWriter logWriter;

    void Awake()
    {
        // Определяем путь к файлу лога
        logFilePath = Path.Combine(Application.dataPath, "unity-activity.log");
        
        // Создаем или открываем файл для записи
        try
        {
            logWriter = new StreamWriter(logFilePath, append: true);
            logWriter.AutoFlush = true;
            
            // Подписываемся на события логирования
            Application.logMessageReceived += OnLogMessageReceived;
            
            Debug.Log($"[UnityLogger] Logging started to: {logFilePath}");
            WriteLog("INFO", "UnityLogger", "=== Unity Logging Started ===");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLogger] Failed to initialize logging: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        Application.logMessageReceived -= OnLogMessageReceived;
        
        // Закрываем файл
        if (logWriter != null)
        {
            WriteLog("INFO", "UnityLogger", "=== Unity Logging Stopped ===");
            logWriter.Close();
            logWriter = null;
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            WriteLog("INFO", "UnityLogger", "=== Unity Application Paused ===");
        }
        else
        {
            WriteLog("INFO", "UnityLogger", "=== Unity Application Resumed ===");
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            WriteLog("INFO", "UnityLogger", "=== Unity Application Focused ===");
        }
        else
        {
            WriteLog("INFO", "UnityLogger", "=== Unity Application Lost Focus ===");
        }
    }

    private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
    {
        string level = type switch
        {
            LogType.Error => "ERROR",
            LogType.Assert => "ERROR",
            LogType.Warning => "WARN",
            LogType.Log => "INFO",
            LogType.Exception => "ERROR",
            _ => "INFO"
        };

        // Извлекаем имя источника лога из сообщения
        string source = "Unity";
        if (logString.Contains("[") && logString.Contains("]"))
        {
            int start = logString.IndexOf("[") + 1;
            int end = logString.IndexOf("]");
            if (end > start)
            {
                source = logString.Substring(start, end - start);
            }
        }

        WriteLog(level, source, logString);
        
        // Добавляем stack trace для ошибок
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (!string.IsNullOrEmpty(stackTrace))
            {
                WriteLog(level, source, $"Stack trace: {stackTrace}");
            }
        }
    }

    private void WriteLog(string level, string source, string message)
    {
        if (logWriter == null) return;
        
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logEntry = $"{timestamp} {level} {source} {message}";
            logWriter.WriteLine(logEntry);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLogger] Failed to write log: {ex.Message}");
        }
    }
} 