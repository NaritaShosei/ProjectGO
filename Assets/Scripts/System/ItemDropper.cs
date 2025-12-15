using UnityEngine;

public class ItemDropper
{
    public void DropItem(in ItemDropData itemDropData, Vector3 dropPositionOnGround)
    {
        if (itemDropData.ItemPrefab == null)
        {
            Debug.LogWarning("アイテムのプレハブが設定されていません");
            return;
        }
        // ドロップ確率に基づいてアイテムをドロップするかどうかを決定
        int randomValue = Random.Range(0, 10000);
        if (randomValue < itemDropData.DropChance)
        {
            for (int i = 0; i < itemDropData.DropCount; i++)
            {
                // アイテムのインスタンスを生成
                GameObject itemInstance = UnityEngine.Object.Instantiate(itemDropData.ItemPrefab);
                // 必要に応じて、アイテムの初期化処理をここに追加
                if (itemInstance.TryGetComponent<Collider>(out var collider))
                {
                    // アイテムが地面に埋まらないように位置を調整
                    dropPositionOnGround.y += collider.bounds.extents.y;
                }
                else
                {
                    Debug.LogWarning("アイテムのプレハブにColliderコンポーネントが存在しません。位置調整が正しく行われない可能性があります。");
                }

                itemInstance.transform.position = dropPositionOnGround;
            }
        }
    }
}
