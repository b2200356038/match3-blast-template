using UnityEngine;

namespace Data {
    [CreateAssetMenu(fileName = "Level", menuName = "Dream Games/Level Data")]
    public class LevelData : ScriptableObject {
        public int levelNumber;
        public int gridWidth;
        public int gridHeight;
        public int moveCount;
        public string[] grid;
    }
}