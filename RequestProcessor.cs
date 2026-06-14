using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
namespace PalindromeServer
{
    public class RequestProcessor
    {
        public readonly RequestQueue _requestQueue;
        public readonly FileSearcher _fileSearcher;

        public readonly Cache<string, int> _cache;
        public readonly CancellationToken _token;

        public readonly int _workerCount;

        public RequestProcessor(RequestQueue requestQueue, FileSearcher fileSearcher, Cache<string, int> cache, CancellationToken token, int WorkerCount = 4)
        {
            _requestQueue = requestQueue;
            _fileSearcher = fileSearcher;
            _cache = cache;
            _token = requestQueue._token;
            _workerCount = WorkerCount;
        }
        public async Task<List<Task>> Start()
        {
            List<Task> task = new List<Task>(_workerCount);
            for (int i = 0; i < _workerCount; i++)
            {
                task[i] = Task.Run(() => Worker());
            }

            for (int i = 0; i < _workerCount; i++)
            {

            }

            return task;
        }
        private void Worker()
        {
            try
            {
                HttpListenerContext context = _requestQueue.removeContext();
                string path = context.Request.Url.AbsolutePath;
                path.TrimStart();
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new Exception("Prazan path");
                }

                if (_cache.TryGet(path, out int value))
                {
                    Logger.Log("KES HIT", Logger.Metode.Info, "Request processor");
                    return;
                }
                else
                {
                    Task<SearchResult> searchResult = _fileSearcher.SearchAsync(path);
                    searchResult.ContinueWith(t => _cache.Add(t.Result.FileName, t.Result.PalindromeCount));
                    .ContinueWith(s => SendResponse(+));

                    //return fileReaserhced.Result.PalindromeCount;

                }

            }
            catch (OperationCanceledException)
            {
                Logger.Log("Cancelation token activated", Logger.Metode.Error, "Request processor");
            }
            catch (Exception e)
            {
                Logger.Log(e.Message, Logger.Metode.Error, "Request processor");
            }
        }


    }
}