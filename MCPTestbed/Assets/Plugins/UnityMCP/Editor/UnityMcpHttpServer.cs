using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UnityMCP
{
    [InitializeOnLoad]
    public static class UnityMcpHttpServer
    {
        private static HttpListener _listener;
        private static Thread _listenerThread;
        private static bool _isRunning;
        private const int Port = 6200;
        private const string Prefix = "http://localhost:6200/";

        static UnityMcpHttpServer()
        {
            StartServer();
            EditorApplication.quitting += StopServer;
        }

        private static void StartServer()
        {
            if (_isRunning) return;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(Prefix);
                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "UnityMCP-HttpServer"
                };
                _listenerThread.Start();

                Debug.Log($"[UnityMCP] HTTP Server started on port {Port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityMCP] Failed to start HTTP server: {e.Message}");
            }
        }

        private static void StopServer()
        {
            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
            _listenerThread?.Join(1000);
            Debug.Log("[UnityMCP] HTTP Server stopped");
        }

        private static void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Server stopped
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UnityMCP] Error in listen loop: {e.Message}");
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                string responseString;
                int statusCode = 200;

                if (request.HttpMethod == "POST")
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string body = reader.ReadToEnd();
                        responseString = ProcessCommand(body, out statusCode);
                    }
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/ping")
                {
                    responseString = "{\"status\":\"ok\",\"server\":\"UnityMCP\"}";
                }
                else
                {
                    statusCode = 404;
                    responseString = "{\"error\":\"Not found\"}";
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                byte[] buffer = Encoding.UTF8.GetBytes($"{{\"error\":\"{e.Message}\"}}");
                response.StatusCode = 500;
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        private static string ProcessCommand(string jsonBody, out int statusCode)
        {
            statusCode = 200;
            
            try
            {
                var request = JsonUtility.FromJson<McpRequest>(jsonBody);
                
                // Execute on main thread
                string result = null;
                Exception exception = null;
                var waitHandle = new ManualResetEvent(false);

                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        result = UGUICommands.Execute(request.command, request.args);
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                    finally
                    {
                        waitHandle.Set();
                    }
                };

                // Wait for main thread execution (timeout 30s)
                if (!waitHandle.WaitOne(30000))
                {
                    statusCode = 500;
                    return "{\"error\":\"Command timeout\"}";
                }

                if (exception != null)
                {
                    statusCode = 500;
                    return $"{{\"error\":\"{EscapeJson(exception.Message)}\"}}";
                }

                return result ?? "{\"success\":true}";
            }
            catch (Exception e)
            {
                statusCode = 400;
                return $"{{\"error\":\"Invalid request: {EscapeJson(e.Message)}\"}}";
            }
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        [Serializable]
        public class McpRequest
        {
            public string command;
            public string args;
        }
    }
}
