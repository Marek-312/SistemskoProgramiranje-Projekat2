using System;
using System.Threading;
using System.IO;

namespace PalindromeServer
{
    public class Logger
    {
        private readonly static Object _loggerLock = new object();
        private readonly static string path = "server.Logge";
        private readonly static StreamWriter _streamWriter = new StreamWriter(path);

        //public readonly string path;
        public enum Metode
        {
            Info,
            Warning,
            Error,
        }

        public static void Log(string message, Metode metoda, string identifikator)
        {
            string logMessage;
            _streamWriter.AutoFlush = true;
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
        /* public static void Dispose()
         {
             lock (_loggerLock)
             {
                 _streamWriter?.Dispose();
             }
         }*/
    }
}