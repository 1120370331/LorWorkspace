using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Steria
{
    public static class SteriaLogger
    {
        private static string _logFilePath;
        private static bool _initialized = false;
        private static bool _initFailed = false;
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            if (_initialized || _initFailed) return;

            try
            {
                string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    _initFailed = true;
                    return;
                }

                string assemblyDir = Path.GetDirectoryName(assemblyLocation);
                string modRootPath = Directory.GetParent(assemblyDir)?.FullName ?? assemblyDir;
                _logFilePath = Path.Combine(modRootPath, "Steria.log");

                lock (_lock)
                {
                    File.WriteAllText(_logFilePath, $"=== Steria Mod Log ===\nStarted: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");
                }

                _initialized = true;
                Debug.Log($"[Steria] Logger initialized: {_logFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Steria] Logger init failed: {ex.Message}");
                _initFailed = true;
            }
        }

        public static void Log(string message)
        {
            try
            {
                Debug.Log($"[Steria] {message}");
                if (_initialized) WriteToFile($"[{DateTime.Now:HH:mm:ss}] [INFO] {message}");
            }
            catch { }
        }

        public static void LogWarning(string message)
        {
            try
            {
                Debug.LogWarning($"[Steria] {message}");
                if (_initialized) WriteToFile($"[{DateTime.Now:HH:mm:ss}] [WARN] {message}");
            }
            catch { }
        }

        public static void LogError(string message)
        {
            try
            {
                Debug.LogError($"[Steria] {message}");
                if (_initialized) WriteToFile($"[{DateTime.Now:HH:mm:ss}] [ERROR] {message}");
            }
            catch { }
        }

        private static void WriteToFile(string message)
        {
            if (!_initialized || string.IsNullOrEmpty(_logFilePath)) return;
            try
            {
                lock (_lock) { File.AppendAllText(_logFilePath, message + "\n"); }
            }
            catch { }
        }
    }
}
