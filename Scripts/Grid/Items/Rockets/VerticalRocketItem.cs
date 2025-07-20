namespace Grid.Items.Rockets
{
    using System;
    using DG.Tweening;
    using UnityEngine;

    public class VerticalRocketItem : RocketItem
    {
        
        protected override void Awake()
        {
            base.Awake();
            direction = RocketDirection.Vertical;
        }
    }
}