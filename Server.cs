using System;
using System.Threading;
using System.Net;
namespace PalindromeServer
{
    public class Server
    {
        public readonly HttpListener _listener;
        public readonly RequestQueue _requestQueue;
        public readonly CancellationToken _token;
        public Server(HttpListener listener, RequestQueue queue, CancellationToken token)
        {
            _listener = listener;
            _requestQueue = queue;
            _token = token;
        }
    }

}