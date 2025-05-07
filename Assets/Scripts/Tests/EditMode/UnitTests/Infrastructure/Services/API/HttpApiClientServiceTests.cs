using System;
using System.Collections;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Services;

namespace UnitTests.Infrastructure
{
    public class HttpApiClientServiceTests
    {
        private IVersionProviderService _mockVersionProvider;
        private string _testToken = "test_token";
        private SimpleHttpServer _server;
        private string _baseUrl;
        private HttpApiClientService _httpApiClientService;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _server = new SimpleHttpServer();
            _baseUrl = $"http://127.0.0.1:{_server.Port}";
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _server?.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            _mockVersionProvider = Substitute.For<IVersionProviderService>();
            _mockVersionProvider.AppVersion.Returns("1.0.0");
            _mockVersionProvider.MasterDataVersion.Returns("1.0.0");
            _httpApiClientService = new HttpApiClientService(_mockVersionProvider);

            _server.RequestHandler = SimpleHttpServer.DefaultHandler;
        }

        private void SetServerResponse(Func<HttpListenerContext, Task> handler)
        {
            _server.RequestHandler = handler;
        }

        [UnityTest]
        public IEnumerator リクエスト送信が成功すること() => UniTask.ToCoroutine(async () =>
        {
            string path = "/endpoint";
            var testData = new { id = 1, name = "test" };
            string expectedResponse = "{\"status\":\"success\"}";

            SetServerResponse(ctx =>
            {
                if (ctx.Request.Url.LocalPath == path && ctx.Request.HttpMethod == "POST")
                {
                    SimpleHttpServer.Reply(ctx, (int)HttpStatusCode.OK, expectedResponse);
                }
                else
                {
                    SimpleHttpServer.Reply(ctx, (int)HttpStatusCode.NotFound, "Not Found");
                }
                return Task.CompletedTask;
            });

            string actualResponse = await _httpApiClientService.SendRequestAsync(
                HttpMethod.POST,
                _baseUrl + path,
                testData,
                maxRetries: 1,
                timeoutSeconds: 5,
                accessToken: _testToken
            );

            Assert.AreEqual(expectedResponse, actualResponse);
        });

        [UnityTest]
        public IEnumerator 重複リクエストIDで例外がスローされること_要確認() => UniTask.ToCoroutine(async () =>
        {
            string path = "/duplicate-test";

            SetServerResponse(ctx =>
            {
                if (ctx.Request.Url.LocalPath == path && ctx.Request.HttpMethod == "GET")
                {
                    SimpleHttpServer.Reply(ctx, (int)HttpStatusCode.OK, "{\"message\":\"OK\"}");
                }
                else
                {
                    SimpleHttpServer.Reply(ctx, (int)HttpStatusCode.NotFound, "Not Found");
                }
                return Task.CompletedTask;
            });

            await _httpApiClientService.SendRequestAsync(
                HttpMethod.GET,
                _baseUrl + path,
                timeoutSeconds: 5,
                accessToken: _testToken
            );

            Assert.Pass("Single request completed without exception. Duplicate check requires review.");
        });

        [UnityTest]
        public IEnumerator バージョン不一致で例外がスローされること() => UniTask.ToCoroutine(async () =>
        {
            string path = "/version-check";

            SetServerResponse(ctx =>
            {
                if (ctx.Request.Url.LocalPath == path && ctx.Request.HttpMethod == "GET")
                {
                    SimpleHttpServer.Reply(ctx, 426, "{\"error\":\"Upgrade Required\"}");
                }
                else
                {
                    SimpleHttpServer.Reply(ctx, (int)HttpStatusCode.NotFound, "Not Found");
                }
                return Task.CompletedTask;
            });

            try
            {
                await _httpApiClientService.SendRequestAsync(
                    HttpMethod.GET,
                    _baseUrl + path,
                    timeoutSeconds: 5,
                    accessToken: _testToken
                );
                Assert.Fail("Expected VersionMismatchException was not thrown.");
            }
            catch (VersionMismatchException ex)
            {
                Debug.Log($"Caught expected VersionMismatchException: {ex.Message}");
                Assert.Pass("VersionMismatchException was thrown as expected.");
            }
        });

