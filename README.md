# Co-op Physics Prototype

A small 2 to 4 player co-op prototype built on Netcode for GameObjects. Players connect to a
host, land in a lobby, load into the level together, and grab and throw shared physics props.

## Launch

**Hosting and joining**

- From the main menu, `Host` creates a session and `Join` connects to one.
- Transport is picked in one place (`AppConfig`, overridable via command line). Switching it
  never touches game code.
  - **Unity Transport.** Type an `IP:port` (or just the port) in the Join field. Works with
    several instances on one PC for local testing.
  - **Steam P2P.** No IP needed. The host creates a Steam lobby and shares either a
    5 character join code, or invites a friend directly from the in-game friends list (or
    Steam's own overlay and friends list).

For Steam mode to work during testing, the build first needs to be added to the local Steam
client as a non-Steam game, with `-transport steam` set as its launch option. This is only a
quirk of testing under the shared test AppID 480 (Spacewar), explained in more detail under
Known issues below. It shouldn't be needed once the project ships under its own Steam AppID.

**Command line arguments** (override the serialized `AppConfig` values at runtime)

| Argument | Values | Effect |
|---|---|---|
| `-transport` | `steam` / `ip` | Forces Steam P2P or Unity Transport |
| `-port` | a number | Overrides the listen/connect port (Unity Transport) |
| `-ip` | an address | Pre-fills the join address (Unity Transport) |

The build also needs `steam_appid.txt` (AppID 480, Spacewar) next to the executable for Steam
mode to initialize. If Steam isn't running, the game doesn't crash. It shows a clear message
instead, and falls back to Unity Transport if that path is configured.

**Controls**

| Action | Key |
|---|---|
| Move | WASD |
| Look | Mouse |
| Spawn item | G |
| Pick up / interact | F (hold) |
| Throw held item | Left Mouse Button |
| Delete held item | Delete |
| Change character color | V |
| Player list (name + ping, hold to view) | Tab |
| Kick a player (host only) | Kick button in the player list |

## Architecture

```
Game.Core          interfaces and DTOs, no Unity/Netcode/Steam dependency
                    (IConnectionService, ILobbyService, IPresenceService, IStatsService, DisconnectReason, AppConfig)
Game.Netcode        Netcode for GameObjects implementation of Game.Core's interfaces
                    (NetcodeConnectionService, ApprovalHandler, LobbySessionController, PingTracker, NetworkSceneLoader)
Game.Transport.Unity  Unity Transport installer (IP:port)
Game.Transport.Steam  Facepunch.Steamworks, Steam P2P transport installer, Steam Lobbies,
                       Rich Presence, Stats, SteamBootstrap (Steam session lifecycle)
Game.Gameplay       player controller, physics items, all UI, depends only on Game.Core
Game.Bootstrap      composition root (VContainer) that wires everything above together,
                    decides which transport to install, and parses command line overrides
```

Gameplay code only ever talks to `IConnectionService`, `ILobbyService`, `IPresenceService`
and `IStatsService`. It never references `NetworkManager`, `SteamClient`, or any transport
type directly. Swapping Unity Transport for Steam P2P, or adding a third transport, just
means registering a different implementation in `Game.Bootstrap`. No other layer changes.

Netcode spawns `NetworkBehaviour`s itself, so VContainer can't constructor-inject them. A
tiny service locator escape hatch (`GameServices.Current.Resolve<T>()`) lets them pull their
dependencies in `OnNetworkSpawn` instead.

**Extension points**

- New transport. Implement `ITransportConfigurator` and register it in `Game.Bootstrap`.
- New item type. Subclass or compose on top of `PhysicsItem`, using the same pickup, throw
  and ownership flow.

## Networking decisions

**Authority.** The host is authoritative for session state and for every physics item. Item
pickup, drop and throw are all validated server side, including pickup distance, whether the
item is already held, and whether the caller actually holds the item they're trying to drop
or throw. If two players grab the same item in the same frame, the server resolves it.
Whoever's request lands first wins, and the second request is simply rejected and does
nothing on that client, so there's no desync.

**Items.** An item is spawned by the host, owned by the server, and physics simulated on the
server. Its `NetworkTransform` is written by the server the whole time. On pickup,
`NetworkObject` ownership transfers to the holder, as the brief asks for, but transform
write authority stays with the server. While held, the item just snaps to the hand pose the
holding client reports. On drop or throw, ownership returns to the host and the item goes
back to being a free rigidbody. If the holder disconnects mid carry, the server force
releases the item so it drops in place instead of disappearing or getting stuck held
forever. That also needed `DontDestroyWithOwner` set on the item prefab, since Netcode
despawns a disconnecting owner's objects by default.

**Characters.** Movement is owner authoritative. Only the owning client reads input and
moves its own `CharacterController`, and `NetworkTransform` replicates the result to
everyone else. This is a co-op prototype, not a competitive shooter, so there's nothing to
cheat by moving your own character, and no reason to pay for server reconciliation. Remote
players are smoothed by `NetworkTransform`'s own interpolation rather than read as raw
per tick snapshots.

**Session lifecycle.** Connection approval on the host checks the build version, the player
count, a duplicate `PlayerId` (a locally saved GUID, or a SteamID in Steam mode), and a list
of players kicked this session, so a kicked player's reconnect attempt is rejected rather
than silently let back in. Scene loading after the lobby's Start button is a synced Netcode
scene event. The host tracks every client's load status, and the loading screen only drops
once everyone has finished. A late joiner instead loads the level on their own, catches up
through the networked state (which items exist, who's holding what), and nobody else waits
for them. A client disconnecting, including Alt+F4 or a dropped connection, never blocks the
rest of the session. If they were holding an item, it's released and drops where they stood.
The host leaving returns every client to the main menu with an on screen reason, and there's
no host migration.

**Steam Lobbies.** Lobbies are created Public rather than FriendsOnly or Private, which the
brief suggests as the lobby type. The reason is that joining by the 5 character code looks
the lobby up through Steam's lobby list search, and that search only ever returns Public
lobbies. FriendsOnly and Private lobbies are invisible to it no matter what key filters are
used, which is a Steam limitation rather than a bug on our side. Direct Steam invites, either
through the in-game friends list or Steam's own overlay, work identically regardless of
visibility, so they're unaffected by this. If strict FriendsOnly or Private visibility is
needed instead, the join by code flow should be dropped, or changed to join by the raw
numeric Steam lobby ID instead of a short code. Lobby visibility is set in exactly one place,
so that's the only change this would need.

A disconnect, whether it's a kick, the host leaving, or our own retry cleanup, shuts down the
Steam P2P transport, and that kills the local Steam session entirely rather than just the P2P
connection. This is a quirk of the community Facepunch transport package, not something we
trigger on purpose. The game detects this and reconnects to Steam automatically, both on the
next use and proactively when landing back on the main menu, so a kicked or disconnected
player never needs to restart the whole game to use Steam features again.

## Known issues

- Stats and Achievements is a logging stub, which the brief allows, and isn't wired to real
  Steam stat names.
- The native Steam overlay (Shift+Tab, Invite) didn't reliably trigger while testing under
  the shared test AppID 480 (Spacewar), unless the build was first added to the local Steam
  client as a non-Steam game with `-transport steam` set as its launch option. This is a
  quirk of testing under a shared test AppID and not something the code controls. It's
  expected to work normally once the project ships under its own real Steam AppID. The
  in-game custom friends list, built as a workaround, covers invites reliably regardless of
  this.

## Demo video

Two screens, one host and one client, captured at the same time.

1. **Join by code.** The client joins the host's lobby by typing the short join code, the
   fallback entry method that doesn't go through a Steam invite.
2. **Host leaves.** The host exits the game. The client sees a message that the host left the
   lobby and is returned to the main menu.
3. **Host restarts and invites through Steam.** The host creates a new lobby and invites the
   client directly through a Steam invite. The client accepts and joins.
4. **Start and gameplay.** The host starts the session. Core gameplay is shown, including
   movement and picking up and throwing physics items.
5. **Kick.** The host kicks the client mid session. The client sees the correct disconnect
   reason and can't rejoin for the rest of the session.
6. **Steam overlay.** The in-game Steam overlay is also shown separately.

One thing worth explaining about the Steam overlay specifically. Getting it to trigger
reliably (Shift+Tab and so on) required first adding the build to the local Steam client as
a non-Steam game, with `-transport steam` set as its launch option. That's a quirk of
testing under the shared test AppID 480 (Spacewar), not a limitation of the code itself.
Once the project ships under its own Steam AppID, the overlay should work correctly on its
own, without that extra setup step.

## Time spent

About 18 hours total.
