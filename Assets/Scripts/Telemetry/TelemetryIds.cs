using System;
using UnityEngine;

public static class TelemetryIds
{
    private const string PlayerIdKey = "telemetry.playerId";

    public static string GetOrCreatePlayerId()
    {
        string existing = PlayerPrefs.GetString(PlayerIdKey, string.Empty);
        if (!string.IsNullOrEmpty(existing))
        {
            return existing;
        }

        string created = Guid.NewGuid().ToString();
        PlayerPrefs.SetString(PlayerIdKey, created);
        PlayerPrefs.Save();
        return created;
    }

    public static string CreateMatchId()
    {
        return Guid.NewGuid().ToString();
    }
}
