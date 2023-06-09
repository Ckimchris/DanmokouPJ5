﻿using System;
using BagoumLib.Culture;
using Danmokou.Core;
using Danmokou.Services;
using Suzunoya.Entities;
using SuzunoyaUnity.Derived;
using SuzunoyaUnity.Mimics;
using UnityEngine;

namespace Danmokou.VN {
public class Yukari : SZYUCharacter {
    public static System.Numerics.Vector3 SpeakIconOffset => new(1.2f, 2.5f, 0);
    public override Color TextColor => new(0.94f, 0.89f, 1f);
    public override Color UIColor => new(0.70f, 0.14f, 0.77f);
    public override LString Name { get; set; } = LocalizedStrings.FindReference("dialogue.yukari");
    
    public override void RollEvent() => ISFXService.SFXService.Request("x-bubble-3", SFXType.TypingSound);
}

public class YukariMimic : PiecewiseCharacterMimic {
    public override Type[] CoreTypes => new[] {typeof(Yukari)};
}

}