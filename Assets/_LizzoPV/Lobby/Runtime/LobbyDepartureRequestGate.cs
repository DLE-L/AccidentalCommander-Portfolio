using System;

namespace Lizzo.PV.Lobby
{
    public enum LobbyDepartureState
    {
        Loading,
        Ready,
        Starting,
    }

    public sealed class LobbyDepartureRequestGate
    {
        readonly Func<bool> _requestGameplay;

        public LobbyDepartureRequestGate(Func<bool> requestGameplay)
        {
            _requestGameplay = requestGameplay ?? throw new ArgumentNullException(nameof(requestGameplay));
        }

        public LobbyDepartureState State { get; private set; } = LobbyDepartureState.Loading;

        public void SetReady()
        {
            State = LobbyDepartureState.Ready;
        }

        public bool TryStart()
        {
            if (State != LobbyDepartureState.Ready)
                return false;

            State = LobbyDepartureState.Starting;
            if (_requestGameplay())
                return true;

            State = LobbyDepartureState.Ready;
            return false;
        }
    }
}
