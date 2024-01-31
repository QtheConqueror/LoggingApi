using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace LoggingApi
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class Plugin : BaseUnityPlugin
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        // General Config
        internal static new Logger Logger;
        internal static ConfigEntry<bool> ConfigCreateLogs;
        internal static ConfigEntry<bool> ConfigCreateCallTrace;

        // Format.General
        internal static ConfigEntry<bool> ConfigEnableRunner;
        internal static ConfigEntry<bool> ConfigEnableMarkers;
        internal static ConfigEntry<bool> ConfigCombineChildlessCalls;
        internal static ConfigEntry<bool> ConfigCombineRepeatCalls;
        internal static ConfigEntry<bool> ConfigShowSourceOfManualLogs;
        internal static ConfigEntry<bool> ConfigShowSourceOfManualLogsInCallTrace;

        // Format.Indent
        internal static ConfigEntry<bool> ConfigIndentCallTrace;
        internal static ConfigEntry<bool> ConfigIndentExceptions;
        internal static ConfigEntry<bool> ConfigIndentManualLogs;
        internal static ConfigEntry<int> ConfigRunnerIndent;
        internal static ConfigEntry<int> ConfigBaseIndent;
        internal static ConfigEntry<int> ConfigIndentIncrement;

        // Format.Symbols
        internal static ConfigEntry<string> ConfigRunner;
        internal static ConfigEntry<string> ConfigEnterMarker;
        internal static ConfigEntry<string> ConfigExitMarker;
        internal static ConfigEntry<string> ConfigCombinedMarker;
        internal static ConfigEntry<string> ConfigExceptionMarker;
        internal static ConfigEntry<string> ConfigCallSeparator;
        internal static ConfigEntry<string> ConfigExceptionSeparator;

        // Format.Information
        internal static ConfigEntry<string> ConfigCallSource;
        internal static ConfigEntry<string> ConfigCallInfo;
        internal static ConfigEntry<string> ConfigExceptionSource;
        internal static ConfigEntry<string> ConfigExceptionInfo;
        internal static ConfigEntry<string> ConfigManualLogSource;

        // Logging Config
        internal static ConfigEntry<bool> ConfigLoggingEnabled;
        internal static ConfigEntry<LogLevel> ConfigLoggingLevel;

        private void Awake()
        {
            InitConfig();
            Logger = new Logger(base.Logger, ConfigLoggingLevel.Value);
            base.Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }

        private void InitConfig()
        {
            // General
            ConfigCreateLogs = Config.Bind("\u202e lareneG", "CreateLogs", true, "Enable the creation of logs for other mods. Each mod sets which level of logs to show.");
            ConfigCreateCallTrace = Config.Bind("\u202e lareneG", "CreateCallTrace", true, "Enable the creation of call trace logs. Call trace logs are debug level.");

            // Format.General
            ConfigEnableRunner = Config.Bind("Format\u202e lareneG.", "EnableRunner", true, "Enable the runner that indicates the bottom level of the call trace.");
            ConfigEnableMarkers = Config.Bind("Format\u202e lareneG.", "EnableMarkers", true, "Enable markers designating whether a call is an entrance, exit, or exception.");
            ConfigCombineChildlessCalls = Config.Bind("Format\u202e lareneG.", "CombineChildlessCalls", true, "Combine childless calls into one line.");
            ConfigCombineRepeatCalls = Config.Bind("Format\u202e lareneG.", "CombineRepeatCalls", true, "Combine repeat childless calls into one line with a count indicator.");
            ConfigShowSourceOfManualLogs = Config.Bind("Format\u202e lareneG.", "ShowSourceOfManualLogs", false, "For manually created log messages, show what call created the log.");
            ConfigShowSourceOfManualLogsInCallTrace = Config.Bind("Format\u202e lareneG.", "ShowSourceOfManualLogsInCallTrace", false, "For manually created log messages, show what call created the log in the call trace.");

            // Format.Indent
            ConfigIndentCallTrace = Config.Bind("Format.Indent", "IndentCallTrace", true, "Enable indenting call trace logs. Indentation follows the flow of calls.");
            ConfigIndentExceptions = Config.Bind("Format.Indent", "IndentExceptions", true, "Enable indenting exception logs. Indentation follows the flow of calls.");
            ConfigIndentManualLogs = Config.Bind("Format.Indent", "IndentManualLogs", true, "Enable indenting manually created logs. Indentation follows the flow of calls.");
            ConfigRunnerIndent = Config.Bind("Format.Indent", "RunnerIndent", 0, new ConfigDescription("Amount of spaces the runner is indented by.", new AcceptableValueRange<int>(0, 32)));
            ConfigBaseIndent = Config.Bind("Format.Indent", "BaseIndent", 1, new ConfigDescription("Amount of spaces the lowest part of the call trace is indented by.", new AcceptableValueRange<int>(0, 32)));
            ConfigIndentIncrement = Config.Bind("Format.Indent", "IndentIncrement", 2, new ConfigDescription("Amount of spaces each level of the call trace is indented by.", new AcceptableValueRange<int>(0, 32)));

            // Format.Symbols
            ConfigRunner = Config.Bind("Format.Symbols", "Runner", "|", "Symbol used as the runner.");
            ConfigEnterMarker = Config.Bind("Format.Symbols", "EnterMarker", "->", "Symbol or phrase indicating a call entrance.");
            ConfigExitMarker = Config.Bind("Format.Symbols", "ExitMarker", "<-", "Symbol or phrase indicating a call exit.");
            ConfigCombinedMarker = Config.Bind("Format.Symbols", "CombinedMarker", "<->", "Symbol or phrase indicating a call with no children.");
            ConfigExceptionMarker = Config.Bind("Format.Symbols", "ExceptionMarker", "!!", "Symbol or phrase indicating an exception was thown.");
            ConfigCallSeparator = Config.Bind("Format.Symbols", "CallSeparator", "<>", "Symbol or phrase separating the call source and info.");
            ConfigExceptionSeparator = Config.Bind("Format.Symbols", "ExceptionSeparator", "<!>", "Symbol or phrase separating the exception source and info.");

            // Format.Information
            string[] availableVaribles = ["{CallerName}", "{CallerFullDescription}", "{CallerReflectedType}", "{CallerReflectedTypeName}", "{CallerDeclaringType}", "{CallerDeclaringTypeName}"];
            string[] exceptionAvailableVaribles = availableVaribles.Concat(["{ExceptionType}", "{ExceptionTypeName}", "{ExceptionMessage}"]).ToArray();
            ConfigCallSource = Config.Bind("Format.Information", "CallSource", "{CallerName}()", $"Call name or identifier.\nAvailible variables: {string.Join(", ", availableVaribles)}");
            ConfigCallInfo = Config.Bind("Format.Information", "CallInfo", "{CallerReflectedType}", $"Information about a call.\nAvailible variables: {string.Join(", ", availableVaribles)}");
            ConfigExceptionSource = Config.Bind("Format.Information", "ExceptionSource", "{CallerName}()", $"Call the exception occured in.\nAvailible variables: {string.Join(", ", exceptionAvailableVaribles)}");
            ConfigExceptionInfo = Config.Bind("Format.Information", "ExceptionInfo", "{ExceptionType}: {ExceptionMessage}", $"Information about an exception.\nAvailible variables: {string.Join(", ", exceptionAvailableVaribles)}");
            ConfigManualLogSource = Config.Bind("Format.Information", "ManualLogSource", "[{CallerReflectedType}::{CallerName}]:", $"Call the log message occured in.\nAvailible variables: {string.Join(", ", availableVaribles)}");

            // Logging
            ConfigLoggingEnabled = Config.Bind("Logging", "Enabled", true, "Enable logging. (Does not control logs created for other mods)");
            ConfigLoggingLevel = Config.Bind("Logging", "LogLevels", LogLevel.Fatal | LogLevel.Error | LogLevel.Warning | LogLevel.Message | LogLevel.Info, "Which log levels to show. (Does not control logs created for other mods)");
        }
    }
}
