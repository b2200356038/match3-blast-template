using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Manages the UI panel that displays level objectives and progress tracking
    /// </summary>
    public class GoalPanel : MonoBehaviour
    {
        /// <summary>
        /// Data structure to track obstacle counts in the UI
        /// </summary>
        [System.Serializable]
        public class ObstacleCounter
        {
            public GridItemType obstacleType;
            public GameObject counterObject;
            public Image obstacleIcon;
            public TextMeshProUGUI counterText;
            public int currentCount;
        }

        [Header("Obstacle Icons")] 
        [SerializeField] private Sprite boxIcon;
        [SerializeField] private Sprite stoneIcon;
        [SerializeField] private Sprite vaseIcon;
        [SerializeField] private Sprite defaultIcon;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject counterPrefab;
        [SerializeField] private GameObject horizontalRowPrefab;
        
        [Header("Layout Settings")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private Color completeColor = Color.green;
        [SerializeField] private int maxItemsPerRow = 2; // Maximum number of items per row

        
        private Dictionary<GridItemType, ObstacleCounter> activeCounters = new Dictionary<GridItemType, ObstacleCounter>();
        private List<GameObject> activeRows = new List<GameObject>();
        private VerticalLayoutGroup verticalLayout;

        private void Awake()
        {
            verticalLayout = contentParent.GetComponent<VerticalLayoutGroup>();
        }

        /// <summary>
        /// Initialize goal panel with counts of each obstacle type
        /// </summary>
        /// <param name="obstacles">Dictionary of obstacle types and their counts</param>
        public void InitializeGoals(Dictionary<GridItemType, int> obstacles)
        {
            ClearAll();
            
            // Create list of active obstacles with counts greater than zero
            List<KeyValuePair<GridItemType, int>> activeObstacles = new List<KeyValuePair<GridItemType, int>>();
            foreach (var obstacle in obstacles)
            {
                if (obstacle.Value > 0)
                {
                    activeObstacles.Add(new KeyValuePair<GridItemType, int>(obstacle.Key, obstacle.Value));
                }
            }
            
            // Calculate rows needed based on item count and max per row
            int itemCount = activeObstacles.Count;
            int rowCount = Mathf.CeilToInt((float)itemCount / maxItemsPerRow);
            
            // Create rows and populate with counter objects
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                GameObject rowObj = CreateRow();
                activeRows.Add(rowObj);
                
                int startIndex = rowIndex * maxItemsPerRow;
                int endIndex = Mathf.Min(startIndex + maxItemsPerRow, itemCount);
                int itemsInThisRow = endIndex - startIndex;
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    GridItemType obstacleType = activeObstacles[i].Key;
                    int count = activeObstacles[i].Value;
                    
                    GameObject counterObj = CreateCounter(obstacleType, count, rowObj.transform);
                    
                    if (counterObj != null)
                    {
                        LayoutElement le = counterObj.GetComponent<LayoutElement>();
                        if (le == null)
                        {
                            le = counterObj.AddComponent<LayoutElement>();
                        }
                        le.flexibleWidth = 1;
                        le.flexibleHeight = 1;
                        
                        // Get UI components
                        Image icon = counterObj.transform.Find("ObstacleIcon")?.GetComponent<Image>();
                        TextMeshProUGUI text = counterObj.transform.Find("CounterText")?.GetComponent<TextMeshProUGUI>();
                        
                        // Create counter tracking structure
                        ObstacleCounter counter = new ObstacleCounter
                        {
                            obstacleType = obstacleType,
                            counterObject = counterObj,
                            obstacleIcon = icon,
                            counterText = text,
                            currentCount = count
                        };
                        
                        activeCounters[obstacleType] = counter;
                    }
                }
            }
            
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
        }
        
        /// <summary>
        /// Create a horizontal row container for counter items
        /// </summary>
        private GameObject CreateRow()
        {
            GameObject rowObj = Instantiate(horizontalRowPrefab, contentParent);
            return rowObj;
        }
        
        /// <summary>
        /// Create a counter object for a specific obstacle type
        /// </summary>
        private GameObject CreateCounter(GridItemType obstacleType, int count, Transform parent)
        {
            GameObject counterObj = Instantiate(counterPrefab, parent);
            
            Image icon = counterObj.transform.Find("ObstacleIcon")?.GetComponent<Image>();
            TextMeshProUGUI text = counterObj.transform.Find("CounterText")?.GetComponent<TextMeshProUGUI>();
            Image checkmark = counterObj.transform.Find("Checkmark")?.GetComponent<Image>();
            
            if (icon == null || text == null)
            {
                Debug.LogError("Required components missing in counter prefab!");
                Destroy(counterObj);
                return null;
            }
            
            // Configure counter appearance
            SetObstacleIcon(icon, obstacleType);
            text.text = count.ToString();
            
            if (checkmark != null)
            {
                checkmark.gameObject.SetActive(false);
            }
            
            return counterObj;
        }
        
        /// <summary>
        /// Update the count for a specific obstacle type
        /// </summary>
        public void UpdateObstacleCount(GridItemType obstacleType, int remainingCount)
        {
            if (!activeCounters.TryGetValue(obstacleType, out ObstacleCounter counter))
            {
                Debug.LogWarning($"No active counter found for obstacle type: {obstacleType}");
                return;
            }

            if (counter.currentCount == remainingCount) return;
            
            counter.currentCount = remainingCount;
            counter.counterText.text = remainingCount.ToString();
            
            if (remainingCount <= 0)
            {
                counter.counterText.color = completeColor;
                
                Transform checkmarkTransform = counter.counterObject.transform.Find("Checkmark");
                if (checkmarkTransform != null)
                {
                    checkmarkTransform.gameObject.SetActive(true);
                }
            }
        }
        
        /// <summary>
        /// Check if all obstacles have been cleared
        /// </summary>
        /// <returns>True if all obstacle counts are zero</returns>
        public bool AreAllGoalsCompleted()
        {
            if (activeCounters.Count == 0) return false;
            
            foreach (var counter in activeCounters.Values)
            {
                if (counter.currentCount > 0)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Clear all counters and row objects
        /// </summary>
        private void ClearAll()
        {
            foreach (var counter in activeCounters.Values)
            {
                if (counter.counterObject != null)
                {
                    Destroy(counter.counterObject);
                }
            }
            activeCounters.Clear();
            
            foreach (var row in activeRows)
            {
                if (row != null)
                {
                    Destroy(row);
                }
            }
            activeRows.Clear();
        }
        
        /// <summary>
        /// Set appropriate icon sprite based on obstacle type
        /// </summary>
        private void SetObstacleIcon(Image icon, GridItemType obstacleType)
        {
            if (icon == null) return;
            
            switch (obstacleType)
            {
                case GridItemType.Box:
                    icon.sprite = boxIcon;
                    break;
                case GridItemType.Stone:
                    icon.sprite = stoneIcon;
                    break;
                case GridItemType.Vase:
                    icon.sprite = vaseIcon;
                    break;
                default:
                    icon.sprite = defaultIcon;
                    break;
            }
        }
    }
}