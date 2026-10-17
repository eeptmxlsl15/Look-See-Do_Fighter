using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Quantum;
using Quantum.LSDF;
using Quantum.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LSDFStructuralTests {
  [TestCase(0,0,0)] [TestCase(4,3,19)] [TestCase(6,3,27)]
  [TestCase(-1,0,-1)] [TestCase(7,0,-1)] [TestCase(0,4,-1)]
  public void SlotMappingHasOneSchema(int direction, int button, int expected) {
    Assert.AreEqual(expected, LSDFLoadout.Slot((CommandDirection)direction, (CommandButton)button));
  }
  [Test]
  public void BudgetValidationIsAtomicAndDetached() {
    var config = ScriptableObject.CreateInstance<LSDF_GameConfig>();
    try {
      config.SkillCosts = new int[84]; config.SkillCosts[1] = 200;
      var model = new LSDFLoadoutSelection(new int[28], config, 140);
      Assert.IsFalse(model.TrySelect(0, 1)); Assert.AreEqual(0, model.Skill(0));
      config.SkillCosts[1] = 20;
      Assert.IsTrue(model.TrySelect(0, 1)); Assert.AreEqual(20, model.Cost);
      var snapshot = model.Snapshot(); snapshot[0] = 0; Assert.AreEqual(1, model.Skill(0));
      config.MaximumLoadoutCost = 10;
      CollectionAssert.AreEqual(new int[28], LSDFLoadout.CopyBudgetValidated(model.Snapshot(), config));
      Assert.IsFalse(model.TrySelect(-1, 0)); Assert.IsFalse(model.TrySelect(0, 9));
    } finally { Object.DestroyImmediate(config); }
  }
  [Test]
  public void ExistingBalanceAssetIsWiredAndKeepsDefaults() {
    var config = AssetDatabase.LoadAssetAtPath<LSDF_GameConfig>("Assets/QuantumUser/Resources/LSDF_GameConfig.asset");
    Assert.IsNotNull(config); Assert.AreEqual(170, config.MaximumHealth); Assert.AreEqual(2, config.HealingPerTick);
    Assert.AreEqual(210, config.RoundLockTicks); Assert.AreEqual(150, config.HealingLeadTicks);
    Assert.AreEqual(32768, config.WalkSpeed.RawValue); Assert.AreEqual(131072, config.DashSpeed.RawValue);
    Assert.AreEqual(47, LSDFLoadout.Cost(new int[28], config));
    var menu = AssetDatabase.LoadAssetAtPath<Quantum.Menu.QuantumMenuConfig>("Assets/QuantumUser/Resources/LSDF_MenuConfig.asset");
    Assert.AreEqual(2, menu.MaxPlayerCount);
    Assert.IsTrue(menu.AvailableScenes[0].RuntimeConfig.GameConfig.Id.IsValid);
  }
  [TestCase(0)] [TestCase(-10)]
  public void KoCountsOnceAndWaitsForExplicitRoundReset(int hp) {
    var a = new LSDF_Player { playerHp = hp };
    var b = new LSDF_Player { playerHp = 170 };
    Assert.IsTrue(LSDFRoundPolicy.TryFinish(ref a, ref b));
    Assert.IsFalse(LSDFRoundPolicy.TryFinish(ref a, ref b));
    Assert.AreEqual(1, a.loseRound); Assert.AreEqual(0, a.playerHp); Assert.AreEqual(0, b.loseRound);
    Assert.AreEqual(LSDFRoundPhase.RoundEnd, LSDFRoundPolicy.Phase(true, a, b, 309));
    Assert.AreEqual(LSDFRoundPhase.RoundEnd, LSDFRoundPolicy.Phase(true, a, b, 31000));
    Assert.AreEqual(LSDFRoundPhase.WaitingForPlayers, LSDFRoundPolicy.Phase(false, a, b, 310));
  }
  [Test]
  public void ContactPriorityPreservesDodgeWallCounterAndStun() {
    var defender = new LSDF_Player { isDodgeHigh = true, canCounter = true, isOnWall = true, isAir = true };
    var hit = new LSDF_HitboxInfo { AttackType = HitboxAttackType.High, startFrame = 10 };
    Assert.AreEqual(LSDFContactReaction.Ignore, LSDFContactPolicy.Evaluate(defender, hit, "Idle"));
    defender.isDodgeHigh = false;
    Assert.AreEqual(LSDFContactReaction.Normal, LSDFContactPolicy.Evaluate(defender, hit, "Idle"));
    defender.isOnWall = false;
    Assert.AreEqual(LSDFContactReaction.Counter, LSDFContactPolicy.Evaluate(defender, hit, "Stun"));
    defender.canCounter = false;
    Assert.AreEqual(LSDFContactReaction.StunHit, LSDFContactPolicy.Evaluate(defender, hit, "Stun"));
    hit.startFrame = 11;
    Assert.AreEqual(LSDFContactReaction.StunGuard, LSDFContactPolicy.Evaluate(defender, hit, "Stun"));
    Assert.AreEqual(LSDFContactReaction.ComboHit, LSDFContactPolicy.Evaluate(defender, hit, "Combo"));
    Assert.AreEqual(LSDFContactReaction.Guard, LSDFContactPolicy.Evaluate(defender, hit, "Idle"));
    hit.HomingReturnType = HomingType.Stun;
    Assert.AreEqual(LSDFContactReaction.ParryAttacker, LSDFContactPolicy.Evaluate(defender, hit, "Parring"));
    hit.HomingReturnType = HomingType.Homing;
    Assert.AreEqual(LSDFContactReaction.Normal, LSDFContactPolicy.Evaluate(defender, hit, "Parring"));
  }
  [TestCase(HitboxAttackType.High,"Idle",false,true)]
  [TestCase(HitboxAttackType.Mid,"Rising",false,true)]
  [TestCase(HitboxAttackType.Low,"Idle",false,false)]
  [TestCase(HitboxAttackType.Low,"Idle",true,true)]
  [TestCase(HitboxAttackType.High,"Siting",true,false)]
  public void GuardMatrixKeepsLegacyRules(HitboxAttackType type, string state, bool sit, bool expected) {
    Assert.AreEqual(expected, LSDFContactPolicy.ShouldGuard(type, state, sit));
  }
  [TestCase(KeyCode.UpArrow,true,false)] [TestCase(KeyCode.DownArrow,false,true)]
  public void ArrowInputDoesNotSetTheOppositeDirection(KeyCode key, bool up, bool down) {
    var input = Quantum.Asteroids.LSDF_PlayerInput.ReadInput(candidate => candidate == key);
    Assert.AreEqual(up, (bool)input.Up); Assert.AreEqual(down, (bool)input.Down);
  }
  [Test]
  public void PredictedEffectsConfirmCancelAndReuseIndependently() {
    var first = new LSDFPresentationLedger<string>(); var second = new LSDFPresentationLedger<string>();
    var key = new EventKey(30, 6, 1); first.Track(key, "effect");
    Assert.IsFalse(second.Cancel(key, out _)); Assert.IsTrue(first.Cancel(key, out var effect)); Assert.AreEqual("effect", effect);
    first.Track(key, "confirmed"); first.Confirm(key); Assert.IsFalse(first.Cancel(key, out _));
    first.Track(key, "source"); first.Forget("source"); Assert.AreEqual(0, first.Count);
  }
  [TestCase(false)] [TestCase(true)]
  public void OnlyTheDamagedHealthBarShakesAndRepeatedVerifiedFramesDoNotRetrigger(bool damageRight) {
    var left = new GameObject("left-health", typeof(RectTransform)).GetComponent<RectTransform>();
    var right = new GameObject("right-health", typeof(RectTransform)).GetComponent<RectTransform>();
    var leftShake = new LSDFHealthBarShake(); var rightShake = new LSDFHealthBarShake();
    var leftOrigin = new Vector2(-100, 20); var rightOrigin = new Vector2(100, 20);
    left.anchoredPosition = leftOrigin; right.anchoredPosition = rightOrigin;
    try {
      var healthy = new LSDF_Player { playerHp = 170 };
      var hit = new LSDF_Player { playerHp = 158, isHit = true, DelayFrame = 35 };
      leftShake.Observe(left, default, healthy, 10); rightShake.Observe(right, default, healthy, 10);
      leftShake.Observe(left, default, damageRight ? healthy : hit, 11);
      rightShake.Observe(right, default, damageRight ? hit : healthy, 11);
      leftShake.Advance(.03f, .22f, 6); rightShake.Advance(.03f, .22f, 6);
      Assert.AreEqual(!damageRight, leftShake.IsShaking); Assert.AreEqual(damageRight, rightShake.IsShaking);
      Assert.AreEqual(damageRight, left.anchoredPosition == leftOrigin);
      Assert.AreEqual(!damageRight, right.anchoredPosition == rightOrigin);
      leftShake.Advance(1, .22f, 6); rightShake.Advance(1, .22f, 6);
      Assert.AreEqual(leftOrigin, left.anchoredPosition); Assert.AreEqual(rightOrigin, right.anchoredPosition);
      leftShake.Observe(left, default, damageRight ? healthy : hit, 11);
      rightShake.Observe(right, default, damageRight ? hit : healthy, 11);
      Assert.IsFalse(leftShake.IsShaking || rightShake.IsShaking);
    } finally {
      leftShake.Reset(); rightShake.Reset(); Object.DestroyImmediate(left.gameObject); Object.DestroyImmediate(right.gameObject);
    }
  }
  [Test]
  public void HealthShakeHandlesZeroDamageHitsGuardRecoveryAndHudRebinding() {
    var first = new GameObject("first-hud", typeof(RectTransform)).GetComponent<RectTransform>();
    var second = new GameObject("second-hud", typeof(RectTransform)).GetComponent<RectTransform>();
    var shake = new LSDFHealthBarShake(); var origin = new Vector2(40, 12);
    first.anchoredPosition = origin;
    try {
      var player = new LSDF_Player { playerHp = 170 };
      shake.Observe(first, default, player, 10);
      player.isGuard = true; player.DelayFrame = 30;
      shake.Observe(first, default, player, 11); Assert.IsFalse(shake.IsShaking);
      player.isGuard = false; player.isHit = true; player.DelayFrame = 40;
      shake.Observe(first, default, player, 12); Assert.IsTrue(shake.IsShaking, "A zero-damage confirmed hit still has impact.");
      shake.Advance(.03f, .22f, 6);
      Assert.AreNotEqual(origin, first.anchoredPosition);
      player.playerHp = 160; player.DelayFrame = 45;
      shake.Observe(first, default, player, 13); shake.Advance(.03f, .22f, 6);
      shake.Reset(); Assert.AreEqual(origin, first.anchoredPosition);
      shake.Observe(first, default, player, 13);
      player.playerHp = 150; shake.Observe(first, default, player, 14); shake.Advance(.03f, .22f, 6);
      player.playerHp = 170; player.isHit = false;
      shake.Observe(first, default, player, 15);
      Assert.IsFalse(shake.IsShaking); Assert.AreEqual(origin, first.anchoredPosition);
      player.playerHp = 160; shake.Observe(first, default, player, 16); shake.Advance(.03f, .22f, 6);
      shake.Observe(second, default, player, 16);
      Assert.IsFalse(shake.IsShaking); Assert.AreEqual(origin, first.anchoredPosition);
      Assert.AreEqual(Vector2.zero, second.anchoredPosition);
      player.playerHp = 150; shake.Observe(second, default, player, 17); shake.Advance(.03f, .22f, 6);
      shake.Observe(second, default, player, 9);
      Assert.IsFalse(shake.IsShaking); Assert.AreEqual(Vector2.zero, second.anchoredPosition);
    } finally {
      shake.Reset(); Object.DestroyImmediate(first.gameObject); Object.DestroyImmediate(second.gameObject);
    }
  }
  [Test]
  public void GameplaySceneHasExplicitUpdaterBindings() {
    var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/LSDFGameplay.unity");
    try {
      LSDFSceneBindings binding = null;
      foreach (var root in scene.GetRootGameObjects()) {
        var found = root.GetComponentsInChildren<LSDFSceneBindings>(true);
        if (found.Length > 0) binding = found[0];
      }
      Assert.IsNotNull(binding); Assert.IsNotNull(binding.Updater);
      Assert.IsNotNull(binding.Player1Camera); Assert.IsNotNull(binding.Player2Camera);
      Assert.IsNotNull(binding.Player1Hud.LeftHealth); Assert.IsNotNull(binding.Player1Hud.RightHealth);
      Assert.IsNotNull(binding.Player2Hud.LeftHealth); Assert.IsNotNull(binding.Player2Hud.RightHealth);
      Assert.AreSame(binding.Player1Hud.LeftHealth.rectTransform.parent, binding.Player1Hud.LeftHealthShakeRoot);
      Assert.AreSame(binding.Player1Hud.RightHealth.rectTransform.parent, binding.Player1Hud.RightHealthShakeRoot);
      Assert.AreSame(binding.Player2Hud.LeftHealth.rectTransform.parent, binding.Player2Hud.LeftHealthShakeRoot);
      Assert.AreSame(binding.Player2Hud.RightHealth.rectTransform.parent, binding.Player2Hud.RightHealthShakeRoot);
      Assert.AreNotSame(binding.Player1Hud.LeftHealthShakeRoot, binding.Player1Hud.RightHealthShakeRoot);
      Assert.AreNotSame(binding.Player2Hud.LeftHealthShakeRoot, binding.Player2Hud.RightHealthShakeRoot);
      Assert.IsNotNull(binding.Player1Hud.Round); Assert.IsNotNull(binding.Player2Hud.Fight);
    } finally { EditorSceneManager.ClosePreviewScene(scene); }
  }
}
