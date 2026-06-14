using System;
using System.Threading;
using System.IO;

namespace PalindromeServer
{
    public class Logger : IDisposable
    {
        private readonly static Object _loggerLock;
        private readonly static StreamWriter _streamWriter;
        //public readonly string path;
        public enum Metode
        {
            Info,
            Warning,
            Error,
        }
        public Logger(string path)
        {

            _loggerLock = new object();
            _streamWriter = new StreamWriter(path);
            if (_streamWriter != null)
                _streamWriter.AutoFlush = true;


        }
        public static void Log(string message, Metode metoda, string identifikator)
        {
            string logMessage;
            if (metoda == Metode.Info)
            {
                logMessage = "[Info] " + identifikator + " " + message + " time " + DateTime.UtcNow.ToString();
            }
            else if (metoda == Metode.Warning)
            {
                logMessage = "[Warning] " + identifikator + " " + message + " time " + DateTime.UtcNow.ToString();
            }
            else
            {
                logMessage = "[Error] " + identifikator + " " + message + " time " + DateTime.UtcNow.ToString();
            }

            lock (_loggerLock)
            {
                _streamWriter.WriteLine(logMessage);
            }

        }
        public void Dispose()
        {
            lock (_loggerLock)
            {
                _streamWriter?.Dispose();
            }
        }
    }
}