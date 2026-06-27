using UniRx;
using UnityEngine;
using System;

# region BossEnemy関連using
using BossEnemy.Data;
#endregion

namespace BossEnemy.Model.CoreLogic
{
    public class BossAttack
    {
        public event Action<BossEnemyAttackData> OnAttackStart;

        public event Action OnAttackFinish;

        public BossAttack(IPlayerInformationService playerInformationService)
        {
            _playerInformationService = playerInformationService;
        }

        public void AttackActionStart(BossEnemyAttackData attackData)
        {
            Debug.Log("攻撃開始");
            _currentAttackData = attackData;
            OnAttackStart?.Invoke(attackData);
        }

        public void Hit()
        {
            _playerInformationService.TakeDamage(_currentAttackData.Damage);
        }

        public void Finish()
        {
            Debug.Log("攻撃終了");
            OnAttackFinish?.Invoke();

        }

        private IPlayerInformationService _playerInformationService;

        private BossEnemyAttackData _currentAttackData;
    }

    #region 移動処理
    public class BossMove
    {
        public void SetBossEnemy(BossEnemyData bossEnemyData)
        {
            Debug.Log("Bossのデータが設定されました");
            _bossEnemyData = bossEnemyData;
        }

        public void MoveTargetPosition(Transform target, float chaseSpeed)
        {
            // speed * Time.deltaTime で「1フレームあたりの移動量」にする
            float oneFrameSpeed = chaseSpeed * Time.deltaTime;

            float savePosY = _bossEnemyData.Position.Value.y;
            Vector3 movePosition = Vector3.MoveTowards(_bossEnemyData.Position.Value, target.position, oneFrameSpeed);
            movePosition.y = savePosY;

            // 移動速度を割り出す
            float powerAdjustment = 1000; // 1000フレーム分の力の補正
            float velocityX = Mathf.Abs(_bossEnemyData.Position.Value.x - movePosition.x) * powerAdjustment;
            float velocityZ = Mathf.Abs(_bossEnemyData.Position.Value.z - movePosition.z) * powerAdjustment;

            // speed * Time.deltaTime で「1フレームあたりの移動量」にする
            _bossEnemyData.SetPosition(movePosition);

            // ターゲット方向へ向きを向かせる
            Vector3 direction = target.position - _bossEnemyData.Position.Value;
            direction.y = 0; // 高度差を無視して水平な向きにする

            if (direction != Vector3.zero)
            {
                Quaternion rotation = Quaternion.LookRotation(direction);
                _bossEnemyData.SetRotation(rotation);
            }

            // 移動速度設定
            Vector3 moveVelocity = new Vector3(velocityX, 0, velocityZ);
            _bossEnemyData.SetVelocity(moveVelocity);
        }

        public void MoveTargetPositionRightOnTime(Vector3 target, float time)
        {
            // 既に目標にいる場合は処理を終了する
            if (time <= 0f) return;

            // 残りの距離と方向（ベクトル）を計算
            Vector3 remainingDirection = target - _bossEnemyData.Position.Value;

            //「残りの距離 ÷ 残りの時間」で、今出すべき速度（秒速ベクトル）を逆算
            Vector3 requiredVelocity = remainingDirection / time;

            // 速度に1フレームの時間をかけて、このフレームの移動量を計算
            Vector3 frameMovement = requiredVelocity * Time.deltaTime;

            // 現在の座標に移動量を足した「到達すべき座標」を返す
            _bossEnemyData.SetPosition(_bossEnemyData.Position.Value + frameMovement);

            // ターゲット方向へ向きを向かせる
            Vector3 direction = target - _bossEnemyData.Position.Value;
            direction.y = 0; // 高度差を無視して水平な向きにする

            if (direction != Vector3.zero)
            {
                Quaternion rotation = Quaternion.LookRotation(direction);
                _bossEnemyData.SetRotation(rotation);
            }
        }

        public void LookAtTarget(Transform target, float lookSpeed, float finishAngleThreshold, out bool isLookingAtTarget)
        {
            isLookingAtTarget = false;

            if (target == null)
            {
                return;
            }

            // ターゲットへの方向ベクトルを計算（自身の位置からターゲットの位置を引く）
            Vector3 direction = target.position - _bossEnemyData.Position.Value;

            // 移動速度設定
            Vector3 moveVelocity = new Vector3(Mathf.Abs(direction.x), 0, Mathf.Abs(direction.z));
            _bossEnemyData.SetVelocity(moveVelocity);

            // 上下の傾き（Y成分）を無視して、水平方向の回転のみにする
            direction.y = 0f;

            // 方向ベクトルがゼロ（真上や全く同じ位置）でないかチェック
            if (direction.sqrMagnitude > 0.001f)
            {
                // 向きたい方向のクォータニオン（回転情報）を計算
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // 現在の回転から目標の回転へ、Time.deltaTimeをかけてゆっくり補間
                _bossEnemyData.SetRotation(Quaternion.Slerp(_bossEnemyData.Rotation.Value, targetRotation, lookSpeed * Time.deltaTime));
                float angleDiff = Quaternion.Angle(_bossEnemyData.Rotation.Value, targetRotation);

                // 角度の差がしきい値以下になったかチェック
                if (angleDiff <= finishAngleThreshold)
                {
                    isLookingAtTarget = true;
                    return;
                }
            }
        }

