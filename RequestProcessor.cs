using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PalindromeServer
{
    public class RequestProcessor
    {
        private readonly RequestQueue _requestQueue;
        private readonly FileSearcher _fileSearcher;
        private readonly Cache<string, SearchResult> _cache;
        private readonly CancellationToken _token;
        private readonly Logger Logger;
        private readonly int _workerCount;

        public RequestProcessor(
            RequestQueue requestQueue,
            FileSearcher fileSearcher,
            Cache<string, SearchResult> cache,
            Logger log,
            CancellationToken token,
            int workerCount = 4)
        {
            _requestQueue = requestQueue;
            _fileSearcher = fileSearcher;
            _cache = cache;
            Logger = log;
            _token = token;
            _workerCount = workerCount;
        }

        public List<Task> Start()
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < _workerCount; i++)
            {
                tasks.Add(Task.Run(() => Worker(), _token));
            }
            return tasks;
        }

        private void Worker()
        {
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context = _requestQueue.removeContext();
                    string path = context.Request.Url.AbsolutePath.TrimStart('/');

                    if (string.IsNullOrWhiteSpace(path))
                    {
                        SendError(context, 400, "Naziv fajla ne sme biti prazan.");
                        continue;
                    }

                    Logger.Log($"Obrada zahteva za fajl: {path}", Logger.Metode.Info, "PROCESSOR");

                    if (_cache.TryGet(path, out SearchResult cached))
                    {
                        Logger.Log($"Kes hit za: {path}", Logger.Metode.Info, "PROCESSOR");
                        SendResponse(context, cached);
                        continue;
                    }

                    _fileSearcher.SearchAsync(path)
                        .ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                Logger.Log($"Greska pri pretrazi: {t.Exception?.InnerException?.Message}", Logger.Metode.Error, "PROCESSOR");
                                SendError(context, 404, $"Fajl '{path}' nije pronadjen.");
                                return;
                            }
                            _cache.Add(path, t.Result);
                            Logger.Log($"Rezultat kaciran za: {path}", Logger.Metode.Info, "PROCESSOR");
                        }, TaskScheduler.Default)
                        .ContinueWith(t =>
                        {
                            if (_cache.TryGet(path, out SearchResult result))
                            {
                                SendResponse(context, result);
                            }
                        }, TaskScheduler.Default);
                }
                catch (OperationCanceledException)
                {
                    Logger.Log("Worker zaustavljen.", Logger.Metode.Info, "PROCESSOR");
                    break;
                }
                catch (Exception e)
                {
                    Logger.Log($"Neocekivana greska: {e.Message}", Logger.Metode.Error, "PROCESSOR");
                }
            }
        }

        private void SendResponse(HttpListenerContext context, SearchResult result)
        {
            try
            {
                string body;
                if (result.PalindromeCount == 0)
                {
                    body = $"U fajlu '{result.FileName}' nisu pronadjeni palindromi.";
                }
                else
                {
                    string lista = string.Join(", ", result.Palindromes);
                    body = $"U fajlu '{result.FileName}' pronadjeno je {result.PalindromeCount} palindroma: {lista}";
                }

                byte[] buffer = Encoding.UTF8.GetBytes(body);
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }

        private void SendError(HttpListenerContext context, int statusCode, string message)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }
    }
}