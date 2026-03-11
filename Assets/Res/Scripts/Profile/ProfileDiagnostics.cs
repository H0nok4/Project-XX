using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class ProfileDiagnostics
{
    public sealed class Report
    {
        private readonly List<string> entries = new List<string>();

        public Report(string context)
        {
            Context = string.IsNullOrWhiteSpace(context) ? "Profile" : context.Trim();
        }

        public string Context { get; }
        public bool HasWarnings { get; private set; }
        public bool HasErrors { get; private set; }
        public bool HasEntries => entries.Count > 0;
        public IReadOnlyList<string> Entries => entries;

        public void Info(string message)
        {
            Add("INFO", message);
        }

        public void Warning(string message)
        {
            HasWarnings = true;
            Add("WARN", message);
        }

        public void Error(string message)
        {
            HasErrors = true;
            Add("ERROR", message);
        }

        public string BuildText()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[{Context}] {DateTime.UtcNow:O}");

            for (int index = 0; index < entries.Count; index++)
            {
                builder.AppendLine(entries[index]);
            }

            return builder.ToString().TrimEnd();
        }

        private void Add(string level, string message)
        {
            string sanitizedMessage = string.IsNullOrWhiteSpace(message) ? "(no details)" : message.Trim();
            entries.Add($"[{level}] {sanitizedMessage}");
        }
    }

    public static Report CreateReport(string context)
    {
        return new Report(context);
    }

    public static Report ValidateBeforeSave(PrototypeProfileService.ProfileData profile)
    {
        var report = CreateReport("ProfileSave");
        if (profile == null)
        {
            report.Error("Cannot save a null profile instance.");
            return report;
        }

        if (profile.profileSchemaVersion <= 0)
        {
            report.Warning("profileSchemaVersion was missing before save and has been normalized.");
        }

        if (profile.worldState == null)
        {
            report.Warning("worldState was missing before save and has been recreated.");
        }

        if (profile.progression == null)
        {
            report.Warning("progression was missing before save and has been recreated.");
        }

        return report;
    }

    public static void FlushToConsole(Report report)
    {
        if (report == null || !report.HasEntries)
        {
            return;
        }

        string message = report.BuildText();
        if (report.HasErrors)
        {
            Debug.LogError(message);
        }
        else if (report.HasWarnings)
        {
            Debug.LogWarning(message);
        }
        else
        {
            Debug.Log(message);
        }
    }
}
