"""One-shot, explicit migration of project-owned structural seams; no SDK or Generated edits."""
import json
import re
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SIM = ROOT / 'Assets/QuantumUser/Simulation'

def read(path): return path.read_bytes().decode('latin1')
def save(path, value): path.write_bytes(value.encode('latin1'))

main_path = SIM/'LSDF_PlayerSystem.cs'
source = read(main_path)
assert 'private void UpdateMovement(' in source, 'Already migrated'
start = source.index('        private void UpdateMovement(')
end = source.index('        #region ', start)
motion = source[start:end]
motion = motion.replace('private void UpdateMovement', 'public static void UpdateMovement')
motion = motion.replace('private void CollisionControll', 'public static void UpdateCollider')
motion = motion.replace('private void DetectDashCommand', 'public static void DetectDashCommand')
motion = motion.replace('ref Filter filter', 'ref LSDF_PlayerSystem.Filter filter')
motion = motion.replace('f.Unsafe.TryGetPointer<PhysicsCollider2D>(filter.Entity, out var collider);',
                        'if (!f.Unsafe.TryGetPointer<PhysicsCollider2D>(filter.Entity, out var collider)) return;')
motion = motion.replace('var buffer = f.Unsafe.GetPointer<DashInputBuffer>(filter.Entity);',
                        'if (!f.Unsafe.TryGetPointer<DashInputBuffer>(filter.Entity, out var buffer)) return;')
motion = re.sub(r'(?m)^\s*FP walkSpeed = FP._0_50; ;\r?\n', '', motion)
motion = re.sub(r'(?m)^\s*int flip = playerLink.PlayerRef == \(PlayerRef\)0 \? 1 : -1;\r?\n', '', motion)
motion = re.sub(r'(?m)^[ \t]*//[^\r\n]*(?:\r?\n|$)', '', motion)
motion = re.sub(r'(?m)[ \t]+(?=\r?$)', '', motion)
motion = re.sub(r'(\r?\n){3,}', '\r\n\r\n', motion)
save(SIM/'LSDFPlayerMotion.cs', 'using Photon.Deterministic;\r\nusing UnityEngine;\r\nnamespace Quantum.LSDF {\r\n  public static unsafe class LSDFPlayerMotion {\r\n' + motion + '  }\r\n}\r\n')
source = source[:start] + source[end:]
source = source.replace('CollisionControll(f, ref filter);', 'LSDFPlayerMotion.UpdateCollider(f, ref filter);')
source = source.replace('DetectDashCommand(f, ref filter, input);', 'LSDFPlayerMotion.DetectDashCommand(f, ref filter, input);')
source = source.replace('UpdateMovement(f, ref filter, input);', 'LSDFPlayerMotion.UpdateMovement(f, ref filter, input);')
source = source.replace('f.Unsafe.TryGetPointer<LSDF_Player>(filter.Entity, out var player);', 'var player = filter.LSDF_Player;')
source = source.replace('Input* input = default;', 'Input neutralInput = default;\r\n            Input* input = &neutralInput;')
source = source.replace('input = f.GetPlayerInput(playerLink->PlayerRef);', 'var polledInput = f.GetPlayerInput(playerLink->PlayerRef);\r\n                if (polledInput != null) input = polledInput;')
source = source.replace('LSDFPlayerHealth.UpdateRecovery(ref *player);', 'LSDFPlayerHealth.UpdateRecovery(ref *player, LSDFBalance.MaximumHealth(f), LSDFBalance.Healing(f));')
source = source.replace('tickToEnableMove-f.Number == 150', 'tickToEnableMove-f.Number == LSDFBalance.HealingLead(f)')
source = source.replace('            bool shouldAttack = true;\r\n', '')
save(main_path, source)

