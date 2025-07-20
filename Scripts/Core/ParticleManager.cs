using UnityEngine;

namespace Core
{
    /// <summary>
    /// Manages particle effects throughout the game
    /// </summary>
    public class ParticleManager : MonoBehaviour
    {
        [Header("Particle Prefabs")]
        [SerializeField] private ParticleSystem redParticlePrefab;
        [SerializeField] private ParticleSystem greenParticlePrefab;
        [SerializeField] private ParticleSystem blueParticlePrefab;
        [SerializeField] private ParticleSystem yellowParticlePrefab;
        
        [Header("Obstacle Particles")]
        [SerializeField] private ParticleSystem boxParticlePrefab;
        [SerializeField] private ParticleSystem stoneParticlePrefab;
        [SerializeField] private ParticleSystem vaseParticlePrefab;
        
        [Header("Special Particles")]
        [SerializeField] private ParticleSystem rocketParticlePrefab;
        
        // Singleton pattern
        private static ParticleManager _instance;
        public static ParticleManager Instance 
        { 
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ParticleManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ParticleManager");
                        _instance = go.AddComponent<ParticleManager>();
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        public void PlayBurstEffect(Vector3 position, GridItemType itemType)
        {
            ParticleSystem prefabToUse = GetParticlePrefabByType(itemType);
            
            if (prefabToUse != null)
            {
                ParticleSystem particleInstance = Instantiate(prefabToUse, position, Quaternion.identity);
                float duration = particleInstance.main.duration + particleInstance.main.startLifetime.constantMax;
                Destroy(particleInstance.gameObject, duration);
            }
        }
        
        private ParticleSystem GetParticlePrefabByType(GridItemType itemType)
        {
            switch (itemType)
            {
                case GridItemType.RedCube:
                    return redParticlePrefab;
                case GridItemType.GreenCube:
                    return greenParticlePrefab;
                case GridItemType.BlueCube:
                    return blueParticlePrefab;
                case GridItemType.YellowCube:
                    return yellowParticlePrefab;
                case GridItemType.Box:
                    return boxParticlePrefab;
                case GridItemType.Stone:
                    return stoneParticlePrefab;
                case GridItemType.Vase:
                    return vaseParticlePrefab;
                case GridItemType.HorizontalRocket:
                case GridItemType.VerticalRocket:
                    return rocketParticlePrefab;
                default:
                    return redParticlePrefab; // Use red as default
            }
        }
    }
}