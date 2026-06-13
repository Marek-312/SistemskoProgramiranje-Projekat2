using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
namespace PalindromeServer
{
    public class RequestQueue
    {

        private readonly BlockingCollection<HttpListenerContext> blockingCollection;

        private readonly CancellationToken _token;
        public RequestQueue(CancellationToken token, int maxSize = 128)
        {
            blockingCollection = new BlockingCollection<HttpListenerContext>(maxSize);


            _token = token;
        }
        public bool addContext(HttpListenerContext context)
        {
            return blockingCollection.TryAdd(context);

        }
        public HttpListenerContext removeContext()
        {
            return blockingCollection.Take(_token);
        }


    }
}