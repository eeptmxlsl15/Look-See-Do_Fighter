using System.Collections.Generic;
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
  private int _maximum;
  private readonly Dictionary<SkillButton, UnityAction> _listeners = new Dictionary<SkillButton, UnityAction>();
  private void OnEnable() {
    var rules = LSDFLocalLoadout.Rules;
    _maximum = rules != null && rules.MaximumLoadoutCost >= 0 ? rules.MaximumLoadoutCost : MaxCost;
    _selection = new LSDFLoadoutSelection(LSDFLocalLoadout.Read(rules), rules, _maximum);
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
    if (costText != null) costText.text = $"Cost : {_selection.Cost}/{_maximum}";
  }
  public void ResetRegistry() {
    LSDFLocalLoadout.Reset();
    if (isActiveAndEnabled) {
      var rules = LSDFLocalLoadout.Rules;
      _maximum = rules != null && rules.MaximumLoadoutCost >= 0 ? rules.MaximumLoadoutCost : MaxCost;
      _selection = new LSDFLoadoutSelection(LSDFLocalLoadout.Read(rules), rules, _maximum);
      Refresh();
    }
  }
}
