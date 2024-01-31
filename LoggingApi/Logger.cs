using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;

namespace LoggingApi
{
    /// <summary>
    /// Provides simplifed methods for creating debuging logs.
    /// </summary>
    public class Logger
    {
        private static readonly Harmony s_harmony = new("qtheconqueror.loggingapi");
        private static readonly Dictionary<MethodBase, ManualLogSource> s_logSources = [];

        // Runner vars
        private static readonly string s_runner = Plugin.ConfigEnableRunner.Value ? Plugin.ConfigRunner.Value : "";
        private static readonly int s_runnerIndent = Plugin.ConfigEnableRunner.Value ? Plugin.ConfigRunnerIndent.Value : 0;

        // Indent vars
        private static readonly int s_indentIncrement = Plugin.ConfigIndentCallTrace.Value ? Plugin.ConfigIndentIncrement.Value : 0;
        private static int s_lastIndent = Plugin.ConfigBaseIndent.Value;
        private static int s_indent = Plugin.ConfigBaseIndent.Value;
        private static int Indent
        {
            get { return s_indent; }
            set
            {
                s_lastIndent = s_indent;
                s_indent = value;
            }
        }

        // Method Tracking vars
        private static int s_repeatCount = 0;
        private static MethodBase s_lastEnteredMethod = null;
        private static MethodBase s_lastExitedMethod = null;
        private static bool s_loggedLastEnteredMethod = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="logSource"></param>
        /// <param name="logLevel"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Logger(ManualLogSource logSource, LogLevel logLevel = LogLevel.All)
        {
            if (logSource == null) { throw new ArgumentNullException("logSource"); }
            if (logLevel < 0 || 127 < (int)logLevel) { throw new ArgumentOutOfRangeException("logLevel"); }

            _logSource = logSource;
            _logLevel = logLevel;
        }

        // Public data members
        private ManualLogSource _logSource;
        /// <summary>
        /// <see cref="ManualLogSource"/> used for logging.
        /// </summary>
        public ManualLogSource LogSource
        {
            get { return _logSource; }
            set { _logSource = value ?? throw new ArgumentNullException("LogSource"); }
        }

        private LogLevel _logLevel;
        /// <summary>
        /// The level of logs to display.
        /// </summary>
        public LogLevel LogLevel
        {
            get { return _logLevel; }
            set
            {
                if (value < 0 || 1023 < (int)value) { throw new ArgumentOutOfRangeException("LogLevel"); }
                _logLevel = value;
            }
        }

        /// <summary>
        /// Create a log of the specified level.
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="data"></param>
        /// <param name="includeInCallTrace">Whether to include the log in trace call formatting.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Log(LogLevel logLevel, object data, bool includeInCallTrace = true)
        {
            if (logLevel < 0 || 127 < (int)logLevel) { throw new ArgumentOutOfRangeException("logLevel"); }

            if (!Plugin.ConfigCreateLogs.Value) { return; }
            if (!Convert.ToBoolean(LogLevel & logLevel)) { return; }

            var callingMethod = new StackFrame(2).GetMethod();

            if (callingMethod.Name.StartsWith("DMD<"))
            {
                if (callingMethod.Name[4..^1].EndsWith("::.ctor"))
                {
                    callingMethod = callingMethod.DeclaringType.GetConstructor(callingMethod.GetParameters().Skip(1).Select(x => x.ParameterType).ToArray());
                }
                else
                {
                    callingMethod = callingMethod.DeclaringType.GetMethod(callingMethod.Name[4..^1].Split(':')[^1], callingMethod.GetParameters().Select(x => x.ParameterType).ToArray());
                }
            }

            if (Plugin.ConfigCombineChildlessCalls.Value)
            {
                if (Plugin.ConfigCombineRepeatCalls.Value)
                {
                    HandleRepeatCalls(callingMethod);
                }

                if (!s_loggedLastEnteredMethod && s_lastEnteredMethod == callingMethod)
                {
                    s_logSources[s_lastEnteredMethod].LogDebug(FormatCallLog("enter", s_lastEnteredMethod, s_lastIndent));
                    s_loggedLastEnteredMethod = true;
                }
            }

            if (includeInCallTrace)
            {
                LogSource.Log(logLevel, FormatManualLog(data.ToString(), callingMethod));
            }
            else
            {
                LogSource.Log(logLevel, data);
            }
        }

