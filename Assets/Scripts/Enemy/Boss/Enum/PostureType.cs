using UnityEngine;

namespace BossEnemy.Enum
{
    public enum PostureType
    {
        None = 0,
        [InspectorName("立ち")] Standing = 1,
        [InspectorName("右ひざを付いた片膝立ち")] RightHalfKneel = 2,
        [InspectorName("左ひざを付いた片膝立ち")] LeftHalfKneel = 3,
        [InspectorName("倒れる")] SpreadEagled = 4
    }
}
