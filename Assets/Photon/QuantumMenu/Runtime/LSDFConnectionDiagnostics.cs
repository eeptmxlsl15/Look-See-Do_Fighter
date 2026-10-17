namespace Quantum.Menu {
  using System;
  using Photon.Realtime;
  using Quantum.LSDF;
  using UnityEngine;

  /// <summary>Keep the actual disconnect reason after the menu has finished its startup handshake.</summary>
  public sealed class LSDFConnectionDiagnostics : IDisposable {
    private readonly IDisposable _plugin, _checksum, _transport;

    public LSDFConnectionDiagnostics(QuantumGame game, RealtimeClient client) {
      _plugin = QuantumCallback.SubscribeManual<CallbackPluginDisconnect>(callback => {
        if (callback.Game == game) Report(game, "PluginDisconnect", callback.Reason);
      });
      _checksum = QuantumCallback.SubscribeManual<CallbackChecksumError>(callback => {
        if (callback.Game == game) Report(game, "ChecksumError", "Clients disagreed on the simulated frame.");
      });
      _transport = client.CallbackMessage.ListenManual<OnDisconnectedMsg>(message => {
        if (message.cause != DisconnectCause.DisconnectByClientLogic)
          Report(game, "TransportDisconnect", message.cause.ToString());
      });
    }

    private static void Report(QuantumGame game, string kind, string reason) {
      int tick = game.Frames.Verified != null ? game.Frames.Verified.Number : -1;
      // No AppId, client ID, authentication data or player names in this diagnostic envelope.
      Debug.LogWarning($"[LSDF Network] {kind}; tick={tick}; protocol={LSDFLoadout.ProtocolVersion}; reason={reason}");
    }

    public void Dispose() {
      _plugin.Dispose();
      _checksum.Dispose();
      _transport.Dispose();
    }
  }
}