        private string FormatManualLog(string message, MethodBase caller)
        {
            var indent = (!Plugin.ConfigIndentManualLogs.Value) ? Plugin.ConfigBaseIndent.Value : s_indent;

            if (Convert.ToBoolean(LogLevel & LogLevel.Debug) && Plugin.ConfigShowSourceOfManualLogsInCallTrace.Value)
            {
                message = $"{Plugin.ConfigManualLogSource.Value} {message}";
            }

            if (!Convert.ToBoolean(LogLevel & LogLevel.Debug) && Plugin.ConfigShowSourceOfManualLogs.Value)
            {
                message = $"{Plugin.ConfigManualLogSource.Value} {message}";
            }

            if (Convert.ToBoolean(LogLevel & LogLevel.Debug))
            {
                message = message.Replace("{", "{{");
                message = message.Replace("}", "}}");
                message = string.Format($"{{0, {s_runnerIndent}}}{s_runner}{{0, {indent}}}{message}", "");
            }

            return ReplacePlaceholders(message, caller);
        }

        /// <summary>
        /// Create a log of Fatal level.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="includeInCallTrace">Whether to include the log in trace call formatting.</param>
        public void LogFatal(object data, bool includeInCallTrace = true)
        {
            Log(LogLevel.Fatal, data, includeInCallTrace);
        }

        /// <summary>
        /// Create a log of Error level.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="includeInCallTrace">Whether to include the log in trace call formatting.</param>
        public void LogError(object data, bool includeInCallTrace = true)
        {
            Log(LogLevel.Error, data, includeInCallTrace);
        }

        /// <summary>
        /// Create a log of Warning level.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="includeInCallTrace">Whether to include the log in trace call formatting.</param>
        public void LogWarning(object data, bool includeInCallTrace = true)
        {
            Log(LogLevel.Warning, data, includeInCallTrace);
        }

        /// <summary>
        /// Create a log of Message level.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="includeInCallTrace">Whether to include the log in trace call formatting.</param>
        public void LogMessage(object data, bool includeInCallTrace = true)
        {
            Log(LogLevel.Message, data, includeInCallTrace);
        }

        /// <summary>
        /// Create a log of Info level.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="includeInCallTrace">Whether to include the log in trace call formatting.</param>
        public void LogInfo(object data, bool includeInCallTrace = true)
        {
            Log(LogLevel.Info, data, includeInCallTrace);
        }

        /// <summary>
        /// Create a log of Debug level.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="includeInCallTrace">Whether to include the log in trace call formatting.</param>
        public void LogDebug(object data, bool includeInCallTrace = true)
        {
            Log(LogLevel.Debug, data, includeInCallTrace);
        }

        /// <summary>
        /// Log all calls to memebers of the given type.
        /// </summary>
        /// <param name="type"></param>
        public void LogAllCalls(Type type)
        {
            LogCalls(type, SearchFlags.All);
        }

