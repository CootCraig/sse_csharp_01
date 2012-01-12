using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using System.Collections;
using Jayrock.Json;
using Jayrock.Json.Conversion;

namespace sse_csharp_01
{
    class Program
    {
        // http://holyhoehle.wordpress.com/2010/01/15/making-an-asynchronous-webrequest/
        // HttpWebRequest Class - Provides an HTTP-specific implementation of the WebRequest class.
        // WebRequest.Create Method (String) - Initializes a new WebRequest instance for the specified URI scheme.
        // HttpWebRequest.BeginGetResponse Method Begins an asynchronous request to an Internet resource.
        // http://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.begingetresponse%28v=VS.80%29.aspx
        // WebRequest.Method Property - http://msdn.microsoft.com/en-US/library/system.net.webrequest.method%28v=VS.80%29.aspx - example
        // http://jayrock.berlios.de/ - Jayrock is a modest and an open source (LGPL) implementation of JSON and JSON-RPC for the Microsoft .NET Framework
        private static void ResponseCallback(IAsyncResult result)
        {
            try
            {
                // Get and fill the RequestState
                RequestState state = (RequestState)result.AsyncState;
                HttpWebRequest request = state.Request;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result);
                state.Response = response;
                Stream aStream = response.GetResponseStream();
                state.ResponseStream = aStream;
                aStream.BeginRead(state.BufferRead, 0, state.BufferSize,new AsyncCallback(ReadCallback), state);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private static void ReadCallback(IAsyncResult result)
        {
            try
            {
                // Get RequestState
                RequestState state = (RequestState)result.AsyncState;
                // determine how many bytes have been read
                int bytesRead = state.ResponseStream.EndRead(result);

                if (bytesRead > 0) // stream has not reached the end yet
                {
                    Console.WriteLine("ReadCallback {0} bytesRead", bytesRead);
                    string msg = Encoding.ASCII.GetString(state.BufferRead, 0, bytesRead);
                    string json_string = msg.Substring(5);
                    json_string = json_string.Trim();
                    JsonObject obj = (JsonObject)JsonConvert.Import(json_string);
                    Console.WriteLine("state: {3}, id: {0}, source: {1}, time: {2}", obj["id"],obj["source"],obj["time"],obj["state"]);
                    state.ResponseStream.BeginRead(state.BufferRead, 0, state.BufferSize, new AsyncCallback(ReadCallback), state);
                }
                else // end of the stream reached
                {
                    Console.WriteLine("ReadCallback no bytes read");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadCallback exception {0}",ex.ToString());
            }
        }


        static void Main(string[] args)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost:4000/actor/me");
                request.Method = "GET";
                request.Proxy = null;
                RequestState myRequestState = new RequestState();
                myRequestState.Request = request;
                IAsyncResult result = request.BeginGetResponse(new AsyncCallback(ResponseCallback), myRequestState);
                AutoResetEvent aEvent = new AutoResetEvent(false);
                aEvent.WaitOne();
                Console.WriteLine("WaitOne returned");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public class RequestState
    {
        public int BufferSize { get; private set; }
        public byte[] BufferRead { get; set; }
        public HttpWebRequest Request { get; set; }
        public HttpWebResponse Response { get; set; }
        public Stream ResponseStream { get; set; }

        public RequestState()
        {
            BufferSize = 8 * 1024;
            BufferRead = new byte[BufferSize];
            Request = null;
            ResponseStream = null;
        }
    }

}
