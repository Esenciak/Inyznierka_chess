# Telemetry Coordinate Conventions

- **Telemetry X/Y** map directly to `Tile.globalCol`/`Tile.globalRow`.
- `FromX/FromY` and `ToX/ToY` in JSON are the same values sent from `BattleMoveSync.SubmitMove()` using `tile.globalCol` and `tile.globalRow`.
- In multiplayer, clients may view a mirrored board; telemetry still logs the coordinates from the local player's perspective (the same numbers they submit), and the battle coordinate overlay shows those same values.
