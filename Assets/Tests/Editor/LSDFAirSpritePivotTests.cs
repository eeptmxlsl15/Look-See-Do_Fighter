using System.Linq;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEditor;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

public class LSDFAirSpritePivotTests {
  private static FP Delta => FP._1 / 60;

  private static ObjectReferenceKeyframe[] AirFrames(bool enemy) {
    string path = enemy
      ? "Assets/Sprites/Character/Enemy/Move_Enemy/Air_Enemy/Air_Enemy.anim"
      : "Assets/Sprites/Character/Player/Move/Air/Air.anim";
    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    Assert.IsNotNull(clip);
    var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
      .Single(value => value.type == typeof(SpriteRenderer) && value.propertyName == "m_Sprite");
    var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
    Assert.AreEqual(60, keys.Length);
    return keys;
  }

  [TestCase(false)] [TestCase(true)]
  public void ImportedAirFramesKeepOneCanvasAnchorIncludingTheLoopBoundary(bool enemy) {
    var frames = AirFrames(enemy);
    var first = (Sprite)frames[0].value;
    Assert.IsNotNull(first);
    Vector2 anchor = first.pivot / first.pixelsPerUnit;
    foreach (var key in frames) {
      var sprite = key.value as Sprite;
      Assert.IsNotNull(sprite);
      Assert.AreEqual(first.rect.size, sprite.rect.size, sprite.name);
      Assert.That(Vector2.Distance(anchor, sprite.pivot / sprite.pixelsPerUnit),
        Is.LessThan(0.00001f), "Changing the canvas anchor moves the picture independently of the fighter: " + sprite.name);
    }
  }

  private static void Step(ref LSDF_AirCombo air, ref PhysicsBody2D body, ref FP y,
      int tick, LSDFJuggleSettings config) {
    body.Velocity.Y -= 10 * body.GravityScale * Delta;
    body.Velocity.Y *= FP._1 - body.Drag * Delta;
    y += body.Velocity.Y * Delta;
    LSDFJuggle.Step(ref air, ref body, y, tick, Delta, config);
  }

  [TestCase(1, false)] [TestCase(2, false)] [TestCase(3, false)] [TestCase(4, false)]
  [TestCase(1, true)] [TestCase(2, true)] [TestCase(3, true)] [TestCase(4, true)]
  public void StoppingBeforeFiveHitsNeverMovesTheSpriteCanvasUpWhenAirLoops(int hits, bool enemy) {
    var frames = AirFrames(enemy);
    var config = new LSDFJuggleSettings();
    var body = new PhysicsBody2D { GravityScale = FP._1, Drag = FP._0_50 };
    var air = new LSDF_AirCombo();
    FP y = 0;
    LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false, config, 0, Delta, starter: true);
    int tick = 0;
    for (int hit = 1; hit <= hits; hit++) {
      for (int step = 0; step < 30; step++) Step(ref air, ref body, ref y, ++tick, config);
      Assert.IsFalse(LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false, config, tick, Delta));
    }
    Assert.AreEqual(hits, air.HitCount);
    int lastHitTick = tick, loopCrossings = 0;
    float previousCanvasY = 0;
    bool descendingBefore = false, landed = false;
    for (int elapsed = 1; elapsed <= 300; elapsed++) {
      Step(ref air, ref body, ref y, ++tick, config);
      var sprite = (Sprite)frames[(tick - lastHitTick) % frames.Length].value;
      float canvasY = y.AsFloat - sprite.pivot.y / sprite.pixelsPerUnit;
      bool descending = air.Phase == LSDFAirComboPhase.Returning || air.Phase == LSDFAirComboPhase.SlowFalling;
      if (descending && descendingBefore) {
        Assert.That(canvasY, Is.LessThanOrEqualTo(previousCanvasY + 0.0001f),
          "The sprite anchor jumped upwards during an uninterrupted fall: " + sprite.name);
        if (elapsed % frames.Length == 0) loopCrossings++;
      }
      previousCanvasY = canvasY; descendingBefore = descending;
      if (descending && y <= air.BaseY) { landed = true; break; }
    }
    Assert.IsTrue(landed);
    Assert.Greater(loopCrossings, 0, "The fixture must cross the failing 60 -> 1 sprite boundary before landing.");
  }
}
