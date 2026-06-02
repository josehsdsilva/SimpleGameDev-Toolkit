using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using System.Diagnostics;

[InitializeOnLoad]
internal class GitBranchToolbar
{
    private const string ENABLED_KEY = "SimpleGameDev_GitBranchEnabled";
    private const double REFRESH_INTERVAL_SECONDS = 30.0;

    private static string _cachedBranch;
    private static double _lastRefreshTime = double.NegativeInfinity;

    internal static bool IsEnabled => EditorPrefs.GetBool(ENABLED_KEY, true);

    static GitBranchToolbar() { }

    internal static void SetEnabled(bool enabled)
    {
        EditorPrefs.SetBool(ENABLED_KEY, enabled);
        _cachedBranch = null;
        _lastRefreshTime = double.NegativeInfinity;
    }

    [MainToolbarElement("GitBranch/Display", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement BranchLabel()
    {
        if (!IsEnabled)
        {
            return new MainToolbarLabel(new MainToolbarContent("", ""));
        }

        double now = EditorApplication.timeSinceStartup;
        if (_cachedBranch == null || now - _lastRefreshTime > REFRESH_INTERVAL_SECONDS)
        {
            _cachedBranch = GetCurrentGitBranch();
            _lastRefreshTime = now;
        }

        MainToolbarContent content = new MainToolbarContent($"\u2387 {_cachedBranch}", $"Git Branch: {_cachedBranch}");
        return new MainToolbarLabel(content);
    }

    static string GetCurrentGitBranch()
    {
        try
        {
            string repoPath = System.IO.Directory.GetParent(Application.dataPath).FullName;

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --abbrev-ref HEAD",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = repoPath
            };

            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    return output.Trim();
                }

                return "No Git";
            }
        }
        catch
        {
            return "No Git";
        }
    }
}
