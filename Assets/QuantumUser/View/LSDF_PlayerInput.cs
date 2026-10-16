using System;
using Photon.Deterministic;
using UnityEngine;
namespace Quantum.Asteroids {
  public class LSDF_PlayerInput : MonoBehaviour {
    public QuantumEntityViewUpdater ViewUpdater;
    private IDisposable _subscription;
    private void OnEnable() {
      _subscription = QuantumCallback.SubscribeManual<CallbackPollInput>(callback => {
        if (ViewUpdater != null && callback.Game == ViewUpdater.ObservedGame) PollInput(callback);
      });
    }
    private void OnDisable() { _subscription?.Dispose(); _subscription = null; }
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
    public void PollInput(CallbackPollInput callback) { callback.SetInput(ReadInput(UnityEngine.Input.GetKey), DeterministicInputFlags.Repeatable); }
  }
}
