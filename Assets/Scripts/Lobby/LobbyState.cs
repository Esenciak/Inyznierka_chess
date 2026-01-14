using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public static class LobbyState
{
        public const string PlayerColorKey = "tileColor1";
        public static string CurrentLobbyId { get; private set; } = string.Empty;
        public static bool IsHostLobby { get; private set; }
        public static string LocalPlayerName { get; private set; } = "Ty";
        public static string OpponentPlayerName { get; private set; } = "Przeciwnik";
        public static Color LocalTileColor1 { get; private set; } = Color.white;
        public static bool HasLocalTileColor1 { get; private set; }
        public static Color OpponentTileColor1 { get; private set; } = Color.white;
        public static bool HasOpponentTileColor1 { get; private set; }

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

        public static void SetLocalTileColor1(Color color)
        {
                LocalTileColor1 = color;
                HasLocalTileColor1 = true;
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
                                if (TryExtractTileColor(player, out var color))
                                {
                                        LocalTileColor1 = color;
                                        HasLocalTileColor1 = true;
                                }
                        }
                        else
                        {
                                OpponentPlayerName = name;
                                if (TryExtractTileColor(player, out var color))
                                {
                                        OpponentTileColor1 = color;
                                        HasOpponentTileColor1 = true;
                                }
                        }
                }
        }

        public static void Clear()
        {
                CurrentLobbyId = string.Empty;
                IsHostLobby = false;
                LocalPlayerName = "Ty";
                OpponentPlayerName = "Przeciwnik";
                LocalTileColor1 = Color.white;
                OpponentTileColor1 = Color.white;
                HasLocalTileColor1 = false;
                HasOpponentTileColor1 = false;
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

        private static bool TryExtractTileColor(Player player, out Color color)
        {
                color = default;
                if (player?.Data == null)
                {
                        return false;
                }

                if (player.Data.TryGetValue(PlayerColorKey, out var data) && !string.IsNullOrWhiteSpace(data.Value))
                {
                        if (ColorUtility.TryParseHtmlString(data.Value, out color))
                        {
                                return true;
                        }
                }

                return false;
        }
}
