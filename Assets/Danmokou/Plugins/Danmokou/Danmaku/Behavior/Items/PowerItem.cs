﻿using System.Collections;
using System.Collections.Generic;
using Danmokou.Core;
using UnityEngine;

namespace Danmokou.Behavior.Items {
public class PowerItem : Item {
    protected override ItemType Type => ItemType.POWER;
    protected override short RenderOffsetIndex => 1;
    protected override float RotationTurns => 1;

    protected override void CollectMe() {
        GameManagement.Instance.AddPowerItems(1);
        base.CollectMe();
    }
}
}