        public void StopMove()
        {
            _bossEnemyData.SetVelocity(Vector3.zero);
        }

        private BossEnemyData _bossEnemyData;
    }
    #endregion

    #region フェーズ切り替え処理
    [Serializable]
    public class PhaseChange
    {
        // Phaseの切り替え通知<現在のPhaseのBossData, 現在のPhase数>
        public event Action OnPhaseChanged;

        // 全てのPhase終了通知
        public event Action OnFinishAllPhase;

        public PhaseChange(BossEnemyMasterData bossEnemyDataHolder)
        {
            _bossEnemyMasterData = bossEnemyDataHolder;
        }

        public BossEnemyData CurrentPhaseBossData => _currentPhaseBossData;
        public int CurrentPhase => _currentPhase;

        /// <summary> 最初のPhaseを開始 </summary>
        public void StartFirstPhase()
        {
            if (_bossEnemyMasterData == null)
                Debug.LogError("Bossのデータがnullです");

            _currentPhase = 0;
            ChangeNextPhase();
            _isFirstChangePhase = false;
        }

        /// <summary> BossのPhaseを次のPhaseに移行する処理 </summary>
        public void ChangeNextPhase()
        {
            if(!_isFirstChangePhase) _currentPhase++;

            // 全Phase終了時の処理
            if (_bossEnemyMasterData.BossEnemyDatas.Length <= _currentPhase)
            {
                FinishAllPhase();
                return;
            }

            _disposables?.Dispose();
            _disposables = new();

            _currentPhaseBossData = _bossEnemyMasterData.GetData(_currentPhase);

            OnPhaseChanged?.Invoke();
        }

        public void FinishAllPhase()
        {
            _isFirstChangePhase = true;
            OnFinishAllPhase?.Invoke();
            _disposables?.Dispose();
        }

        private BossEnemyMasterData _bossEnemyMasterData = null;

        private BossEnemyData _currentPhaseBossData = null;

        private CompositeDisposable _disposables = new CompositeDisposable();

        private int _currentPhase = 0;
        private bool _isFirstChangePhase = true;
    }
    #endregion

    #region ダメージ計算処理
    public class Damage
    {
        public void Init(BossEnemyData bossEnemyData) 
        {
            _currentBossEnemyData = bossEnemyData;
            _isInit = true;
        }

        /// <summary> ダメージを受けた際に呼ばれるメソッド </summary>
        public void TakeDamage(DamageContext damageContext, PartsType hitPartsType, bool isHitArmor,
            ArmorAttachmentPoint attachmentPoints = ArmorAttachmentPoint.None)
        {
            int defense = 0;
            int damage = 0;

            if (_currentBossEnemyData == null) Debug.LogError("BossEnemyDataが設定されていません");

            if (!isHitArmor)
            {
                // 受けて個所によって防御力(肉質)を取得
                defense = DamageSystem.GetHitPartsDefense(hitPartsType, _currentBossEnemyData);

                // Damageを計算
                if (hitPartsType == PartsType.VitalPoint
                    || hitPartsType == PartsType.WeekPoint)
                {
                    // 弱点への攻撃ならPlayerもModeによるダメージの減増を行う
                    damage = DamageSystem.CalculateDamage(defense, damageContext, true, EnemyDefenceType.Flesh);
                }
                else damage = DamageSystem.CalculateDamage(defense, damageContext);

                Debug.Log("本体にダメージ！：" + damage);
                _currentBossEnemyData.TakeDamage(damage);
            }
            else
            {
                defense = DamageSystem.GetHitPartsArmorDefense(attachmentPoints, _currentBossEnemyData);

                damage = DamageSystem.CalculateDamage(defense, damageContext, true, EnemyDefenceType.Armor);

                switch (attachmentPoints)
                {
                    case ArmorAttachmentPoint.LeftArm:
                        _currentBossEnemyData.LeftArmArmer.Damage(damage);
                        break;
                    case ArmorAttachmentPoint.RightArm:
                        _currentBossEnemyData.RightArmArmer.Damage(damage);
                        break;
                    case ArmorAttachmentPoint.LeftLeg:
                        _currentBossEnemyData.LeftLegArmer.Damage(damage);
                        break;
                    case ArmorAttachmentPoint.RightLeg:
                        _currentBossEnemyData.RightLegArmer.Damage(damage);
                        break;
                }

                Debug.Log("鎧にダメージ！：" + damage);
            }
        }

        private BossEnemyData _currentBossEnemyData;
        private bool _isInit = false;
    }
    #endregion

    public class BossDead
    {

    }
}
