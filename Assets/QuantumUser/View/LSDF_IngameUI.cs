using Quantum;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LSDF_IngameUI : QuantumEntityViewComponent
{
    public Image LeftHpGage;
    public Image RightHpGage;

    private EntityRef myPlayerEntity;
    private EntityRef opponentEntity;
    private bool initialized = false;

    public override void OnUpdateView()
    {
        var game = QuantumRunner.Default.Game;
        var frame = game.Frames?.Predicted;

        if (frame == null) return;

        PlayerRef myRef = game.GetLocalPlayers().FirstOrDefault();

        // �÷��̾�� ��� ��ƼƼ �� ���� ã��
        if (!initialized)
        {
            foreach (var pair in frame.GetComponentIterator<PlayerLink>())
            {
                var entity = pair.Entity;
                var link = frame.Get<PlayerLink>(entity);

                if (link.PlayerRef == myRef)
                {
                    myPlayerEntity = entity;

                }
                else
                {
                    opponentEntity = entity;

                }
            }

            if (myPlayerEntity.IsValid && opponentEntity.IsValid)
            {
                initialized = true;

            }

        }

        // �� ü��
        if (frame.TryGet<LSDF_Player>(myPlayerEntity, out var myPlayer))
        {
            float ratio = myPlayer.playerHp / 170f;
            LeftHpGage.fillAmount = ratio;

        }

        // ��� ü��
        if (frame.TryGet<LSDF_Player>(opponentEntity, out var oppPlayer))
        {
            float ratio = oppPlayer.playerHp / 170f;
            RightHpGage.fillAmount = ratio;

        }
    }
}
