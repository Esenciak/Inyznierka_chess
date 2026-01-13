using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using UnityEngine;

public class LobbyServiceManager : MonoBehaviour
{
        public static LobbyServiceManager Instance { get; private set; }

        [Header("Lobby Settings")]
        [SerializeField] private int maxPlayers = 2;
        [SerializeField] private int lobbyNameMaxLength = 10;
        [SerializeField] private float heartbeatInterval = 15f;
        [SerializeField] private float pollInterval = 1.5f;

        private Lobby currentLobby;
        private bool isHost;
        private float heartbeatTimer;
        private CancellationTokenSource pollCts;

        private const string RelayJoinCodeKey = "relay_join_code";

        public Lobby CurrentLobby => currentLobby;
        public bool IsHost => isHost;

        private async void Awake()
        {
                if (Instance != null && Instance != this)
                {
                        Destroy(gameObject);
                        return;
                }

                Instance = this;
                DontDestroyOnLoad(gameObject);

                await InitializeServicesAsync();
        }

        private async Task InitializeServicesAsync()
        {
                if (UnityServices.State == ServicesInitializationState.Initialized)
                {
                        return;
                }

                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
        }

        private void Update()
        {
                if (!isHost || currentLobby == null)
                {
                        return;
                }

                heartbeatTimer += Time.deltaTime;
                if (heartbeatTimer >= heartbeatInterval)
                {
                        heartbeatTimer = 0f;
                        _ = SendHeartbeatAsync();
                }
        }

        public async Task<Lobby> CreateLobbyAsync(string lobbyName, bool isPrivate = false)
        {
                await InitializeServicesAsync();

                string sanitizedName = SanitizeLobbyName(lobbyName);
                var options = new CreateLobbyOptions
                {
                        IsPrivate = isPrivate,
                        Player = BuildPlayer()
                };

                currentLobby = await LobbyService.Instance.CreateLobbyAsync(sanitizedName, maxPlayers, options);
                isHost = true;
                StartPolling();
                return currentLobby;
        }

        public async Task<Lobby> QuickJoinAsync()
        {
                await InitializeServicesAsync();

                currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions
                {
                        Player = BuildPlayer()
                });

                isHost = false;
                StartPolling();
                return currentLobby;
        }

        public async Task<Lobby> JoinByCodeAsync(string lobbyCode)
        {
                await InitializeServicesAsync();

                currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
                {
                        Player = BuildPlayer()
                });

                isHost = false;
                StartPolling();
                return currentLobby;
        }

        public async Task SetRelayJoinCodeAsync(string joinCode)
        {
                if (currentLobby == null || !isHost)
                {
                        return;
                }

                var data = new Dictionary<string, DataObject>
                {
                        [RelayJoinCodeKey] = new DataObject(DataObject.VisibilityOptions.Public, joinCode)
                };

                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                {
                        Data = data
                });
        }

        public string GetRelayJoinCode()
        {
                if (currentLobby != null && currentLobby.Data != null && currentLobby.Data.TryGetValue(RelayJoinCodeKey, out var data))
                {
                        return data.Value;
                }

                return string.Empty;
        }

        public async Task LeaveLobbyAsync()
        {
                if (currentLobby == null)
                {
                        return;
                }

                string lobbyId = currentLobby.Id;

                if (isHost)
                {
                        await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                }
                else
                {
                        await LobbyService.Instance.RemovePlayerAsync(lobbyId, AuthenticationService.Instance.PlayerId);
                }

                ClearLobbyState();
        }

        private async Task SendHeartbeatAsync()
        {
                if (currentLobby == null || !isHost)
                {
                        return;
                }

                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
        }

        private void StartPolling()
        {
                pollCts?.Cancel();
                pollCts = new CancellationTokenSource();
                _ = PollLobbyLoopAsync(pollCts.Token);
        }

        private async Task PollLobbyLoopAsync(CancellationToken token)
        {
                while (!token.IsCancellationRequested && currentLobby != null)
                {
                        await Task.Delay(TimeSpan.FromSeconds(pollInterval), token);

                        try
                        {
                                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                                if (currentLobby.Players == null || currentLobby.Players.Count == 0)
                                {
                                        if (isHost)
                                        {
                                                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                                        }
                                        ClearLobbyState();
                                        return;
                                }
                        }
                        catch (LobbyServiceException)
                        {
                                ClearLobbyState();
                                return;
                        }
                }
        }

        private void ClearLobbyState()
        {
                pollCts?.Cancel();
                pollCts = null;
                currentLobby = null;
                isHost = false;
                heartbeatTimer = 0f;
        }

        private Player BuildPlayer()
        {
                return new Player(AuthenticationService.Instance.PlayerId);
        }

        private string SanitizeLobbyName(string lobbyName)
        {
                string name = string.IsNullOrWhiteSpace(lobbyName) ? "Lobby" : lobbyName.Trim();
                if (name.Length > lobbyNameMaxLength)
                {
                        name = name.Substring(0, lobbyNameMaxLength);
                }
                return name;
        }
}
