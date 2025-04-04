using Quantum;
using UnityEngine;

public class LSDF_ViewHandler : QuantumEntityViewComponent
{
    public GameObject PlayerViewPrefab;
    public GameObject EnemyViewPrefab;

    private bool _replaced = false;

    public override void OnUpdateView()
    {
        //if (_replaced)
        //    return;

        //if (PredictedFrame.TryGetPointer<PlayerLink>(EntityRef, out var playerLink))
        //{
        //    var localPlayer = QuantumRunner.Default.Game.Player;
        //    bool isMine = playerLink->PlayerRef == localPlayer;

        //    GameObject newView = GameObject.Instantiate(isMine ? PlayerRightViewPrefab : EnemyLeftViewPrefab);

        //    var entityView = GetComponent<QuantumEntityView>();
        //    var updater = GetComponentInParent<QuantumEntityViewUpdater>();

        //    // 연결 유지
        //    entityView.EntityRef = this.EntityRef;
        //    entityView.PredictedFrame = this.PredictedFrame;

        //    newView.GetComponent<QuantumEntityView>().EntityRef = this.EntityRef;
        //    newView.GetComponent<QuantumEntityView>().PredictedFrame = this.PredictedFrame;

        //    // 현재 View 파괴
        //    Destroy(gameObject);

        //    _replaced = true;
        //}
    }
}