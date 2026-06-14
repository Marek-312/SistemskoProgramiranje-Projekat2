using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PalindromeServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // string LoggerPath = "server.Logger";
            string rootFolder = AppDomain.CurrentDomain.BaseDirectory;
            int workerCount = 4;
            int cacheSize = 50;
            TimeSpan cacheTtl = TimeSpan.FromSeconds(60);

            //Loggerger Logger = new Loggerger(LoggerPath);
            //Logger.;

            using CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Logger.Log("Ctrl+C detektovan, gasenje servera...", Logger.Metode.Info, "MAIN");
                cts.Cancel();
            };

            try
            {
                Cache<string, SearchResult> cache = new Cache<string, SearchResult>(cacheTtl, 5);

                RequestQueue requestQueue = new RequestQueue(cts.Token);

                FileSearcher fileSearcher = new FileSearcher(rootFolder);

                RequestProcessor processor = new RequestProcessor(
                    requestQueue,
                    fileSearcher,
                    cache,
                    cts.Token,
                    workerCount);

                HttpListener listener = new HttpListener();
                Server server = new Server(listener, requestQueue, cts.Token);

                List<Task> workerTasks = processor.Start();
                server.Start();

                Logger.Log($"Server slusа na http://localhost:5050/", Logger.Metode.Info, "MAIN");
                Logger.Log($"Worker taskovi: {workerCount}", Logger.Metode.Info, "MAIN");
                Logger.Log($"Root folder: {rootFolder}", Logger.Metode.Info, "MAIN");
                Logger.Log("Pritisnite Ctrl+C za gasenje.", Logger.Metode.Info, "MAIN");

                try
                {
                    Task.WaitAll(workerTasks.ToArray(), cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Logger.Log("Worker taskovi zaustavljeni.", Logger.Metode.Info, "MAIN");
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Fatalna greska pri pokretanju: {e.Message}", Logger.Metode.Error, "MAIN");
            }
            finally
            {
                Logger.Log("Server ugasen.", Logger.Metode.Info, "MAIN");
                //Logger.Dispose();
            }
        }
    }
}