save(SIM/'LSDF_GameConfig.cs', '''using Photon.Deterministic;
using UnityEngine;
namespace Quantum.LSDF {
  public class LSDF_GameConfig : AssetObject {
    public int MaximumHealth = 170;
    public int HealingPerTick = 2;
    public int RoundLockTicks = 210;
    public int HealingLeadTicks = 150;
    public int RoundSeconds = 60;
    public FP WalkSpeed = FP._0_50;
    public FP DashSpeed = 2;
    public FP SpawnOffset = FP._0_50;
    public int MaximumLoadoutCost = 140;
    public int[] SkillCosts = DefaultCosts();
    // Retain the serialized legacy field for asset compatibility; not a fighter setting.
    [HideInInspector] public FP AsteroidInitialSpeed = 8;
    private static int[] DefaultCosts() {
      var values = new int[LSDFLoadout.SlotCount * LSDFLoadout.SkillsPerSlot];
      for (int slot = 0; slot < LSDFLoadout.SlotCount; slot++)
        for (int skill = 0; skill < LSDFLoadout.SkillsPerSlot; skill++)
          values[slot * LSDFLoadout.SkillsPerSlot + skill] = LSDFLoadout.DefaultCost(slot, skill);
      return values;
    }
  }
}
''')
asset = ROOT/'Assets/QuantumUser/Resources/LSDF_GameConfig.asset'
yaml = asset.read_text(encoding='utf-8').rstrip() + '\n'
yaml += '  MaximumHealth: 170\n  HealingPerTick: 2\n  RoundLockTicks: 210\n  HealingLeadTicks: 150\n  RoundSeconds: 60\n'
yaml += '  WalkSpeed:\n    RawValue: 32768\n  DashSpeed:\n    RawValue: 131072\n  SpawnOffset:\n    RawValue: 32768\n  MaximumLoadoutCost: 140\n  SkillCosts:\n'
costs = {0:[5,0,0],4:[6,7,10],8:[8]*3,12:[5]*3,16:[5]*3,20:[8]*3,24:[10]*3}
for slot in range(28):
    for skill in range(3): yaml += '  - ' + str(costs.get(slot,[0,0,0])[skill]) + '\n'
asset.write_text(yaml, encoding='utf-8')
menu_asset = ROOT/'Assets/QuantumUser/Resources/LSDF_MenuConfig.asset'
menu_asset.write_text(menu_asset.read_text(encoding='utf-8').replace('_maxPlayers: 6','_maxPlayers: 2'),encoding='utf-8')

spawn_path = SIM/'LSDF_PlayerSpawnSystem.cs'; spawn = read(spawn_path)
spawn = spawn.replace('new FPVector2(-FP._0_50, 0) : new FPVector2(FP._0_50, 0)', 'new FPVector2(-LSDFBalance.SpawnOffset(f), 0) : new FPVector2(LSDFBalance.SpawnOffset(f), 0)')
spawn = spawn.replace('for (int i = 0; i < LSDFLoadout.SlotCount; i++)\r\n            {', 'var validatedSkills = LSDFLoadout.CopyBudgetValidated(data.CommandSkills, LSDFBalance.Config(f));\r\n            for (int i = 0; i < LSDFLoadout.SlotCount; i++)\r\n            {')
spawn = spawn.replace('LSDFLoadout.ReadSkill(data.CommandSkills, i)', 'validatedSkills[i]')
spawn = spawn.replace('LSDF_player->playerHp = LSDFPlayerHealth.Maximum;', 'LSDF_player->playerHp = LSDFBalance.MaximumHealth(f);')
spawn = spawn.replace('f.Global->Time = 60;', 'f.Global->Time = LSDFBalance.RoundSeconds(f);')
save(spawn_path, spawn)

command_path = SIM/'LSDF_CommandSystem.cs'; command = read(command_path)
command = command.replace('((int)direction * 4) + (int)button', 'LSDFLoadout.Slot(direction, button)')
command = command.replace('int skillId = player->CommandSkillMap[index];', 'if (index < 0) return;\r\n            int skillId = player->CommandSkillMap[index];')
command = command.replace('var input = f.GetPlayerInput(playerLink->PlayerRef);', 'var input = f.GetPlayerInput(playerLink->PlayerRef);\r\n            if (input == null) return;')
save(command_path, command)

