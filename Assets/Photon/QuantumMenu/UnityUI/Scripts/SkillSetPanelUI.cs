using Quantum.LSDF;
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