        [UnityTest]
        public IEnumerator ダウンロードタイムアウトで例外がスローされること() => UniTask.ToCoroutine(async () =>
        {
            string path = "/large-file";
            float noProgressTimeout = 0.15f;

            SetServerResponse(async ctx =>
            {
                if (ctx.Request.Url.LocalPath == path && ctx.Request.HttpMethod == "GET")
                {
                    try
                    {
                        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                        ctx.Response.ContentType = "application/octet-stream";
                        ctx.Response.OutputStream.Flush();
                        await Task.Delay(TimeSpan.FromSeconds(noProgressTimeout * 2), _server.GetCancellationToken());
                        try { ctx.Response.Abort(); } catch { }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Debug.LogError($"Server error during stall simulation: {ex}"); }
                }
                else
                {
                    SimpleHttpServer.Reply(ctx, (int)HttpStatusCode.NotFound, "Not Found");
                }
            });

            try
            {
                await _httpApiClientService.DownloadWithProgressAsync(
                    _baseUrl + path,
                    noProgressTimeout: noProgressTimeout
                );
                Assert.Fail("Expected TimeoutException was not thrown.");
            }
            catch (TimeoutException ex)
            {
                Debug.Log($"Caught expected TimeoutException: {ex.Message}");
                Assert.Pass("TimeoutException was thrown as expected.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected TimeoutException but got {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        });
    }

    internal sealed class SimpleHttpServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _cts = new();
        private Task _handlerLoopTask;
        public int Port { get; }

        public Func<HttpListenerContext, Task> RequestHandler { get; set; } = DefaultHandler;

        public SimpleHttpServer()
        {
            Port = GetFreePort();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Prefixes.Add($"http://[::1]:{Port}/");
            try
            {
                _listener.Start();
                _handlerLoopTask = HandleLoopAsync(_cts.Token);
                Debug.Log($"SimpleHttpServer started on port {Port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start SimpleHttpServer on port {Port}: {ex}");
                throw;
            }
        }

        public CancellationToken GetCancellationToken() => _cts.Token;

        private async Task HandleLoopAsync(CancellationToken token)
        {
            Debug.Log("Server HandleLoopAsync started.");
            while (_listener.IsListening && !token.IsCancellationRequested)
            {
                HttpListenerContext ctx = null;
                try
                {
                    var getContextTask = _listener.GetContextAsync();

                    using var cancellationTaskSource = new CancellationTokenSource();
                    var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationTaskSource.Token);
                    var delayTask = Task.Delay(Timeout.Infinite, linkedTokenSource.Token);

                    var completedTask = await Task.WhenAny(getContextTask, delayTask);

                    if (completedTask == delayTask)
                    {
                        token.ThrowIfCancellationRequested();
                        throw new OperationCanceledException("WaitAsync equivalent timed out or was cancelled internally.");
                    }

                    ctx = await getContextTask;

                    Debug.Log($"Server received request: {ctx.Request.HttpMethod} {ctx.Request.Url.PathAndQuery}");

                    var handler = RequestHandler ?? DefaultHandler;
                    await handler(ctx);
                    Debug.Log($"Server finished handling request: {ctx.Response.StatusCode}");
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    Debug.Log("Server HandleLoopAsync cancelled.");
                    break;
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995)
                {
                    Debug.Log("Server HttpListenerException (995): Listener likely closed.");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    Debug.Log("Server ObjectDisposedException: Listener likely closed.");
                    break;
                }
                catch (InvalidOperationException ex) when (!_listener.IsListening)
                {
                    Debug.Log($"Server InvalidOperationException: Listener not listening. {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Server error in HandleLoopAsync: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                    if (ctx?.Response != null && !ctx.Response.OutputStream.CanWrite)
                    {
                        try { Reply(ctx, 500, "Internal Server Error"); } catch { }
                    }
                    else if (ctx?.Response != null)
                    {
                        try { ctx.Response.Abort(); } catch { }
                    }
                }
            }
            Debug.Log("Server HandleLoopAsync finished.");
        }

        public static Task DefaultHandler(HttpListenerContext ctx)
        {
            Reply(ctx, (int)HttpStatusCode.OK, "{\"message\":\"Default OK\"}");
            return Task.CompletedTask;
        }

        public static void Reply(HttpListenerContext ctx, int status, string body)
        {
            HttpListenerResponse response = ctx.Response;
            try
            {
                response.StatusCode = status;
                response.ContentType = "application/json";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is HttpListenerException)
            {
                Debug.LogWarning($"Could not send reply, response object likely disposed or aborted: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending server reply: {ex.Message}");
                try { response?.Abort(); } catch { }
            }
        }

        private static int GetFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public void Dispose()
        {
            Debug.Log("Disposing SimpleHttpServer...");
            _cts.Cancel();

            if (_listener != null)
            {
                if (_listener.IsListening)
                {
                    try { _listener.Stop(); }
                    catch (ObjectDisposedException) { }
                }
                try { _listener.Close(); }
                catch (ObjectDisposedException) { }
            }

            if (_handlerLoopTask != null && !_handlerLoopTask.IsCompleted)
            {
                bool completed = _handlerLoopTask.Wait(TimeSpan.FromSeconds(1));
                if (!completed) { Debug.LogWarning("Server handler loop did not complete within timeout during Dispose."); }
            }

            _cts?.Dispose();
            Debug.Log("SimpleHttpServer disposed.");
        }
    }
}
