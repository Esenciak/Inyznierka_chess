# Telemetry Setup

## Files added/changed
- **Added** `Assets/Scripts/Telemetry/TelemetryConfig.cs`
- **Added** `Assets/Scripts/Telemetry/TelemetryService.cs`
- **Added** `Assets/Scripts/Telemetry/TelemetryEventBase.cs`
- **Added** `Assets/Scripts/Telemetry/TelemetryRoundBatchDto.cs`
- **Added** `Assets/Scripts/Telemetry/TelemetryHttpClient.cs`
- **Added** `Assets/Scripts/Telemetry/TelemetryQueueStorage.cs`
- **Added** `Assets/Scripts/Telemetry/TelemetryClock.cs`
- **Added** `Assets/Scripts/Telemetry/TelemetryIds.cs`
- **Added** `Assets/Scripts/Telemetry/TelemetryJson.cs`
- **Changed** `Assets/Scripts/Shop/ShopManager.cs`
- **Changed** `Assets/Scripts/PieceMovement.cs`
- **Changed** `Assets/Scripts/BattleMoveSync.cs`
- **Changed** `Assets/Scripts/GameManager/GameManager.cs`
- **Changed** `Assets/Scripts/Shop/EconomyConfig.cs`
- **Changed** `Assets/Scripts/UI/SceneExitPrompt.cs`

## Hook points (where telemetry is logged)
- **Round start + shop offer**: `ShopManager.InitializeShop()` and `ShopManager.RefillShop()`
- **Purchase/Reroll**: `ShopManager.TryBuyPiece()` and `ShopManager.TryRerollShop()`
- **Placement (setup board)**: `PieceMovement.OnMouseUp()` (when moving in Shop scene)
- **Battle start**: `GameManager.HandleSceneLoaded()` for `Battle` scene
- **Battle move/capture (offline + client)**: `PieceMovement.OnMouseUp()`
- **Battle move/capture (host/server)**: `BattleMoveSync.SubmitMoveServerRpc()`
- **Round end + batch send**: `GameManager.GameOver()`
- **Match end**: `GameManager.GameOver()` when `gamesPlayed >= 9`
- **Resign**: `BattleMoveSync.RequestResignServerRpc()` and `SceneExitPrompt.HandleBattleResign()`

## Creating and configuring TelemetryConfig (ScriptableObject)
1. In Unity, create a Resources folder if it does not exist: `Assets/Resources/Telemetry/`.
2. Right-click in the Project window → **Create** → **Chess** → **Telemetry Config**.
3. Save the asset as `TelemetryConfig` in `Assets/Resources/Telemetry/`.
4. Configure fields:
   - **baseUrl**: e.g. `https://example.com`
   - **roundBatchEndpointPath**: e.g. `/telemetry/round`
   - **requestTimeoutSeconds**: default `10`
   - **maxRetries**: default `3`
   - **flushIntervalSeconds**: default `15`
   - **enableTelemetry**: `true` to enable
   - **logToUnityConsole**: `true` for debug logging

> The URL is **only** controlled by `TelemetryConfig` (no hardcoding in code).

## Economy config version
`EconomyConfig` now has a `configVersion` string. Update it whenever you change prices/weights so telemetry batches can reference the balance version.

## Offline queue + retry behavior
- When sending fails (non-2xx response or network error), the batch JSON is saved to `Application.persistentDataPath/telemetry_queue/`.
- Files are written as `*.tmp` and then atomically renamed to reduce corruption risk.
- On startup and every `flushIntervalSeconds`, Telemetry attempts to resend queued batches.
- Retry backoff: **1s → 3s → 7s**, up to `maxRetries` attempts per send.

## Testing & debugging
- Point `baseUrl` at a test HTTP endpoint that accepts POST JSON (e.g., a local mock server).
- Enable `logToUnityConsole` to see `[Telemetry] Logged ...` messages.
- To simulate offline, disable network or use an invalid URL; confirm files appear under `Application.persistentDataPath/telemetry_queue/`.

## Event list and fields
All events share base fields:
- `eventId`, `matchId`, `playerId`, `roundNumber`, `eventType`, `timestampUtc`, `clientTimeMsFromMatchStart`

