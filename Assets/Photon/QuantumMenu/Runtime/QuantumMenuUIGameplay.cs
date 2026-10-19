namespace Quantum.Menu {
  using System.Collections;
  using System.Text;
  using Quantum.LSDF;
#if QUANTUM_ENABLE_TEXTMESHPRO
  using Text = TMPro.TMP_Text;
#else 
  using Text = UnityEngine.UI.Text;
#endif
  using UnityEngine;
  using UnityEngine.UI;
  using static QuantumUnityExtensions;

  /// <summary>
  /// The gameplay screen.
  /// </summary>
  public partial class QuantumMenuUIGameplay : QuantumMenuUIScreen {
    /// <summary>
    /// The session code label.
    /// </summary>
    [InlineHelp, SerializeField] protected Text _codeText;
    /// <summary>
    /// The list of players.
    /// </summary>
    [InlineHelp, SerializeField] protected Text _playersText;
    /// <summary>
    /// The current player count.
    /// </summary>
    [InlineHelp, SerializeField] protected Text _playersCountText;
    /// <summary>
    /// The max player count.
    /// </summary>
    [InlineHelp, SerializeField] protected Text _playersMaxCountText;
    /// <summary>
    /// The menu header text.
    /// </summary>
    [InlineHelp, SerializeField] protected Text _headerText;
    /// <summary>
    /// The GameObject of the session part to be toggled off.
    /// </summary>
    [InlineHelp, SerializeField] protected GameObject _sessionGameObject;
    /// <summary>
    /// The GameObject of the player part to be toggled off.
    /// </summary>
    [InlineHelp, SerializeField] protected GameObject _playersGameObject;
    /// <summary>
    /// The copy session button.
    /// </summary>
    [InlineHelp, SerializeField] protected Button _copySessionButton;
    /// <summary>
    /// The disconnect button.
    /// </summary>
    [InlineHelp, SerializeField] protected Button _disconnectButton;
    /// <summary>
    /// Uses FindFirstObjectByType to find a camera to toggle on/off when going and leaving into the game screen.
    /// Sets <see cref="_menuCamera"/>.
    /// </summary>
    [InlineHelp, SerializeField] protected bool _detectAndToggleMenuCamera;
    /// <summary>
    /// Toggles this camera on/off when entering or leaving the game screen.
    /// </summary>
    [InlineHelp, SerializeField] protected Camera _menuCamera;
    /// <summary>
    /// In what frequency are the usernames refreshed.
    /// </summary>
    [InlineHelp] public float UpdateUsernameRateInSeconds = 2;
    /// <summary>
    /// The coroutine is started during Show() and updates the Usernames using this interval <see cref="UpdateUsernameRateInSeconds"/>.
    /// </summary>
    protected Coroutine _updateUsernamesCoroutine;
    private bool _isLeavingGameplay;
    private bool _matchReturnRequested;

    partial void AwakeUser();
    partial void InitUser();
    partial void ShowUser();
    partial void HideUser();

    /// <summary>
    /// The Unity awake method. Calls partial method <see cref="AwakeUser"/> to be implemented on the SDK side.
    /// </summary>
    public override void Awake() {
      base.Awake();
      AwakeUser();
    }

    /// <summary>
    /// The screen init method. Calls partial method <see cref="InitUser"/> to be implemented on the SDK side.
    /// </summary>
    public override void Init() {
      base.Init();
      InitUser();

      if (_detectAndToggleMenuCamera) {
        _menuCamera = FindFirstObjectByType<Camera>();
      }
    }

    /// <summary>
    /// The screen show method. Calls partial method <see cref="ShowUser"/> to be implemented on the SDK side.
    /// Will check is the session code is compatible with the party code to toggle the session UI part.
    /// </summary>
    public override void Show() {
      base.Show();
      _isLeavingGameplay = false;
      _matchReturnRequested = false;
      ShowUser();

      if (_menuCamera != null ) {
        _menuCamera.enabled = false;
      }

      if (Config.CodeGenerator != null && Config.CodeGenerator.IsValid(Connection.SessionName)) {
        // Only show the session UI if it is a party code
        _codeText.text = Connection.SessionName;
        _sessionGameObject.SetActive(true);
      } else {
        _codeText.text = string.Empty;
        _sessionGameObject.SetActive(false);
      }

      UpdateUsernames();

      if (UpdateUsernameRateInSeconds > 0) {
        _updateUsernamesCoroutine = StartCoroutine(UpdateUsernamesCoroutine());
      }
    }

    /// <summary>
    /// The screen hide method. Calls partial method <see cref="HideUser"/> to be implemented on the SDK side.
    /// </summary>
    public override void Hide() {
      _isLeavingGameplay = true;
      base.Hide();
      HideUser();

      if (_menuCamera != null) {
        _menuCamera.enabled = true;
      }

      if (_updateUsernamesCoroutine != null) {
        StopCoroutine(_updateUsernamesCoroutine);
        _updateUsernamesCoroutine = null;
      }
    }

    /// <summary>
    /// Is called when the <see cref="_disconnectButton"/> is pressed using SendMessage() from the UI object.
    /// </summary>
    protected virtual async void OnDisconnectPressed() {
      if (_isLeavingGameplay || Connection == null || Controller == null) return;
      _isLeavingGameplay = true;
      _matchReturnRequested = true;
      var connection = Connection;
      var controller = Controller;
      try {
        await connection.DisconnectAsync(ConnectFailReason.UserRequest);
        if (this != null && Connection == connection && Controller == controller) {
          controller.Show<QuantumMenuUIMain>();
        }
      } catch (System.Exception exception) {
        Debug.LogException(exception);
        // Leave automatic return latched; the disconnect button can retry explicitly.
        if (this != null) _isLeavingGameplay = false;
      }
    }

    private void Update() {
      if (!IsShowing || _isLeavingGameplay || _matchReturnRequested ||
          !(Connection is QuantumMenuConnectionBehaviourSDK connection)) return;
      // Use only this menu's runner and its confirmed result, never a predicted KO.
      var confirmed = connection.Runner?.Game?.Frames.Verified;
      if (confirmed == null || !LSDFRoster.TryPair(confirmed, out var first, out var second) ||
          !confirmed.TryGet<LSDF_Player>(first, out var a) ||
          !confirmed.TryGet<LSDF_Player>(second, out var b)) return;
      CheckLSDFMatchEnd(a, b, confirmed.Number);
    }

    protected void CheckLSDFMatchEnd(in LSDF_Player first, in LSDF_Player second, int verifiedTick) {
      if (!IsShowing || _isLeavingGameplay || _matchReturnRequested ||
          LSDFRoundPolicy.Phase(true, first, second, verifiedTick) != LSDFRoundPhase.MatchEnd ||
          verifiedTick < first.tickToEnableMove || verifiedTick < second.tickToEnableMove) return;
      // Preserve the same result-display interval used between ordinary rounds.
      OnDisconnectPressed();
    }

    private void OnDisable() {
      _isLeavingGameplay = true;
    }

    /// <summary>
    /// Is called when the <see cref="_copySessionButton"/> is pressed using SendMessage() from the UI object.
    /// </summary>
    protected virtual void OnCopySessionPressed() {
      GUIUtility.systemCopyBuffer = _codeText.text;
    }

    /// <summary>
    /// Update the usernames list. Will cancel itself if UpdateUsernameRateInSeconds less or equal to 0.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator UpdateUsernamesCoroutine() {
      while (UpdateUsernameRateInSeconds > 0) {
        yield return new WaitForSeconds(UpdateUsernameRateInSeconds);
        UpdateUsernames();
      }
    }

    /// <summary>
    /// Update the usernames and toggle the UI part on/off depending on the <see cref="QuantumMenuConnectionBehaviour.Usernames"/>
    /// </summary>
    protected virtual void UpdateUsernames() {
      if (Connection.Usernames != null && Connection.Usernames.Count > 0) {
        _playersGameObject.SetActive(true);
        var sBuilder = new StringBuilder();
        var playerCount = 0;
        foreach (var username in Connection.Usernames) {
          sBuilder.AppendLine(username);
          playerCount += string.IsNullOrEmpty(username) ? 0 : 1;
        }
        _playersText.text = sBuilder.ToString();
        _playersCountText.text = $"{playerCount}";
        _playersMaxCountText.text = $"/{Connection.MaxPlayerCount}";
      } else {
        _playersGameObject.SetActive(false);
      }
    }
  }
}
