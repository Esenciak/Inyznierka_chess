using System;
using Unity.Services.Authentication;
using UnityEngine;

public static class TelemetryIds
{
    private const string PlayerIdKey = "TelemetryPlayerId";

    public static string GetOrCreatePlayerId()
    {
        if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
        {
            string authPlayerId = AuthenticationService.Instance.PlayerId;
            if (!string.IsNullOrEmpty(authPlayerId))
            {
                return authPlayerId;
            }
        }

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
