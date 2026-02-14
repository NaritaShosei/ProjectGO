using UnityEngine;

[CreateAssetMenu(fileName = "SkillSelectSequence", menuName = "GameData/Sequence/SkillSelectSequence")]

public class SkillSelectSequence : SequenceBase
{
    public override bool IsComplete(SequenceContext context)
    {
        return context.SkillSelected;
    }


    public override void OnSequenceStart(SequenceContext context)
    {
        context.SkillSelected = false;

        if (context.InputHandler != null)
        {
            context.InputHandler.EnableInput(false);
        }

        if (context.SkillSelectView == null || context.SkillManager == null)
        {
            context.SkillSelected = true;
            Debug.LogWarning("SkillSelectViewまたはSkillManagerがnullなので、スキル選択UIを表示できません");
            return;
        }

        // 既存のPresenterがあればイベント購読を解除
        _presenter?.Dispose();

        _presenter = new SkillSelectPresenter(context.SkillManager, context.SkillSelectView, context.Player);

        if (!_presenter.Open(context.SkillSelectCount))
        {
            // スキル候補がない場合は即座に選択完了とする
            context.SkillSelected = true;
            Debug.Log("スキル候補がないため、スキル選択をスキップします");
        }
    }

    public override void OnSequenceUpdate(SequenceContext context)
    {
        // 毎フレームの更新

        // フェーズ終了時にpresenterを破棄
        if (context.SkillSelected)
        {
            _presenter.Dispose();
            _presenter = null;
        }
    }

    private SkillSelectPresenter _presenter;
}
