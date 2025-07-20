using Core;
namespace Grid.Items.Obstacles
{
    public class StoneItem : ObstacleItem
    {
        public override void DestroyItem()
        {
            ParticleManager.Instance.PlayBurstEffect(transform.position, ItemType);
            base.DestroyItem();
        }
    }
}
