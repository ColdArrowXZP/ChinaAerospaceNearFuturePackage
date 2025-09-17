using SaveUpgradePipeline;
using System;
using System. Collections. Generic;
using System. IO;
using System. Text;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Core. Managers
{
    [KSPAddon( KSPAddon.Startup. MainMenu, true )]
    public class CASNFPLogger : MonoBehaviour
    {
        public static CASNFPLogger Instance
        {
            get; private set;
        }
        [Header ("日志设置")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private string logFileName = "CASNFPLog.txt";
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private bool logToFile = true;
        [SerializeField] private int maxLogEntries = 1000;
        [SerializeField] private LogType minimumLogLevel = LogType. Log;
        private List<LogEntry> logEntries = new List<LogEntry> ();
        private string _logPath;
        private bool isInitialized = false;

        [Serializable]
        private class LogEntry
        {
            public string message;
            public LogType logType;
            public DateTime timestamp;
            public string stackTrace;

            public LogEntry (string message, LogType logType, string stackTrace = "")
            {
                this. message = message;
                this. logType = logType;
                this. timestamp = DateTime. Now;
                this. stackTrace = stackTrace;
            }
        }

        private void Awake ()
        {
            if ( Instance == null )
            {
                Instance = this;
                DontDestroyOnLoad (gameObject);
                InitializeLogger ();
            }
            else
            {
                Destroy (gameObject);
            }
        }

        private void InitializeLogger ()
        {
            if ( isInitialized )
                return;

            // 创建日志目录
            string logDirectory = Path. Combine (CASNFP_Globals.AssemblyPath, @"Logs\");
            if ( !Directory. Exists (logDirectory) )
            {
                Directory. CreateDirectory (logDirectory);
            }

            // 设置日志文件路径
            _logPath = Path. Combine (logDirectory, logFileName);

            if ( File. Exists (_logPath) )
            {
                using ( var stream = File. Open (_logPath, FileMode. Open, FileAccess. Write) )
                {
                    stream. SetLength (0);
                    stream. Close ();
                }
            }

            //初始化日志条目列表
            logEntries.Clear ();

            isInitialized = true;

            Log ("日志系统已初始化", LogType. Log);
        }

        public void Log (string message, LogType logType = LogType. Log)
        {
            if ( !enableLogging || logType < minimumLogLevel )
                return;

            LogEntry entry = new LogEntry (message, logType);
            logEntries. Add (entry);

            // 确保日志条目数量不超过最大值
            if ( logEntries. Count > maxLogEntries )
            {
                logEntries. RemoveAt (0);
            }

            // 输出到控制台
            if ( logToConsole )
            {
                switch ( logType )
                {
                    case LogType. Error:
                        Debug. LogError ($"[{entry. timestamp:yyyy-MM-dd HH:mm:ss}] {message}");
                        break;
                    case LogType. Warning:
                        Debug. LogWarning ($"[{entry. timestamp:yyyy-MM-dd HH:mm:ss}] {message}");
                        break;
                    case LogType. Log:
                    default:
                        Debug. Log ($"[{entry. timestamp:yyyy-MM-dd HH:mm:ss}] {message}");
                        break;
                }
            }

            // 写入文件
            if ( logToFile )
            {
                WriteLogToFile (entry);
            }
        }

        private void WriteLogToFile (LogEntry entry)
        {
            try
            {
                string logLine = $"[{entry. timestamp:yyyy-MM-dd HH:mm:ss}] [{entry. logType}] {entry. message}";

                // 如果是错误或异常，添加堆栈跟踪
                if ( entry. logType == LogType. Error || entry. logType == LogType. Exception )
                {
                    logLine += $"\n{entry. stackTrace}";
                }

                File. AppendAllText (_logPath, logLine + "\n");
            }
            catch ( Exception e )
            {
                Debug. LogError ($"写入日志文件失败: {e. Message}");
            }
        }

        public void LogWarning (string message)
        {
            Log (message, LogType. Warning);
        }

        public void LogError (string message)
        {
            Log (message, LogType. Error);
        }

        public void LogException (Exception exception)
        {
            Log (exception. Message, LogType. Exception);
            Log (exception. StackTrace, LogType. Exception);
        }

        private List<LogEntry> GetLogs (LogType? filterType = null, int count = -1)
        {
            List<LogEntry> result = new List<LogEntry> ();

            foreach ( LogEntry entry in logEntries )
            {
                if ( filterType == null || entry. logType == filterType )
                {
                    result. Add (entry);
                }
            }

            if ( count > 0 && result. Count > count )
            {
                result. RemoveRange (0, result. Count - count);
            }

            return result;
        }

        public void ClearLogs ()
        {
            logEntries. Clear ();

            if ( File. Exists (_logPath) )
            {
                File. Delete (_logPath);
            }

            Log ("日志已清除", LogType. Log);
        }

        // 确保在应用退出时保存所有日志
        private void OnApplicationQuit ()
        {
            SaveAllLogs ();
        }

        private void OnDisable ()
        {
            SaveAllLogs ();
        }

        private void SaveAllLogs ()
        {
            if ( !logToFile || !enableLogging )
                return;

            try
            {
                using ( StreamWriter writer = new StreamWriter (_logPath, false) )
                {
                    foreach ( LogEntry entry in logEntries )
                    {
                        string logLine = $"[{entry. timestamp:yyyy-MM-dd HH:mm:ss}] [{entry. logType}] {entry. message}";

                        if ( entry. logType == LogType. Error || entry. logType == LogType. Exception )
                        {
                            logLine += $"\n{entry. stackTrace}";
                        }

                        writer. WriteLine (logLine);
                    }
                }
            }
            catch ( Exception e )
            {
                Debug. LogError ($"保存日志失败: {e. Message}");
            }
        }
    }
}