using System;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public static class TelemetryIds
{
	private const string PlayerIdKey = "TelemetryPlayerId";

	/// <summary>

	/// </summary>
	public static string GetOrCreatePlayerId()
	{

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

			}
		}

		var existing = PlayerPrefs.GetString(PlayerIdKey, string.Empty);
		if (!string.IsNullOrEmpty(existing))
			return existing;

		var created = Guid.NewGuid().ToString();
		PlayerPrefs.SetString(PlayerIdKey, created);
		PlayerPrefs.Save();
		return created;
	}

	/// <summary>

	/// </summary>
	public static string CreateMatchId() => Guid.NewGuid().ToString();
}
