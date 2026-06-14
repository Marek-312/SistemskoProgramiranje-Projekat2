using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace PalindromeServer
{
    public class Server
    {
        private readonly HttpListener _listener;
        private readonly RequestQueue _requestQueue;
        private readonly CancellationToken _token;
        public Server(HttpListener listener, RequestQueue queue, CancellationToken token)
        {
            _listener = listener;
            _listener.Prefixes.Add("http://localhost:5050/");
            _requestQueue = queue;
            _token = token;
        }
        public void Start()
        {
            Thread t = new Thread(new ThreadStart(Listen));
            t.IsBackground = true;
            t.Start();


        }
        private void Listen()
        {

            try
            {
                _listener.Start();
                HttpListenerContext context2 = _listener.GetContext();
                while (!_token.IsCancellationRequested)
                {
                    if (_requestQueue.addContext(_listener.GetContext()))
                    {
                        Logger.Log("Stigao zahtev", Logger.Metode.Info, "Server");
                    }
                    else
                    {
                        SendError(context2, 503, "Server zauzet, pokusajte ponovo.");
                        Logger.Log("Vec postoji u add context", Logger.Metode.Warning, "Server");
                    }

                }

            }
            catch (HttpListenerException e)
            {
                Logger.Log(e.Message, Logger.Metode.Error, "Server");
                // SendError(context2, 501, "crko sam");
            }
            finally
            {

                _listener.Stop();
                _listener.Close();
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