for name, old, new in [('MoveFront','FP._0_50 * flip','Quantum.LSDF.LSDFBalance.WalkSpeed(f) * flip'),
                       ('MoveBack','-FP._0_50 * flip','-Quantum.LSDF.LSDFBalance.WalkSpeed(f) * flip'),
                       ('DashFront','2 * flip','Quantum.LSDF.LSDFBalance.DashSpeed(f) * flip'),
                       ('DashBack','-2 * flip','-Quantum.LSDF.LSDFBalance.DashSpeed(f) * flip')]:
    path = SIM/'LSDF_Animator_Window_Event/Move'/f'{name}WindowEvent.cs'; text = read(path)
    assert old in text, path
    save(path,text.replace(old,new))

# A missing optional component is ignored rather than dereferenced. Filters/valid prototypes keep their original path.
for path in [spawn_path, SIM/'LSDF_Animator_Window_Event/Move'/ 'AirWindowEvent.cs', * (SIM/'LSDF_Animator_Window_Event/Move').glob('*.cs')]:
    text = read(path)
    text = re.sub(r'(?m)^([ \t]*)(f\.Unsafe\.TryGetPointer<[^>]+>\([^;\r\n]+\));', r'\1if (!\2) return;',text)
    save(path,text)

save(SIM/'LSDF_IngameSystem.cs','''using UnityEngine.Scripting;
namespace Quantum.LSDF {
  [Preserve]
  public unsafe class LSDF_IngameSystem : SystemMainThread {
    public override void Update(Frame f) {
      if (!LSDFRoster.TryPair(f, out var first, out var second)) return;
      if (!f.Unsafe.TryGetPointer<LSDF_Player>(first, out var a) || !f.Unsafe.TryGetPointer<LSDF_Player>(second, out var b)) return;
      if (LSDFRoundPolicy.Phase(true, *a, *b, f.Number) == LSDFRoundPhase.Recovering) return;
      a->isRoundEnd = false;
      b->isRoundEnd = false;
      if (!LSDFRoundPolicy.IsKnockedOut(*a) && !LSDFRoundPolicy.IsKnockedOut(*b)) return;
      LSDFRoundPolicy.BeginRecovery(ref *a, f.Number, LSDFBalance.RoundLock(f));
      LSDFRoundPolicy.BeginRecovery(ref *b, f.Number, LSDFBalance.RoundLock(f));
      if (f.Unsafe.TryGetPointer<PhysicsBody2D>(first, out var firstBody)) firstBody->Velocity.X = 0;
      if (f.Unsafe.TryGetPointer<PhysicsBody2D>(second, out var secondBody)) secondBody->Velocity.X = 0;
    }
  }
}
''')

