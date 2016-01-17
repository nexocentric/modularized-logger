using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Nexocentric.LoggingTools
{
	public enum LoggerDebugLevels
	{
		Emergency,
		Alert,
		Critical,
		Error,
		Warning,
		Notification,
		Informational,
		Debug,
		Test,
		Profile
	}

	public class ModularizedLogger
	{
		public bool DisplayConfigurationDebuggingInformation { set; get; }
		private string logConfigurationFilePath = "";
		private string loggerName = "";
		private FileInfo configurationFile = null;
		private DateTime lastSavedConfigurationFileWriteTime;
		private log4net.ILog log = null;

		public ModularizedLogger(string configurationFilePath = @".\Log.config", string loggerName = "Logger")
		{
			this.loggerName = loggerName;
			logConfigurationFilePath = Path.GetFullPath(configurationFilePath);
		}

		public void Write(
			object message,
			Exception exception = null,
			LoggerDebugLevels selectedLevel = LoggerDebugLevels.Debug,
			[CallerFilePath] string filePath = "",
			[CallerLineNumber] int lineNumber = 0,
			[CallerMemberName] string functionName = ""
		)
		{
			bool? configurationFileReloaded = ConfigurationFileChanged(logConfigurationFilePath);
			if (!configurationFileReloaded.HasValue)
			{
				WriteConfigurationDebuggingInformation(
					"The configuration file located at [{0}] could not be (re)loaded. Please check the file path.",
					logConfigurationFilePath
				);
				return;
			}

			if (configurationFileReloaded.Value)
			{
				WriteConfigurationDebuggingInformation(
					"The configuration file located at [{0}] was (re)loaded successfully.",
					logConfigurationFilePath
				);
			}
			else
			{
				WriteConfigurationDebuggingInformation(
					"The configuration file located at [{0}] had no changes.",
					logConfigurationFilePath
				);
			}

			SetPredefinedGlobalLogVariables(filePath, lineNumber, functionName, selectedLevel.ToString());
			SelectWriter(message, exception, selectedLevel);
		}

		public bool? ConfigurationFileChanged(string configurationFilePath)
		{
			if (!File.Exists(configurationFilePath))
			{
				return null;
			}

			configurationFile = new FileInfo(configurationFilePath);
			if (lastSavedConfigurationFileWriteTime == configurationFile.LastWriteTime)
			{
				return false;
			}

			return TryReloadConfigurationFile();
		}

		private bool TryReloadConfigurationFile()
		{
			try
			{
				log4net.Config.XmlConfigurator.Configure(configurationFile);
				log = log4net.LogManager.GetLogger(loggerName);
				lastSavedConfigurationFileWriteTime = configurationFile.LastWriteTime;
				logConfigurationFilePath = configurationFile.ToString();
				return true;
			}
			catch (Exception exception)
			{
				WriteConfigurationDebuggingInformation(
					"Reload of the configuration file located at [{0}] failed with the following error:\n{1}\n{2}.",
					configurationFile.ToString(),
					exception.Message,
					exception.StackTrace
				);
				return false;
			}
		}

		private void SetPredefinedGlobalLogVariables(string filePath, int lineNumber, string functionName, string severity)
		{
			log4net.GlobalContext.Properties["source_file_path"] = filePath;
			log4net.GlobalContext.Properties["source_line_number"] = lineNumber;
			log4net.GlobalContext.Properties["source_function_name"] = functionName;
			log4net.GlobalContext.Properties["severity"] = severity.ToUpper();
			log4net.GlobalContext.Properties["severity_name"] = severity.ToUpper();
			log4net.GlobalContext.Properties["log_severity_name"] = severity.ToUpper();
			log4net.GlobalContext.Properties["log_severity"] = severity.ToUpper();
		}

		public void SelectWriter(object message, Exception exception, LoggerDebugLevels selectedLevel)
		{
			switch (selectedLevel)
			{
				case LoggerDebugLevels.Emergency:
				case LoggerDebugLevels.Alert:
				case LoggerDebugLevels.Critical:
					log.Fatal(message, exception);
					break;
				case LoggerDebugLevels.Error:
					log.Error(message, exception);
					break;
				case LoggerDebugLevels.Warning:
					log.Warn(message, exception);
					break;
				case LoggerDebugLevels.Notification:
				case LoggerDebugLevels.Informational:
					log.Info(message, exception);
					break;
				case LoggerDebugLevels.Debug:
				case LoggerDebugLevels.Test:
				case LoggerDebugLevels.Profile:
				default:
					log.Debug(message, exception);
					break;
			}
		}

		public void WriteConfigurationDebuggingInformation(string format, params object[] values)
		{
			if (!DisplayConfigurationDebuggingInformation)
			{
				return;
			}
			Console.Error.WriteLine(format, values);
		}

		public void AddLogProperty(string name, object value)
		{
			log4net.GlobalContext.Properties[name] = value;
		}
	}
}