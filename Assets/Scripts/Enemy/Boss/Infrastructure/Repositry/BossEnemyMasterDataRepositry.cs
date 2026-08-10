using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

using BossEnemy.Interface;
using BossEnemy.Character;


namespace BossEnemy.Infrastructure.Repository
{
    [CreateAssetMenu(fileName = "BossEnemyMasterDataRepository", menuName = "Repositry/BossEnemyMasterData")]
    public class BossEnemyEntityRepository : ScriptableObject, IBossEnemyEntityRepository
    {
        public void Init()
        {

        }

        public BossCharacterEntity GetData(int id)
        {
            return default;
        }
    }
}
