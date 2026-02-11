using System;

namespace ExchangeAPIController
{
    /// <summary>
    /// API 요청/응답 로그 출력용. Form1 등 UI에서 Subscribe하여 로그창에 표시.
    /// </summary>
    public static class ApiRequestLogger
    {
        public static Action<string> OnLog { get; set; }

        public static void Log(string message)
        {
            OnLog?.Invoke(message ?? "");
        }

        public static void LogRequest(string method, string url, string paramsForLog)
        {
            Log($"[API 요청] {method} {url}");
            if (!string.IsNullOrEmpty(paramsForLog))
                Log($"  파라미터: {paramsForLog}");
        }

        public static void LogResponse(int statusCode, string body, int maxBodyLength = 2000)
        {
            var bodyPreview = string.IsNullOrEmpty(body) ? "(empty)"
                : body.Length > maxBodyLength ? body.Substring(0, maxBodyLength) + $"... (truncated, total {body.Length} chars)"
                : body;
            Log($"[API 응답] HTTP {statusCode}");
            Log($"  본문: {bodyPreview}");
        }

        /// <summary>
        /// 전송하는 Request Body 전체를 로그에 표시 (디버깅/검증용)
        /// </summary>
        public static void LogRequestBody(string body, int maxLength = 0)
        {
            if (string.IsNullOrEmpty(body))
            {
                Log("  [전송 Body] (empty)");
                return;
            }
            var toLog = (maxLength > 0 && body.Length > maxLength)
                ? body.Substring(0, maxLength) + $"... (truncated, total {body.Length} chars)"
                : body;
            Log($"  [전송 Body] {toLog}");
        }
    }
}
