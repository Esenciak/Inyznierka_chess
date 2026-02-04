#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ConsoleWarningFilter
{
	private const string TlsAllocatorWarning = "TLS Allocator ALLOC_TEMP_TLS";
	private static bool pendingClear;

	static ConsoleWarningFilter()
	{
		Application.logMessageReceived += OnLogMessageReceived;
		EditorApplication.update += OnEditorUpdate;
	}

	private static void OnLogMessageReceived(string condition, string stacktrace, LogType type)
	{
		if (!string.IsNullOrEmpty(condition) && condition.Contains(TlsAllocatorWarning))
		{
			pendingClear = true;
		}
	}

	private static void OnEditorUpdate()
	{
		if (!pendingClear)
		{
			return;
		}

		pendingClear = false;
		ClearConsole();
	}

	private static void ClearConsole()
	{
		var logEntriesType = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");
		MethodInfo clearMethod = logEntriesType?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
		clearMethod?.Invoke(null, null);
	}
}
#endif
