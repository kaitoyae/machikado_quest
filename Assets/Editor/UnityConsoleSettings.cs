using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class UnityConsoleSettings
{
    static UnityConsoleSettings()
    {
        // Unity Console の設定
        // スタックトレースを最小限に設定
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
    }
}