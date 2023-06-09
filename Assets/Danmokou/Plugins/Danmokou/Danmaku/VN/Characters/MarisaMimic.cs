﻿using System;
using BagoumLib.Culture;
using Danmokou.Core;
using Danmokou.Services;
using Suzunoya.Entities;
using SuzunoyaUnity.Derived;
using SuzunoyaUnity.Mimics;
using UnityEngine;

namespace Danmokou.VN {
public class Marisa : SZYUCharacter {
    public static System.Numerics.Vector3 SpeakIconOffset => new(1, 1.7f, 0);
    public override Color TextColor => new(.97f, .93f, .82f);
    public override Color UIColor => new(1, .77f, .26f);
    public override LString Name { get; set; } = LocalizedStrings.FindReference("dialogue.marisa");
    
    public override void RollEvent() => ISFXService.SFXService.Request("x-bubble-2", SFXType.TypingSound);
}

public class MarisaMimic : PiecewiseCharacterMimic {
    public override Type[] CoreTypes => new[] {typeof(Marisa)};
}

}