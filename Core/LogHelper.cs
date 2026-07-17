using System;
using System.IO;
using System.Text;

namespace CadToShpGISSystem.Core
{
    /// <summary>
    /// 简单日志工具：同时支持写入文本文件和推送到界面日志窗口。
    /// 日志格式：时间 | 模块 | 等级 | 内容。
    /// </summary>
    public static class LogHelper
    {
        private static readonly object SyncRoot = new object();

        public static event Action<string> LogAdded;

        public static string LogDirectory
        {
            get
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                return dir;
            }
        }

        public static void Info(string module, string message)
        {
            Write(module, "INFO", message, null);
        }

        public static void Warn(string module, string message)
        {
            Write(module, "WARN", message, null);
        }

        public static void Error(string module, string message, Exception exception)
        {
            Write(module, "ERROR", message, exception);
        }

        private static void Write(string module, string level, string message, Exception exception)
        {
            string fullMessage = string.Format(
                "{0:yyyy-MM-dd HH:mm:ss} | {1} | {2} | {3}",
                DateTime.Now,
                module,
                level,
                message);

            if (exception != null)
            {
                fullMessage += Environment.NewLine + exception;
            }

            lock (SyncRoot)
            {
                string logFile = Path.Combine(LogDirectory, DateTime.Now.ToString("yyyyMMdd") + ".txt");
                File.AppendAllText(logFile, fullMessage + Environment.NewLine, Encoding.UTF8);
            }

            Action<string> handler = LogAdded;
            if (handler != null)
            {
                handler(fullMessage);
            }
        }
    }
}
