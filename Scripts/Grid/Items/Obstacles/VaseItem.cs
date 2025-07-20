using Core;
using UnityEngine;

namespace Grid.Items.Obstacles
{
    public class VaseItem : ObstacleItem
    {
        [SerializeField] private Sprite crackedSprite;
        
        protected override void Awake()
        {
            base.Awake();
            maxHealth = 2;
        }
        public override bool TakeDamage(int amount)
        {
            bool isDamaged = base.TakeDamage(amount);
            
            if (currentHealth == 1)
            {
                ParticleManager.Instance.PlayBurstEffect(transform.position, ItemType);

                spriteRenderer.sprite = crackedSprite;
            }
            else if (currentHealth <= 0)
            {
                DestroyItem();
            }
            return isDamaged;
        }
    }
}