save(SIM/'LSDF_CollisionSystem.cs','''using UnityEngine.Scripting;
namespace Quantum.LSDF {
  [Preserve]
  public unsafe class LSDF_CollisionSystem : SystemSignalsOnly, ISignalOnTriggerEnter2D, ISignalOnCollisionEnter2D, ISignalOnCollisionExit2D {
    private static bool TryFighter(Frame f, EntityRef entity, out LSDF_Player* player, out AnimatorComponent* animator) {
      animator = null;
      return f.Unsafe.TryGetPointer<LSDF_Player>(entity, out player) && f.Unsafe.TryGetPointer<AnimatorComponent>(entity, out animator);
    }
    public void OnCollisionEnter2D(Frame f, CollisionInfo2D info) {
      if (!TryFighter(f, info.Other, out var player, out var animator)) return;
      if (f.Unsafe.TryGetPointer<LSDF_Wall>(info.Entity, out var wall)) {
        f.Signals.OnCollisionEnterWall(info, player, animator, wall);
        if (player->isAir || player->hitWallLauncher) f.Signals.OnCollisionWallHitEnter(info, player, animator, wall);
      }
      if (f.Unsafe.TryGetPointer<LSDF_Ground>(info.Entity, out var ground)) f.Signals.OnCollisionGroundEnter(info, player, animator, ground);
    }
    public void OnCollisionExit2D(Frame f, ExitInfo2D info) {
      if (!TryFighter(f, info.Other, out var player, out var animator)) return;
      if (f.Unsafe.TryGetPointer<LSDF_Ground>(info.Entity, out var ground)) f.Signals.OnCollisionGroundExit(info, player, animator, ground);
      if (f.Unsafe.TryGetPointer<LSDF_Wall>(info.Entity, out var wall)) f.Signals.OnCollisionExitWall(info, player, animator, wall);
    }
    public void OnTriggerEnter2D(Frame f, TriggerInfo2D info) {
      if (!TryFighter(f, info.Entity, out var defender, out var animator)) return;
      if (!f.Unsafe.TryGetPointer<LSDF_HitboxInfo>(info.Other, out var hit)) return;
      if (hit->AttackerEntity == info.Entity || !animator->AnimatorGraph.Id.IsValid) return;
      if (animator->Layers.Equals(default(Quantum.Collections.QListPtr<LayerData>))) return;
      var graph = f.FindAsset<AnimatorGraph>(animator->AnimatorGraph);
      var layers = f.ResolveList(animator->Layers);
      if (graph == null || layers.Count == 0) return;
      var state = graph.GetState(layers.GetPointer(0)->CurrentStateId);
      if (state == null) return;
      var reaction = LSDFContactPolicy.Evaluate(*defender, *hit, state.Name);
      switch (reaction) {
        case LSDFContactReaction.Normal: f.Signals.OnTriggerNormalHit(info, defender, animator, hit); break;
        case LSDFContactReaction.Counter: f.Signals.OnTriggerCounterHit(info, defender, animator, hit); break;
        case LSDFContactReaction.StunHit:
          f.Signals.OnTriggerNormalHit(info, defender, animator, hit); defender->isStun = false; break;
        case LSDFContactReaction.StunGuard:
          f.Signals.OnTriggerGuard(info, defender, animator, hit); defender->isStun = false; break;
        case LSDFContactReaction.StunIgnore: defender->isStun = false; break;
        case LSDFContactReaction.ComboHit:
          f.Signals.OnTriggerNormalHit(info, defender, animator, hit); defender->isCombo = false; break;
        case LSDFContactReaction.Guard:
          f.Signals.OnTriggerGuard(info, defender, animator, hit);
          if (hit->DelayGuardTpye != DelayGuardType.Normal && TryFighter(f, hit->AttackerEntity, out var guardedAttacker, out var guardedAnimator))
            f.Signals.OnTriggerEnemyGuard(info, guardedAttacker, guardedAnimator, hit);
          break;
        case LSDFContactReaction.ParryAttacker:
          if (TryFighter(f, hit->AttackerEntity, out var parriedAttacker, out var parriedAnimator))
            f.Signals.OnTriggerEnemyParring(info, parriedAttacker, parriedAnimator, hit);
          break;
      }
    }
  }
}
''')

# Keep GUIDs and type names for empty compatibility components; their assembly intentionally becomes LSDF.View.
legacy = ROOT/'Assets/QuantumUser/View/Legacy'; legacy.mkdir(exist_ok=True)
for name in ['LSDF_AnimatorView','LSDF_GameUI','LSDF_DashSystem','LSDF_TimerSystem']:
    for suffix in ['.cs','.cs.meta']:
        old = SIM/(name+suffix); target = legacy/(name+suffix)
        assert old.resolve().is_relative_to(SIM.resolve()) and target.resolve().is_relative_to((ROOT/'Assets/QuantumUser/View').resolve())
        assert not target.exists(), target
        old.rename(target)
baseline_path = ROOT/'tools/ai/quantum-baseline.json'
baseline = json.loads(baseline_path.read_text(encoding='utf-8'))
baseline['entries'] = {}
baseline['note'] = '2026-10-16 remaining refactor: all five retired legacy findings removed; no allowances added.'
baseline_path.write_text(json.dumps(baseline,indent=2)+'\n',encoding='utf-8')

# Minimal metas avoid changing import defaults; GUIDs are stable for these newly authored paths.
for directory in [SIM, ROOT/'Assets/Tests/Editor', ROOT/'Assets/QuantumUser/View']:
    for path in directory.glob('*.cs'):
        meta = path.with_suffix('.cs.meta')
        if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,str(path.relative_to(ROOT))).hex+'\n',encoding='ascii')
legacy_meta = legacy.with_suffix('.meta')
if not legacy_meta.exists(): legacy_meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'LSDF/View/Legacy').hex+'\nfolderAsset: yes\n',encoding='ascii')
print('Migrated movement, balance, collision/roster/round seams and four GUID-preserved compatibility components.')
