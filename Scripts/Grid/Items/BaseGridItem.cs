// Assets/Scripts/Grid/BaseGridItem.cs
using Core;
using DG.Tweening;
using UnityEngine;

namespace Grid.Items
{
    /// <summary>
    /// Base class for all grid items in the game
    /// Follows Single Responsibility Principle by handling common grid item behavior
    /// </summary>
    public abstract class BaseGridItem : MonoBehaviour
    {
        // Grid coordinates
        [SerializeField] protected int gridX;
        [SerializeField] protected int gridY;

        // Components
        protected SpriteRenderer spriteRenderer;
        protected BoxCollider2D boxCollider;
        protected GridManager gridManager;

        // Properties
        public GridItemType ItemType { get; protected set; }
        public int GridX => gridX;
        public int GridY => gridY;
        
        protected bool isMoving;
        public bool IsMoving => isMoving;
        
        // Disable state
        protected bool isDisabled = false;
        public bool IsDisabled => isDisabled;

        // Virtual properties to be overridden by subclasses
        public virtual bool IsClickable => !IsDisabled && !IsMoving && !IsObstacle;

        public bool IsObstacle =>
            ItemType == GridItemType.Box ||
            ItemType == GridItemType.Stone ||
            ItemType == GridItemType.Vase;

        protected virtual void Awake()
        {
            // Get or add required components
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();

            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.size = Vector2.one;
            }
        }

        /// <summary>
        /// Initializes the grid item with its type and position
        /// </summary>
        public virtual void Initialize(GridItemType type, int x, int y, GridManager manager)
        {
            ItemType = type;
            SetGridPosition(x, y);
        
            // Inject GridManager dependency
            this.gridManager = manager;
        }

        /// <summary>
        /// Sets the grid position of the item
        /// </summary>
        public void SetGridPosition(int x, int y)
        {
            gridX = x;
            gridY = y;
        }

        /// <summary>
        /// Sets the world position of the item
        /// </summary>
        public virtual void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// Sets whether the item is currently moving (falling)
        /// </summary>
        public virtual void SetIsMoving(bool value)
        {
            // Disabled items can't be set to moving
            if (isDisabled) return;
    
            isMoving = value;
    
            // Null-safe operations
            if (gridManager == null) return;
    
            if (value)
            {
                gridManager.AddFallingItem();
            }
            else
            {
                gridManager.RemoveFallingItem();
            }
        }

        /// <summary>
        /// Handles mouse click events
        /// </summary>
        protected virtual void OnMouseDown()
        {
            if (isDisabled || !IsClickable || isMoving)
            {
                PlayBounceRotationEffect();
                return;
            }

            HandleClick();
        }

        /// <summary>
        /// Processes click events through the GridManager
        /// </summary>
        protected virtual void HandleClick()
        {
            if (gridManager != null)
            {
                gridManager.OnGridItemClicked(this);
            }
        }

        /// <summary>
        /// Plays a bounce animation when the item cannot be clicked
        /// </summary>
        private void PlayBounceRotationEffect()
        {
            // Animation sequence
            Sequence bounceSequence = DOTween.Sequence();

            // First rotation to the left
            bounceSequence.Append(transform.DORotate(new Vector3(0, 0, 15f), 0.08f).SetEase(Ease.OutQuad));
            // Then rotation to the right
            bounceSequence.Append(transform.DORotate(new Vector3(0, 0, -8f), 0.08f).SetEase(Ease.OutQuad));
            // Then lighter rotation to the left
            bounceSequence.Append(transform.DORotate(new Vector3(0, 0, 4f), 0.08f).SetEase(Ease.OutQuad));
            // Another rotation to the right (extra turn)
            bounceSequence.Append(transform.DORotate(new Vector3(0, 0, -2f), 0.08f).SetEase(Ease.OutQuad));
            // Final light rotation to the left (extra turn)
            bounceSequence.Append(transform.DORotate(new Vector3(0, 0, 1f), 0.06f).SetEase(Ease.OutQuad));
            // Return to normal rotation
            bounceSequence.Append(transform.DORotate(Vector3.zero, 0.06f).SetEase(Ease.OutQuad));

            // Start the animation
            bounceSequence.Play();
        }

        /// <summary>
        /// Disables the item, preventing further interaction
        /// </summary>
        public virtual void DisableItem()
        {
            // If already disabled, don't do anything
            if (isDisabled) return;
            
            isDisabled = true;
            
            // Disable the collider to prevent interaction
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }
            
            // Cancel any ongoing animations or tweens
            DOTween.Kill(transform);
            
            // Ensure all child colliders are also disabled (if any)
            Collider2D[] childColliders = GetComponentsInChildren<Collider2D>();
            foreach (Collider2D collider in childColliders)
            {
                collider.enabled = false;
            }
            
            // Slightly reduce opacity to visually indicate disabled state
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(
                    spriteRenderer.color.r, 
                    spriteRenderer.color.g, 
                    spriteRenderer.color.b, 
                    0.8f
                );
            }
        }

        /// <summary>
        /// Destroys the item with animation
        /// </summary>
        public virtual void DestroyItem()
        {
            // First disable the item to prevent further interaction
            DisableItem();
            
            // Play particle effect and shrink animation
            ParticleManager.Instance.PlayBurstEffect(transform.position, ItemType);
            transform.DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    Destroy(gameObject);
                    
                });
        }
        protected virtual void OnDestroy()
        {
            DOTween.Kill(transform);
            if (spriteRenderer != null)
            {
                spriteRenderer.DOKill();
            }
            if (isMoving)
            {
                gridManager.RemoveFallingItem();
            }
        }
    }
}