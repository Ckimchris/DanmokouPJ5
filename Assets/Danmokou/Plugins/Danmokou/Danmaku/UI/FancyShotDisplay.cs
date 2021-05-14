﻿using System;
using Danmokou.Core;
using Danmokou.DMath;
using Danmokou.Graphics;
using Danmokou.Scriptables;
using Danmokou.Services;
using TMPro;
using UnityEngine;

namespace Danmokou.UI {
public class FancyShotDisplay : FancyDisplay {
    public TextMeshPro shotTitle = null!;
    public TextMeshPro shotParams = null!;
    public TextMeshPro shotDescription = null!;
    public SpriteRenderer[] difficultyStars = null!;
    public int showDefaultStars = 3;
    public float starActiveScale = 0.8f;
    public float starInactiveScale = 0.5f;

    public void SetShot(PlayerConfig p, ShotConfig s, Subshot sub) {
        shotTitle.fontSharedMaterial.SetMaterialOutline(p.uiColor);
        shotDescription.fontSharedMaterial.SetMaterialOutline(p.uiColor);
        var ss = s.GetSubshot(sub);
        ShowStars(difficultyStars, ShotRatingToInt(ss.shotDifficulty), p.uiColor);
        shotParams.text = ss.type;
        shotTitle.text = s.isMultiShot ? $"{LocalizedStrings.UI.shotsel_multi_prefix}{ss.Title}" : ss.Title.ValueOrEn;
        shotDescription.text = ss.description;
    }

    public int ShotRatingToInt(ShotConfig.StarRating r) => r switch {
        ShotConfig.StarRating.One => 1,
        ShotConfig.StarRating.Two => 2,
        ShotConfig.StarRating.Three => 3,
        ShotConfig.StarRating.Five => 5,
        _ => 0
    };

    private void ShowStars(SpriteRenderer[] stars, int num, Color c) {
        for (int ii = 0; ii < stars.Length; ++ii) {
            if (ii < num) {
                stars[ii].color = c;
                stars[ii].transform.localScale = new Vector3(starActiveScale, starActiveScale, starActiveScale);
            } else if (ii < showDefaultStars) {
                stars[ii].color = Color.gray;
                stars[ii].transform.localScale = new Vector3(starInactiveScale, starInactiveScale, starInactiveScale);
            } else {
                stars[ii].color = Color.clear;
            }
        }
    }

    private static Vector2 axis => new Vector2(0, -MainCamera.ScreenHeight);

    public void SetRelative(int thisIndex, int selectedIndex, bool first) {
        var myLoc = axis * (thisIndex - selectedIndex);
        var isSel = thisIndex == selectedIndex;
        var scale = isSel ? selectedScale : unselectedScale;
        canceller?.Cancel();
        canceller = new Cancellable();
        float time = 0.6f;
        if (first) {
            tr.localPosition = myLoc;
            tr.localScale = new Vector3(scale, scale, scale);
        } else {
            RunDroppableRIEnumerator(tr.GoTo(myLoc, time, M.EOutSine, canceller));
            RunDroppableRIEnumerator(tr.ScaleTo(scale, time, M.EOutSine, canceller));
        }
    }
}
}