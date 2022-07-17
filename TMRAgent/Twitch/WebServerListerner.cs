using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TMRAgent.Twitch
{
    internal class WebServerListerner : IDisposable
    {
        private HttpListener listener;

        public WebServerListerner(string uri)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(uri);
        }

        public async Task<Models.Authorisation> Listen()
        {
            listener.Start();
            return await onRequest();
        }

        private async Task<Models.Authorisation> onRequest()
        {
            while (listener.IsListening)
            {
                var ctx = await listener.GetContextAsync();
                var req = ctx.Request;
                var resp = ctx.Response;

                using (var writer = new StreamWriter(resp.OutputStream))
                {
                    if (req.QueryString.AllKeys.Any("code".Contains))
                    {
                        writer.WriteLine("Authorization started! Check your application!");
                        writer.Flush();
                        return new Models.Authorisation(req.QueryString["code"]);
                    }
                    else
                    {
                        writer.WriteLine("No code found in query string!");
                        writer.Flush();
                    }
                }
            }
            return null;
        }

        public void Dispose()
        {
            ((IDisposable) listener)?.Dispose();
        }
    }
}
