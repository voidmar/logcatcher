using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace LogCatcher
{
    /// <summary>
    /// LogCatcher implements the Debug.Log* interface by calling a set of delegates instead
    /// </summary>
    public static class LogCatcher
    {
        /// <summary>
        /// Per-thread setting to prevent LogCatcher from appending stack traces
        /// </summary>
        public static bool HideThreadStackTrace
        {
            get
            {
                return thread_hide_trace;
            }
            set
            {
                thread_hide_trace = value;
            }
        }

        /// <summary>
        /// Global setting to prevent LogCatcher from appending stack traces
        /// </summary>
        public static bool HideStackTrace
        {
            get;
            set;
        }

        public static Action<object, UnityEngine.Object> on_log;
        public static Action<object, UnityEngine.Object> on_log_warning = UnityEngine.Debug.LogWarning;
        public static Action<object, UnityEngine.Object> on_log_error = UnityEngine.Debug.LogError;
        public static Action<Exception, UnityEngine.Object> on_log_exception = UnityEngine.Debug.LogException;

        public static void Log(object message)
        {
            DoLog(message, null, on_log);
        }

        public static void Log(object message, UnityEngine.Object context)
        {
            DoLog(message, context, on_log);
        }

        public static void LogWarning(object message)
        {
            DoLog(message, null, on_log_warning);
        }

        public static void LogWarning(object message, UnityEngine.Object context)
        {
            DoLog(message, context, on_log_warning);
        }

        public static void LogError(object message)
        {
            DoLog(message, null, on_log_error);
        }

        public static void LogError(object message, UnityEngine.Object context)
        {
            DoLog(message, context, on_log_error);
        }

        public static void LogException(Exception exception)
        {
            if (on_log_exception != null)
                on_log_exception(exception, null);
        }

        public static void LogException(Exception exception, UnityEngine.Object context)
        {
            if (on_log_exception != null)
                on_log_exception(exception, context);
        }

        [ThreadStatic]
        static bool thread_hide_trace;

        static void DoLog(object message, UnityEngine.Object context, Action<object, UnityEngine.Object> callback)
        {
            if (callback == null)
                return;

            if (thread_hide_trace || HideStackTrace)
            {
                callback(message, context);
                return;
            }

            var trace = new StackTrace(2, true);
            var trace_message = "\n" + trace;

            if (message == null)
            {
                message = "(null)" + trace_message.ToString();
            }
            else
            {
                message = message.ToString() + trace_message.ToString();
            }

            callback(message, context);
        }
    }
}
