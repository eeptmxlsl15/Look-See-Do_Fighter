"""Replace project-owned menu adapters; retain class/field names and Unity GUIDs."""
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
UI = ROOT/'Assets/Photon/QuantumMenu/UnityUI/Scripts'
(UI/'SkillSelectionManager.cs').write_text('''using System.Collections.Generic;
using Quantum.LSDF;
using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SkillSelectionManager : MonoBehaviour {
  public int MaxCost = 140;
  public Color selectedColor = Color.green;
  public Color defaultColor = Color.white;
  public TextMeshProUGUI costText;
  private LSDFLoadoutSelection _selection;
  private readonly Dictionary<SkillButton, UnityAction> _listeners = new Dictionary<SkillButton, UnityAction>();
  private void OnEnable() {
    var rules = LSDFLocalLoadout.Rules;
    _selection = new LSDFLoadoutSelection(LSDFLocalLoadout.Read(rules), rules, MaxCost);
    foreach (var button in GetComponentsInChildren<SkillButton>(true)) {
      button.UIButton = button.GetComponent<Button>();
      if (button.UIButton == null) continue;
      var captured = button;
      UnityAction action = () => Select(captured);
      _listeners.Add(button, action);
      button.UIButton.onClick.AddListener(action);
    }
    Refresh();
  }
  private void OnDisable() {
    foreach (var pair in _listeners)
      if (pair.Key != null && pair.Key.UIButton != null) pair.Key.UIButton.onClick.RemoveListener(pair.Value);
    _listeners.Clear();
    _selection = null;
  }
  private void Select(SkillButton button) {
    int slot = LSDFLoadout.Slot(button.Direction, button.ButtonType);
    if (_selection == null || !_selection.TrySelect(slot, button.SkillIndex)) return;
    LSDFLocalLoadout.Save(_selection.Snapshot());
    Refresh();
  }
  private void Refresh() {
    if (_selection == null) return;
    foreach (var pair in _listeners) {
      var button = pair.Key;
      var image = button.UIButton.GetComponent<Image>();
      int slot = LSDFLoadout.Slot(button.Direction, button.ButtonType);
      if (image != null) image.color = slot >= 0 && _selection.Skill(slot) == button.SkillIndex ? selectedColor : defaultColor;
    }
    if (costText != null) costText.text = $"Cost : {_selection.Cost}/{MaxCost}";
  }
  public void ResetRegistry() {
    LSDFLocalLoadout.Reset();
    if (isActiveAndEnabled) {
      _selection = new LSDFLoadoutSelection(LSDFLocalLoadout.Read(), LSDFLocalLoadout.Rules, MaxCost);
      Refresh();
    }
  }
}
''',encoding='utf-8')
(UI/'SkillSetPanelUI.cs').write_text('''using Quantum.LSDF;
using Quantum.Menu;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class SkillSelectionUI : MonoBehaviour {
  public CommandDirection parentIndex;
  public CommandButton Button;
  private UnityEngine.UI.Button[] _buttons;
  private UnityAction[] _listeners;
  private void OnEnable() {
    // The manager owns listeners and validation when present; never write a second selection.
    if (GetComponentInParent<SkillSelectionManager>() != null) return;
    _buttons = GetComponentsInChildren<UnityEngine.UI.Button>(true);
    _listeners = new UnityAction[_buttons.Length];
    for (int i = 0; i < _buttons.Length; i++) {
      int index = i;
      _listeners[i] = () => Select(index);
      _buttons[i].onClick.AddListener(_listeners[i]);
    }
  }
  private void OnDisable() {
    if (_buttons != null && _listeners != null)
      for (int i = 0; i < _buttons.Length; i++) if (_buttons[i] != null) _buttons[i].onClick.RemoveListener(_listeners[i]);
    _buttons = null; _listeners = null;
  }
  private void Select(int index) {
    int slot = LSDFLoadout.Slot(parentIndex, Button);
    var metadata = _buttons[index].GetComponent<SkillButton>();
    int skill = metadata != null ? metadata.SkillIndex : index;
    if (!LSDFLocalLoadout.TrySet(slot, skill)) return;
    for (int i = 0; i < _buttons.Length; i++) {
      var image = _buttons[i].GetComponent<Image>();
      if (image != null) image.color = i == index ? Color.green : Color.white;
    }
  }
}
''',encoding='utf-8')
(UI/'Legacy/SkillSaveManager.cs').write_text('''using Quantum.Menu;
using Quantum.LSDF;
public static class SkillSaveManager {
  public static void SaveSkillSelection(int index, int skillIndex) { LSDFLocalLoadout.TrySet(index, skillIndex); }
  public static int LoadSkillSelection(int index) { return LSDFLoadout.ReadSkill(LSDFLocalLoadout.Read(), index); }
}
''',encoding='utf-8')
print('Replaced menu adapters with one selection model/store and scoped listeners; unrelated PlayerPrefs are preserved.')
