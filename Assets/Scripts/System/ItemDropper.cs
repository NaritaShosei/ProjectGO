using UnityEngine;

public class ItemDropper
{
    public void DropItem(ItemDropData itemDropData, Vector3 dropPosition)
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
                GameObject itemInstance = UnityEngine.Object.Instantiate(itemDropData.ItemPrefab, dropPosition, Quaternion.identity);
                // 必要に応じて、アイテムの初期化処理をここに追加
            }
        }
    }
}
