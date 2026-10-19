using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Photon.Realtime;
using Quantum;
using Quantum.LSDF;
using Quantum.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;

public class LSDFMatchReturnTests {
  [TestCase(true)] [TestCase(false)]
  public void FinalResultWaitsForBothDeadlinesAndRequestsOneDisconnect(bool firstWins) {
    using var fixture = new Fixture();
    FinalResult(firstWins, out var first, out var second);
    second.tickToEnableMove++;
    fixture.Observe(first, second, 219);
    fixture.Observe(first, second, 220);
    Assert.AreEqual(0, fixture.Connection.DisconnectCount);
    fixture.Observe(first, second, 221);
    fixture.Observe(first, second, 222);
    fixture.PressDisconnect();
    Assert.AreEqual(1, fixture.Connection.DisconnectCount);
    Assert.AreEqual(ConnectFailReason.UserRequest, fixture.Connection.Reason);
    Assert.AreEqual(0, fixture.Controller.MainCount, "Cleanup must finish before showing the menu.");
    Assert.AreEqual(3, Math.Max(first.loseRound, second.loseRound));
    Assert.IsTrue(first.isRoundEnd && second.isRoundEnd);
  }

  [Test]
  public void OrdinaryRoundDrawIntroAndUnconfirmedKoDoNotLeaveGameplay() {
    using var fixture = new Fixture();
    var first = new LSDF_Player { playerHp = 170 };
    var second = new LSDF_Player { playerHp = 0 };
    fixture.Observe(first, second, 1000);
    LSDFRoundPolicy.TryFinish(ref first, ref second, 100, 120);
    fixture.Observe(first, second, 1000);
    LSDFRoundPolicy.TryBeginNextRound(ref first, ref second, 1000, 170, 150);
    fixture.Observe(first, second, 2000);
    first = new LSDF_Player { playerHp = 0, loseRound = 2 };
    second = first;
    LSDFRoundPolicy.TryFinish(ref first, ref second, 100, 120);
    fixture.Observe(first, second, 2000);
    Assert.AreEqual(0, fixture.Connection.DisconnectCount);
    Assert.AreEqual(0, fixture.Controller.MainCount);
  }

  [UnityTest]
  public IEnumerator CleanupCompletesBeforeMainAndANewMatchCanReturnAgain() {
    using var fixture = new Fixture();
    FinalResult(true, out var first, out var second);
    fixture.Observe(first, second, 220);
    // A simultaneous plugin/transport error must not start another cleanup or reconnection.
    var process = typeof(QuantumMenuUIGameplay).GetMethod("ProcessDisconnect", BindingFlags.Instance | BindingFlags.NonPublic);
    var suppressed = (Task)process.Invoke(fixture.Screen, new object[] { "test-disconnect" });
    Assert.IsTrue(suppressed.IsCompleted);
    fixture.Connection.Complete();
    for (int i = 0; i < 10 && fixture.Controller.MainCount == 0; i++) yield return null;
    Assert.AreEqual(1, fixture.Controller.MainCount);
    fixture.Screen.Hide();
    fixture.Observe(first, second, 500);
    Assert.AreEqual(1, fixture.Connection.DisconnectCount);
    fixture.Connection.ResetCompletion();
    fixture.Screen.Show();
    fixture.Observe(first, second, 220);
    Assert.AreEqual(2, fixture.Connection.DisconnectCount);
    Assert.AreEqual(1, fixture.Controller.MainCount);
    fixture.Connection.Complete();
    for (int i = 0; i < 10 && fixture.Controller.MainCount == 1; i++) yield return null;
    Assert.AreEqual(2, fixture.Controller.MainCount);
  }

  [Test]
  public void MenuWithoutItsOwnRunnerDoesNotUseAnotherGame() {
    using var fixture = new Fixture();
    fixture.Screen.SendMessage("Update");
    Assert.IsNull(fixture.Connection.Runner);
    Assert.AreEqual(0, fixture.Connection.DisconnectCount);
  }

  private static void FinalResult(bool firstWins, out LSDF_Player first, out LSDF_Player second) {
    first = new LSDF_Player { playerHp = firstWins ? 170 : 0, loseRound = firstWins ? 0 : 2 };
    second = new LSDF_Player { playerHp = firstWins ? 0 : 170, loseRound = firstWins ? 2 : 0 };
    Assert.IsTrue(LSDFRoundPolicy.TryFinish(ref first, ref second, 100, 120));
  }

  private sealed class Fixture : IDisposable {
    public readonly QuantumMenuUIGameplay Screen;
    public readonly LSDFMatchReturnTestConnection Connection;
    public readonly LSDFMatchReturnTestController Controller;
    private readonly Scene _scene;
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public Fixture() {
      _scene = EditorSceneManager.OpenPreviewScene("Assets/Photon/QuantumMenu/LSDF_Menu.unity");
      Screen = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<QuantumMenuUIGameplay>(true)).Single();
      var host = new GameObject("match-return-test");
      SceneManager.MoveGameObjectToScene(host, _scene);
      Connection = host.AddComponent<LSDFMatchReturnTestConnection>();
      Controller = host.AddComponent<LSDFMatchReturnTestController>();
      Screen.Connection = Connection;
      Screen.Controller = Controller;
      Screen.Config = AssetDatabase.LoadAssetAtPath<QuantumMenuConfig>("Assets/QuantumUser/Resources/LSDF_MenuConfig.asset");
      Screen.ConnectionArgs = new QuantumMenuConnectArgs();
      Screen.UpdateUsernameRateInSeconds = 0;
      Screen.Show();
    }

    public void Observe(LSDF_Player first, LSDF_Player second, int tick) =>
      typeof(QuantumMenuUIGameplay).GetMethod("CheckLSDFMatchEnd", PrivateInstance).Invoke(Screen, new object[] { first, second, tick });

    public void PressDisconnect() =>
      typeof(QuantumMenuUIGameplay).GetMethod("OnDisconnectPressed", PrivateInstance).Invoke(Screen, null);

    public void Dispose() {
      Connection.Complete();
      Screen.Hide();
      EditorSceneManager.ClosePreviewScene(_scene);
    }
  }
}

public class LSDFMatchReturnTestConnection : QuantumMenuConnectionBehaviourSDK {
  private readonly RealtimeClient _client = new RealtimeClient();
  private TaskCompletionSource<bool> _completion = new TaskCompletionSource<bool>();
  public int DisconnectCount, Reason;
  public override RealtimeClient Client => _client;
  public override Task DisconnectAsync(int reason) {
    DisconnectCount++;
    Reason = reason;
    return _completion.Task;
  }
  public void Complete() => _completion.TrySetResult(true);
  public void ResetCompletion() => _completion = new TaskCompletionSource<bool>();
}

public class LSDFMatchReturnTestController : QuantumMenuUIController {
  public int MainCount;
  protected override void Awake() { }
  protected override void Start() { }
  public override void Show<S>() {
    Assert.AreEqual(typeof(QuantumMenuUIMain), typeof(S));
    MainCount++;
  }
}