        /// <summary>
        /// Log calls to memebers of the given type that meet the criteria.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="flags"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void LogCalls(Type type, SearchFlags flags = SearchFlags.Default)
        {
            if (type == null) { throw new ArgumentNullException("type"); }
            if (flags < 0 || 1023 < (int)flags) { throw new ArgumentOutOfRangeException("flags"); }

            if (!Plugin.ConfigCreateLogs.Value) { return; }
            if (!Plugin.ConfigCreateCallTrace.Value) { return; }
            if (!Convert.ToBoolean(LogLevel & LogLevel.Debug)) { return; }

            if (flags == SearchFlags.Default)
            {
                flags = (SearchFlags.Public | SearchFlags.Declared);
            }

            var instanceFlags = (SearchFlags.Instance | SearchFlags.Static);
            var accessFlags = (SearchFlags.Public | SearchFlags.NonPublic);
            var inheritanceFlags = (SearchFlags.Inherited | SearchFlags.Declared);
            var typeFlag = (SearchFlags.Constructor | SearchFlags.Method | SearchFlags.Operator | SearchFlags.Property);

            instanceFlags = (instanceFlags & flags) == 0 ? instanceFlags : (instanceFlags & flags);
            accessFlags = (accessFlags & flags) == 0 ? accessFlags : (accessFlags & flags);
            inheritanceFlags = (inheritanceFlags & flags) == 0 ? inheritanceFlags : (inheritanceFlags & flags);
            typeFlag = (typeFlag & flags) == 0 ? typeFlag : (typeFlag & flags);

            var methodFlags = (instanceFlags | accessFlags);
            var constructorFlags = ((instanceFlags & SearchFlags.Instance) | accessFlags);

            if (inheritanceFlags == SearchFlags.Declared)
            {
                methodFlags |= SearchFlags.Declared;
                constructorFlags |= SearchFlags.Declared;
            }

            HashSet<MethodBase> methods = [.. ((MethodBase[])type.GetMethods((BindingFlags)methodFlags))];
            HashSet<MethodBase> constructors = [.. ((MethodBase[])type.GetConstructors((BindingFlags)constructorFlags))];

            if (inheritanceFlags == SearchFlags.Inherited)
            {
                foreach (var declaredMethod in type.GetMethods((BindingFlags)(methodFlags | SearchFlags.Declared)))
                {
                    methods.Remove(declaredMethod);
                }

                foreach (var declaredConstructor in type.GetConstructors((BindingFlags)(constructorFlags | SearchFlags.Declared)))
                {
                    constructors.Remove(declaredConstructor);
                }
            }

            if (inheritanceFlags != SearchFlags.Declared)
            {
                // Remove unimplemented methods inherited from System.Object
                foreach (var baseMethod in typeof(object).GetMethods((BindingFlags)methodFlags))
                {
                    var method = type.GetMethod(baseMethod.Name, (BindingFlags)methodFlags, null, baseMethod.GetParameters().Select(p => p.ParameterType).ToArray(), null);
                    if (method != null && method.GetMethodBody() == baseMethod.GetMethodBody())
                    {
                        methods.Remove(method);
                    }
                }
            }

            foreach (var method in methods)
            {
                if ((method.Name.StartsWith("get_") || method.Name.StartsWith("set_")) && !Convert.ToBoolean(typeFlag & SearchFlags.Property))
                {
                    methods.Remove(method);
                }
                else if (method.Name.StartsWith("op_") && !Convert.ToBoolean(typeFlag & SearchFlags.Property))
                {
                    methods.Remove(method);
                }
                else if (!Convert.ToBoolean(typeFlag & SearchFlags.Property))
                {
                    methods.Remove(method);
                }
            }

            if (Convert.ToBoolean(typeFlag & SearchFlags.Constructor))
            {
                methods = [.. methods, .. constructors];
            }

            foreach (var method in methods)
            {
                LogCalls(method);
            }
        }

