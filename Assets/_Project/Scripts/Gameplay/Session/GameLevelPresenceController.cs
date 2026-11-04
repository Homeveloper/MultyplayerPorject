using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Session
{
    // keeps Steam rich presence ("In Game 2/4") in sync with the roster while in GameLevel
    public class GameLevelPresenceController : MonoBehaviour
    {
        IPresenceService _presence;
        ISessionController _subscribedSession;

        void Start()
        {
            _presence = GameServices.Current.Resolve<IPresenceService>();
            ActiveSession.Changed += OnSessionChanged;
            OnSessionChanged();
        }

        void OnDestroy()
        {
            ActiveSession.Changed -= OnSessionChanged;
            if (_subscribedSession != null) _subscribedSession.Changed -= Refresh;
        }

        void OnSessionChanged()
        {
            if (_subscribedSession != null) _subscribedSession.Changed -= Refresh;
            _subscribedSession = ActiveSession.Current;
            if (_subscribedSession != null) _subscribedSession.Changed += Refresh;
            Refresh();
        }

        void Refresh()
        {
            if (_subscribedSession == null) return;
            var config = GameServices.Current.Resolve<AppConfig>();
            _presence.SetInGame(_subscribedSession.Players.Count, config.MaxPlayers);
        }
    }
}
