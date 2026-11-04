# Co-op Physics Prototype

A small 2-4 player co-op prototype built on Netcode for GameObjects. Players connect to a
host, land in a lobby, load into the level together, and grab/throw shared physics props.

## Launch

**Hosting / joining**

- From the main menu: `Host` creates a session, `Join` connects to one.
- Transport is picked in one place (`AppConfig`, overridable via command line) - switching
  it never touches game code.
  - **Unity Transport**: type an `IP:port` (or just the port) in the Join field. Works with
    several instances on one PC for local testing.
  - **Steam P2P**: no IP needed. Host creates a Steam lobby and shares either a 5-character
    join code, or invites a friend directly from the in-game friends list (or Steam's own
    overlay/friends list).

**Command line arguments** (override the serialized `AppConfig` values at runtime):

| Argument | Values | Effect |
|---|---|---|
| `-transport` | `steam` / `ip` | Forces Steam P2P or Unity Transport |
| `-port` | a number | Overrides the listen/connect port (Unity Transport) |
| `-ip` | an address | Pre-fills the join address (Unity Transport) |

Requires `steam_appid.txt` (AppID 480, Spacewar) next to the executable for Steam mode to
initialize. If Steam isn't running, the game falls back to showing a clear message instead
of crashing (and to Unity Transport, if that path is configured).

**Controls**

| Action | Key |
|---|---|
| Move | WASD |
| Look | Mouse |
| Jump | Space |
| Sprint | Left Shift |
| Crouch | C |
| Spawn item | G |
| Pick up / interact | F (hold) |
| Throw held item | Left Mouse Button |
| Delete held item | Delete |
| Change character color | V |
| Player list (name + ping, hold to view) | Tab |
| Kick a player (host only) | Kick button in the player list |

## Architecture

```
Game.Core          - interfaces, DTOs, no Unity/Netcode/Steam dependency
                      (IConnectionService, ILobbyService, IPresenceService, IStatsService, DisconnectReason, AppConfig)
Game.Netcode        - Netcode for GameObjects implementation of Game.Core's interfaces
                      (NetcodeConnectionService, ApprovalHandler, LobbySessionController, PingTracker, NetworkSceneLoader)
Game.Transport.Unity  - Unity Transport installer (IP:port)
Game.Transport.Steam  - Facepunch.Steamworks: Steam P2P transport installer, Steam Lobbies,
                         Rich Presence, Stats, SteamBootstrap (Steam session lifecycle)
Game.Gameplay       - player controller, physics items, all UI - depends only on Game.Core
Game.Bootstrap      - composition root (VContainer): wires everything above together,
                      decides which transport to install, parses command line overrides
```

Gameplay code only ever talks to `IConnectionService` / `ILobbyService` / `IPresenceService`
/ `IStatsService` - it never references `NetworkManager`, `SteamClient`, or any transport
type directly. Swapping Unity Transport for Steam P2P (or adding a third transport) means
registering a different implementation in `Game.Bootstrap`; no other layer changes.

Netcode spawns `NetworkBehaviour`s itself, so they can't be constructor-injected by VContainer.
A tiny service-locator escape hatch (`GameServices.Current.Resolve<T>()`) lets them pull
dependencies in `OnNetworkSpawn` instead.

**Extension points**

- New transport: implement `ITransportConfigurator`, register it in `Game.Bootstrap`.
- New item type: subclass/compose on top of `PhysicsItem`, same pickup/throw/ownership flow.

## Networking decisions

**Authority.** The host is authoritative for session state and every physics item. Item
pickup, drop, and throw are all validated server-side (pickup distance, whether the item is
already held, whether the caller actually holds the item they're trying to drop/throw). If
two players grab the same item in the same frame, the server resolves it: whoever's request
lands first wins, the second request is rejected and simply does nothing client-side - no
desync.

**Items.** An item is spawned by the host, server-owned, and physics-simulated on the server;
its `NetworkTransform` is written by the server the whole time. On pickup, `NetworkObject`
ownership transfers to the holder (per the brief) but transform write-authority stays with
the server - while held, the item just snaps to the hand pose the holding client reports.
On drop/throw, ownership returns to the host and it goes back to being a free rigidbody. If
the holder disconnects mid-carry, the server force-releases the item so it drops in place
instead of disappearing or getting stuck "held" forever (this needed `DontDestroyWithOwner`
set on the item prefab too, since Netcode despawns a disconnecting owner's objects by
default).

**Characters.** Owner-authoritative movement: only the owning client reads input and moves
its own `CharacterController`, `NetworkTransform` replicates the result to everyone else.
This is a co-op prototype, not a competitive shooter - there's nothing to cheat by moving
your own character, so there's no reason to pay for server reconciliation. Remote players are
smoothed by `NetworkTransform`'s own interpolation, not read as raw per-tick snapshots.

**Session lifecycle.** Connection approval on the host checks build version, player count,
duplicate `PlayerId` (a locally-saved GUID, or SteamID in Steam mode), and a "kicked this
session" list - a kicked player's reconnect attempt is rejected, not silently allowed back
in. Scene loading after the lobby's Start button is a synced Netcode scene event: the host
tracks every client's load status and the loading screen only drops once everyone has
finished. A late-joiner instead loads the level on their own, catches up via the networked
state (which items exist, who's holding what), and nobody else waits for them. A client
disconnecting (including Alt+F4 or a dropped connection) never blocks the rest of the
session; if they were holding an item, it's released and drops where they stood. The host
leaving returns every client to the main menu with an on-screen reason, no host migration.

**Steam Lobbies.** Lobbies are created **Public**, not `FriendsOnly`/`Private` as suggested
in the brief. Reason: joining by the 5-character code looks the lobby up via Steam's lobby
list search, and that search only ever returns Public lobbies - `FriendsOnly`/`Private`
lobbies are invisible to it no matter what key filters are used, that's a Steam limitation,
not a bug on our side. Direct Steam invites (in-game friends list or Steam's own overlay)
work identically regardless of visibility, so they're unaffected. If strict
`FriendsOnly`/`Private` visibility is required instead, the join-by-code flow should be
dropped (or changed to join by the raw numeric Steam lobby ID instead of a short code) -
lobby visibility is set in exactly one place, so that's the only change needed.

A disconnect (a kick, the host leaving, our own retry cleanup) shuts down the Steam P2P
transport, which kills the local Steam session entirely, not just the P2P connection - a
quirk of the community Facepunch transport package, not something we trigger on purpose.
The game detects this and reconnects to Steam automatically (on next use, and proactively
when landing back on the main menu) so a kicked or disconnected player never needs to
restart the whole game to use Steam features again.

## Known issues

- Connection timeout as an explicit rejection reason isn't separately implemented - Netcode's
  own built-in approval timeout handles it, but it surfaces through Netcode's own message
  rather than our friendly `DisconnectReason` text.
- Stats/Achievements is a logging stub (as allowed by the brief), not wired to real Steam
  stat names.
- No dedicated reconnect flow for a client that drops mid-session and wants back in without
  being kicked - they can rejoin like a fresh late-joiner, but there's no session resume.
