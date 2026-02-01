using System;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public static class TelemetryIds
{
	private const string PlayerIdKey = "TelemetryPlayerId";

	/// <summary>
	/// Prefer authenticated UGS playerId when available (UGS initialized + signed in),
	/// otherwise fallback to a stable GUID stored in PlayerPrefs.
	/// </summary>
	public static string GetOrCreatePlayerId()
	{
		// IMPORTANT:
		// Do NOT touch AuthenticationService.Instance unless UnityServices is initialized,
		// otherwise it can throw ServicesInitializationException.
		if (UnityServices.State == ServicesInitializationState.Initialized)
		{
			try
			{
				if (AuthenticationService.Instance.IsSignedIn)
				{
					var authPlayerId = AuthenticationService.Instance.PlayerId;
					if (!string.IsNullOrEmpty(authPlayerId))
						return authPlayerId;
				}
			}
			catch (Exception)
			{
				// If anything goes wrong with auth, fall back to PlayerPrefs ID.
				// We intentionally swallow here to keep telemetry non-blocking.
			}
		}

		// Fallback: stable per device/install
		var existing = PlayerPrefs.GetString(PlayerIdKey, string.Empty);
		if (!string.IsNullOrEmpty(existing))
			return existing;

		var created = Guid.NewGuid().ToString();
		PlayerPrefs.SetString(PlayerIdKey, created);
		PlayerPrefs.Save();
		return created;
	}

	/// <summary>
	/// Offline fallback matchId. Online should use lobby ID (e.g., LobbyState.CurrentLobbyId).
	/// </summary>
	public static string CreateMatchId() => Guid.NewGuid().ToString();
}