Additional fields per event:
- **ShopOfferGenerated**: `offeredPieces`, `shopSlots`, `rerollCost`
- **Purchase**: `pieceType`, `price`, `shopSlotIndex`, `coinsBefore`, `coinsAfter`
- **Reroll**: `cost`, `coinsBefore`, `coinsAfter`
- **Sell**: `pieceType`, `refund`, `coinsBefore`, `coinsAfter`
- **PiecePlaced**: `pieceType`, `toX`, `toY`, `source` (Inventory|Swap|Initial), `boardContext` (Setup)
- **BattleStart**: `boardSize`
- **PieceMoved**: `pieceType`, `fromX`, `fromY`, `toX`, `toY`, `boardContext` (Battle)
- **PieceCaptured**: `pieceType`, `fromX`, `fromY`, `toX`, `toY`, `capturedPieceType`, `boardContext` (Battle)
- **RoundEnd**: `playerWon`, `coinsEnd`, `piecesRemaining`, `boardSize`
- **MatchEnd**: `winnerColor`, `reason`, `totalRounds`

> Note: King is **never** logged as a shop offer or purchase. King can still appear in battle events.

## Example batch JSON (round 1)
```json
{
  "matchId": "7f7d8d6b-1c8b-4b78-9b2d-5f0b447b74a4",
  "playerId": "2d3f6db4-0d45-4f97-8a2c-0f274da2ef35",
  "roundNumber": 1,
  "balanceVersion": "v1",
  "boardSize": 3,
  "coinsBeforeShop": 100,
  "coinsAfterShop": 85,
  "events": [
    {
      "eventId": "6e2f3d1d-5f50-4b97-9a6b-4f904e6c9f2e",
      "matchId": "7f7d8d6b-1c8b-4b78-9b2d-5f0b447b74a4",
      "playerId": "2d3f6db4-0d45-4f97-8a2c-0f274da2ef35",
      "roundNumber": 1,
      "eventType": "RoundStart",
      "timestampUtc": "2025-01-01T12:00:00.000Z",
      "clientTimeMsFromMatchStart": 10
    },
    {
      "eventId": "5bd8f9b6-4b39-4f1e-9820-60d8a2f3c1a0",
      "matchId": "7f7d8d6b-1c8b-4b78-9b2d-5f0b447b74a4",
      "playerId": "2d3f6db4-0d45-4f97-8a2c-0f274da2ef35",
      "roundNumber": 1,
      "eventType": "ShopOfferGenerated",
      "timestampUtc": "2025-01-01T12:00:00.100Z",
      "clientTimeMsFromMatchStart": 110,
      "offeredPieces": ["Pawn", "Knight", "Bishop"],
      "shopSlots": 6,
      "rerollCost": 5
    },
    {
      "eventId": "2f0b5bda-0a1e-4f10-83a3-0a7f17af7f2b",
      "matchId": "7f7d8d6b-1c8b-4b78-9b2d-5f0b447b74a4",
      "playerId": "2d3f6db4-0d45-4f97-8a2c-0f274da2ef35",
      "roundNumber": 1,
      "eventType": "Purchase",
      "timestampUtc": "2025-01-01T12:00:05.000Z",
      "clientTimeMsFromMatchStart": 5100,
      "pieceType": "Pawn",
      "price": 10,
      "shopSlotIndex": 2,
      "coinsBefore": 100,
      "coinsAfter": 90
    },
    {
      "eventId": "b24b5b19-5b38-4f71-9a5c-1c4ce8140e45",
      "matchId": "7f7d8d6b-1c8b-4b78-9b2d-5f0b447b74a4",
      "playerId": "2d3f6db4-0d45-4f97-8a2c-0f274da2ef35",
      "roundNumber": 1,
      "eventType": "BattleStart",
      "timestampUtc": "2025-01-01T12:01:00.000Z",
      "clientTimeMsFromMatchStart": 60000,
      "boardSize": 3
    },
    {
      "eventId": "e0b8e12b-44ef-49b4-8b0f-3c6b3d2b9bde",
      "matchId": "7f7d8d6b-1c8b-4b78-9b2d-5f0b447b74a4",
      "playerId": "2d3f6db4-0d45-4f97-8a2c-0f274da2ef35",
      "roundNumber": 1,
      "eventType": "RoundEnd",
      "timestampUtc": "2025-01-01T12:03:00.000Z",
      "clientTimeMsFromMatchStart": 180000,
      "playerWon": true,
      "coinsEnd": 110,
      "piecesRemaining": 3,
      "boardSize": 3
    }
  ]
}
```

## TODO / limitations
- **Sell** event is wired in telemetry service, but the project currently has no sell mechanic to hook.
- Telemetry requires `TelemetryConfig` to exist in `Resources/Telemetry/` for auto-loading.
- If the backend schema evolves, update `TelemetryEventBase` and `TelemetryRoundBatchDto` accordingly.