        /// <summary>
        /// Log all calls to the given method.
        /// </summary>
        /// <param name="method"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void LogCalls(MethodBase method)
        {
            if (method == null) { throw new ArgumentNullException("method"); }

            if (!Plugin.ConfigCreateLogs.Value) { return; }
            if (!Plugin.ConfigCreateCallTrace.Value) { return; }
            if (!Convert.ToBoolean(LogLevel & LogLevel.Debug)) { return; }

            try
            {
                string methodType;

                if (method.Name.StartsWith(".ctor"))
                {
                    methodType = "Constructor";
                }
                else if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                {
                    methodType = "Property";
                }
                else if (method.Name.StartsWith("op_"))
                {
                    methodType = "Operator";
                }
                else
                {
                    methodType = "Method";
                }

                Plugin.Logger.LogDebug($"Logging {methodType}:  {method.FullDescription()}", false);

                MethodInfo prefix = null;
                MethodInfo postfix = null;
                MethodInfo finalizer = null;

                prefix = typeof(Logger).GetMethod($"Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                postfix = typeof(Logger).GetMethod($"Postfix", BindingFlags.Static | BindingFlags.NonPublic);
                finalizer = typeof(Logger).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.NonPublic);

                s_logSources.Add(method, LogSource);
                s_harmony.Patch(method, prefix != null ? new HarmonyMethod(prefix) : null, postfix != null ? new HarmonyMethod(postfix) : null, null, new HarmonyMethod(finalizer), null);
            }
            catch (Exception ex) { Plugin.Logger.LogError($"Failed Logging: {method.Name} <!> {ex}", false); }
        }

        private static void Prefix(MethodBase __originalMethod)
        {
            if (Plugin.ConfigCombineChildlessCalls.Value)
            {
                if (Plugin.ConfigCombineRepeatCalls.Value)
                {
                    HandleRepeatCalls(__originalMethod);
                }

                if (s_lastExitedMethod == __originalMethod && s_lastExitedMethod == s_lastEnteredMethod)
                {
                    s_repeatCount++;
                }

                if (!s_loggedLastEnteredMethod && s_indent > s_lastIndent && s_lastEnteredMethod != __originalMethod)
                {
                    s_logSources[s_lastEnteredMethod].LogDebug(FormatCallLog("enter", s_lastEnteredMethod, s_lastIndent));
                }

                s_lastEnteredMethod = __originalMethod;
            }
            else
            {
                s_logSources[__originalMethod].LogDebug(FormatCallLog("enter", __originalMethod, s_indent));
            }

            Indent += s_indentIncrement;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            if (Plugin.ConfigCombineChildlessCalls.Value)
            {
                if (Plugin.ConfigCombineRepeatCalls.Value)
                {
                    HandleRepeatCalls(__originalMethod);
                }

                if (s_lastEnteredMethod != __originalMethod)
                {
                    s_logSources[__originalMethod].LogDebug(FormatCallLog("exit", __originalMethod, s_indent - s_indentIncrement));
                    s_loggedLastEnteredMethod = false;
                }

                if (s_lastEnteredMethod == __originalMethod && !Plugin.ConfigCombineRepeatCalls.Value)
                {
                    s_logSources[__originalMethod].LogDebug(FormatCallLog("combined", __originalMethod, s_indent - s_indentIncrement));
                }

                s_lastExitedMethod = __originalMethod;
            }
            else
            {
                s_logSources[__originalMethod].LogDebug(FormatCallLog("exit", __originalMethod, s_indent - s_indentIncrement));
            }
        }

        private static void Finalizer(Exception __exception, MethodBase __originalMethod)
        {
            Indent -= s_indentIncrement;

            if (__exception != null)
            {
                if (Plugin.ConfigCombineChildlessCalls.Value)
                {
                    if (Plugin.ConfigCombineRepeatCalls.Value)
                    {
                        HandleRepeatCalls(__originalMethod);
                    }

                    if (!s_loggedLastEnteredMethod && s_lastEnteredMethod == __originalMethod)
                    {
                        s_logSources[__originalMethod].LogDebug(FormatCallLog("enter", __originalMethod, s_indent));
                    }

                    s_loggedLastEnteredMethod = false;
                    s_repeatCount = 0;
                    s_lastExitedMethod = __originalMethod;
                }

                s_logSources[__originalMethod].LogDebug(FormatCallLog("exception", __originalMethod, s_indent, __exception));
            }
        }

