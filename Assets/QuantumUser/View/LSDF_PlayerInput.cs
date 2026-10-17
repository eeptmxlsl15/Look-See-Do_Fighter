using System;
using Photon.Deterministic;
using UnityEngine;
namespace Quantum.Asteroids {
  public class LSDF_PlayerInput : MonoBehaviour {
    public QuantumEntityViewUpdater ViewUpdater;
    public LSDFPlayer2TestInputSettings Player2TestInput;
    private readonly LSDFPlayer2TestInputPlayback _testPlayback = new LSDFPlayer2TestInputPlayback();
    private IDisposable _subscription;
    private void OnEnable() {
      _subscription?.Dispose();
      _testPlayback.Reset();
      _subscription = QuantumCallback.SubscribeManual<CallbackPollInput>(callback => {
        if (ViewUpdater != null && callback.Game == ViewUpdater.ObservedGame) PollInput(callback);
      });
    }
    private void OnDisable() { _subscription?.Dispose(); _subscription = null; _testPlayback.Reset(); }
    public static Quantum.Input ReadInput(Func<KeyCode, bool> pressed) {
      return new Quantum.Input {
        Left = pressed(KeyCode.A) || pressed(KeyCode.LeftArrow),
        Right = pressed(KeyCode.D) || pressed(KeyCode.RightArrow),
        Up = pressed(KeyCode.W) || pressed(KeyCode.UpArrow),
        Down = pressed(KeyCode.S) || pressed(KeyCode.DownArrow),
        LeftPunch = pressed(KeyCode.U), RightPunch = pressed(KeyCode.I),
        LeftKick = pressed(KeyCode.J), RightKick = pressed(KeyCode.K)
      };
    }
    public void PollInput(CallbackPollInput callback) {
      if (ViewUpdater == null || callback.Game == null || callback.Game != ViewUpdater.ObservedGame) return;
      var input = ReadInput(UnityEngine.Input.GetKey);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
      var game = callback.Game;
      // PlayerSlot is client-local: 2P normally also has PlayerSlot 0 on a separate client.
      if (!game.Session.IsReplay && !game.Session.IsSpectating &&
          game.Session.TryGetLocalPlayer(callback.PlayerSlot, out var player) && player == (PlayerRef)1) {
        var frame = game.Frames.Verified;
        bool ready = frame != null && Quantum.LSDF.LSDFRoster.TryFind(frame, player, out var entity) &&
          frame.TryGet<LSDF_Player>(entity, out var state) && state.playerHp > 0 &&
          !state.isRoundEnd && frame.Number >= state.tickToEnableMove;
        if (_testPlayback.TrySample(game, player, Player2TestInput, ready, callback.Frame, out var testInput))
          input = testInput;
      }
#endif
      callback.SetInput(input, DeterministicInputFlags.Repeatable);
    }
  }
}
