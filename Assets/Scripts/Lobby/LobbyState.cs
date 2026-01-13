using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public static class LobbyState
{
        public static string CurrentLobbyId { get; private set; } = string.Empty;
        public static bool IsHostLobby { get; private set; }
        public static string LocalPlayerName { get; private set; } = "Ty";
        public static string OpponentPlayerName { get; private set; } = "Przeciwnik";

        public static void RegisterLobby(string lobbyId, bool isHost)
        {
                CurrentLobbyId = lobbyId ?? string.Empty;
                IsHostLobby = isHost;
        }

        public static void SetLocalPlayerName(string playerName)
        {
                if (!string.IsNullOrWhiteSpace(playerName))
                {
                        LocalPlayerName = playerName.Trim();
                }
        }

        public static void UpdateFromLobby(Lobby lobby, string localPlayerId)
        {
                if (lobby == null || lobby.Players == null)
                {
                        return;
                }

                foreach (var player in lobby.Players)
                {
                        string name = ExtractPlayerName(player);
                        if (!string.IsNullOrWhiteSpace(localPlayerId) && player.Id == localPlayerId)
                        {
                                LocalPlayerName = name;
                        }
                        else
                        {
                                OpponentPlayerName = name;
                        }
                }
        }

        public static void Clear()
        {
                CurrentLobbyId = string.Empty;
                IsHostLobby = false;
                LocalPlayerName = "Ty";
                OpponentPlayerName = "Przeciwnik";
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

        private static string ExtractPlayerName(Player player)
        {
                if (player == null)
                {
                        return "Gracz";
                }

                if (player.Data != null && player.Data.TryGetValue("name", out var data) && !string.IsNullOrWhiteSpace(data.Value))
                {
                        return data.Value;
                }

                return string.IsNullOrWhiteSpace(player.Id) ? "Gracz" : player.Id;
        }
}
