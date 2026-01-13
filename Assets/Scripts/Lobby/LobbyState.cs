using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;

public static class LobbyState
{
        public static string CurrentLobbyId { get; private set; } = string.Empty;
        public static bool IsHostLobby { get; private set; }

        public static void RegisterLobby(string lobbyId, bool isHost)
        {
                CurrentLobbyId = lobbyId ?? string.Empty;
                IsHostLobby = isHost;
        }

        public static void Clear()
        {
                CurrentLobbyId = string.Empty;
                IsHostLobby = false;
        }

        public static async Task LeaveLobbyAsync()
        {
                if (string.IsNullOrWhiteSpace(CurrentLobbyId))
                {
                        return;
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                        return;
                }

                try
                {
                        if (IsHostLobby)
                        {
                                await LobbyService.Instance.DeleteLobbyAsync(CurrentLobbyId);
                        }
                        else
                        {
                                string playerId = AuthenticationService.Instance.PlayerId;
                                if (!string.IsNullOrWhiteSpace(playerId))
                                {
                                        await LobbyService.Instance.RemovePlayerAsync(CurrentLobbyId, playerId);
                                }
                        }
                }
                catch (Exception ex)
                {
                        Debug.LogWarning($"Nie udało się opuścić lobby: {ex.Message}");
                }
                finally
                {
                        Clear();
                }
        }
}
