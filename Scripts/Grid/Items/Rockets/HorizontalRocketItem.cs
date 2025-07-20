namespace Grid.Items.Rockets
{
    using System;
    using DG.Tweening;
    using UnityEngine;
    
    public class HorizontalRocketItem : RocketItem
    {
        protected override void Awake()
        {
            base.Awake();
            direction = RocketDirection.Horizontal;
        }
    }
}