using Core;
using UnityEngine;

namespace Grid.Items.Obstacles
{
    public class BoxItem : ObstacleItem
    {
        
        public override void DestroyItem()
        {
            ParticleManager.Instance.PlayBurstEffect(transform.position, ItemType);
            base.DestroyItem();
        }
    }
}