        private static void HandleRepeatCalls(MethodBase originalMethod, [CallerMemberName] string callerName = "")
        {
            if (s_lastExitedMethod != null && s_lastExitedMethod != originalMethod)
            {
                int indent = callerName == "Finalizer" ? s_lastIndent : s_indent;
                int count = callerName == "Finalizer" ? s_repeatCount : s_repeatCount + 1;
                if (s_repeatCount > 0)
                {
                    s_logSources[s_lastExitedMethod].LogDebug(FormatCallLog("combined", s_lastExitedMethod, indent, null, count));
                    s_repeatCount = 0;
                    if (callerName == "Log")
                    {
                        s_lastExitedMethod = originalMethod;
                    }
                }
                else if (s_lastExitedMethod == s_lastEnteredMethod)
                {
                    s_logSources[s_lastExitedMethod].LogDebug(FormatCallLog("combined", s_lastExitedMethod, indent));
                }
            }
        }

        private static string FormatCallLog(string callType, MethodBase caller, int indent, Exception exception = null, int repeatCount = 0)
        {
            if (callType == "exception" && exception == null) { throw new ArgumentNullException("exception"); }

            indent = (callType == "exception" && !Plugin.ConfigIndentExceptions.Value) ? Plugin.ConfigBaseIndent.Value : indent;

            var source = (callType == "exception") ? Plugin.ConfigExceptionSource.Value : Plugin.ConfigCallSource.Value;
            var info = (callType == "exception") ? Plugin.ConfigExceptionInfo.Value : Plugin.ConfigCallInfo.Value;

            var marker = "";
            var separator = "";
            switch (callType)
            {
                case "enter":
                    marker = Plugin.ConfigEnterMarker.Value;
                    separator = Plugin.ConfigCallSeparator.Value;
                    break;
                case "exit":
                    marker = Plugin.ConfigExitMarker.Value;
                    separator = Plugin.ConfigCallSeparator.Value;
                    break;
                case "combined":
                    marker = Plugin.ConfigCombinedMarker.Value;
                    separator = Plugin.ConfigCallSeparator.Value;
                    break;
                case "exception":
                    marker = Plugin.ConfigExceptionMarker.Value;
                    separator = Plugin.ConfigExceptionSeparator.Value;
                    break;
            }
            marker = (Plugin.ConfigEnableMarkers.Value && marker != "") ? $"{marker} " : "";
            separator = (separator != "") ? $" {separator} " : " ";
            var repeatIndicator = (repeatCount > 0) ? $" ({repeatCount}) " : "";

            var message = $"{marker}{repeatIndicator}{source}{separator}{info}";
            message = message.Replace("{", "{{");
            message = message.Replace("}", "}}");
            message = string.Format($"{{0, {s_runnerIndent}}}{s_runner}{{0, {indent}}}{message}", "");

            return ReplacePlaceholders(message, caller, exception);
        }

        private static string ReplacePlaceholders(string text, MethodBase caller, Exception exception = null)
        {
            Dictionary<string, string> placeholders = [];
            placeholders.Add("{CallerName}", caller.Name);
            placeholders.Add("{CallerFullDescription}", caller.FullDescription());
            placeholders.Add("{CallerReflectedType}", caller.ReflectedType.FullName);
            placeholders.Add("{CallerReflectedTypeName}", caller.ReflectedType.Name);
            placeholders.Add("{CallerDeclaringType}", caller.DeclaringType.FullName);
            placeholders.Add("{CallerDeclaringTypeName}", caller.DeclaringType.Name);
            if (exception != null)
            {
                placeholders.Add("{ExceptionType}", exception.GetType().FullName);
                placeholders.Add("{ExceptionTypeName}", exception.GetType().Name);
                placeholders.Add("{ExceptionMessage}", exception.Message);
            }

            foreach (var placeholder in placeholders)
            {
                text = text.Replace(placeholder.Key, placeholder.Value);
            }

            return text;
        }
    }
}
