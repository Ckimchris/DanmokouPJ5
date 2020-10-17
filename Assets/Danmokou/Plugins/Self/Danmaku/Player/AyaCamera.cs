﻿using System;
using System.Collections;
using System.IO;
using DMath;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SM;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Collision = DMath.Collision;
using Object = UnityEngine.Object;

namespace Danmaku {
public class AyaCamera : BehaviorEntity {

    public enum State {
        NORMAL = 0,
        CHARGE = 1,
        FIRING = 2,
        REFRACTORY = 3
    }

    public enum Orientation {
        HORIZONTAL = 0,
        VERTICAL = 1
    }

    private static Orientation Reverse(Orientation o) => 
        o == Orientation.HORIZONTAL
        ? Orientation.VERTICAL
        : Orientation.HORIZONTAL;

    public static State CameraState { get; private set; } = State.NORMAL;
    public static Orientation CameraOrientation { get; private set; } = Orientation.HORIZONTAL;
    private float CameraOrientationAngleOffset => (CameraOrientation == Orientation.HORIZONTAL) ? 0f : 90f;

    public float CameraSpeedMultiplier =>
        CameraState == State.FIRING ? 0f :
        (CameraState == State.CHARGE && player.IsFocus) ? 
            0.5f :
            1f;
    
    private PlayerInput player;
    public Transform viewfinder;
    private SpriteRenderer viewfinderSR;
    public SpriteRenderer flash;
    public TextMeshPro text;
    public Color textUnfilledColor;
    public Color textFilledColor;
    private Color TextColor => (ChargeFull || CameraState == State.FIRING) ? textFilledColor : textUnfilledColor;
    public float viewfinderRadius;
    public SFXConfig onOrientationSwitch;
    public SFXConfig whileCharge;
    public SFXConfig onFullCharge;
    public SFXConfig whileFire;
    public SFXConfig onFlash;
    public SFXConfig onPictureSuccess;
    public SFXConfig onPictureMiss;
    public SFXConfig onTimeout;
    public GameObject pinnedPhotoPrefab;

    private const string textFormat = "<mspace=2.4>{0:F0}%</mspace>";
    
    private float angle;
    private Vector2 location;
    private static Vector2 AimAt => (GameManagement.campaign.ExecutingBoss == null) ?
        new Vector2(0f, 5f) :
        GameManagement.campaign.ExecutingBoss.rBPI.loc;
    private float BoundedViewfinderRadius => Mathf.Min(viewfinderRadius, (AimAt - player.hitbox.location).magnitude);
    private float AngleToTarget =>
        (player.IsMoving && !player.IsFocus) 
            ? M.AtanD(player.LastDelta)
            : M.AtanD(AimAt - player.hitbox.location);
    private float BaseViewfinderAngle => AngleToTarget - 90;
    private Vector2 TargetPosition => player.hitbox.location + BoundedViewfinderRadius * M.CosSinDeg(AngleToTarget);

    private const float lerpToAngleRate = 4f;
    private const float lerpToPositionRate = 6f;

    private float orientationSwitchWaiting = -1f;
    private const float orientationSwitchCooldown = 0.2f;

    public Vector2 cameraHalfBounds;
    private Vector2 CameraHalfBounds => CameraOrientation == Orientation.HORIZONTAL ?
        cameraHalfBounds :
        new Vector2(cameraHalfBounds.y, cameraHalfBounds.x);
    public float cameraFireSize;
    public float cameraLerpDownTime;
    private const float slowdownRatio = 0.5f;
    public float cameraFireControlSpeed;
    public float freezeTime = 1f;
    public float flashTime = 0.3f;

    private int lowCameraLayer;
    private int highCameraLayer;

    private static void ResetCameraState() {
        CameraState = State.NORMAL;
    }
    protected override void Awake() {
        base.Awake();
        ResetCameraState();
        RunDroppableRIEnumerator(UpdateNormal());
        lowCameraLayer = LayerMask.NameToLayer("LowEffects");
        highCameraLayer = LayerMask.NameToLayer("TransparentFX");
        viewfinderSR = viewfinder.GetComponent<SpriteRenderer>();
        flash.enabled = false;
    }

    public void Initialize(PlayerInput playr) {
        player = playr;
        angle = BaseViewfinderAngle;
        location = TargetPosition;
        tr.position = new Vector2(0, -100); //hide it offscreen for the first frame
    }

    protected override void RegularUpdateMove() {
        orientationSwitchWaiting -= ETime.FRAME_TIME;
        if (player.IsTryingBomb && orientationSwitchWaiting < 0f) {
            orientationSwitchWaiting = orientationSwitchCooldown;
            CameraOrientation = Reverse(CameraOrientation);
            SFXService.Request(onOrientationSwitch);
        }
        //while firing, the angle is static and the position is controlled by the coroutine
        if (CameraState != State.FIRING) {
            angle = Mathf.Lerp(angle, BaseViewfinderAngle, lerpToAngleRate * ETime.FRAME_TIME);
            tr.position = location = Vector2.Lerp(location, TargetPosition, lerpToPositionRate * ETime.FRAME_TIME);
            viewfinder.eulerAngles = new Vector3(0f, 0f, angle + CameraOrientationAngleOffset);
        }
    }
    public override void RegularUpdate() {
        base.RegularUpdate();
        if (player.AllowPlayerInput) {
            bool full = ChargeFull;
            charge = M.Clamp(chargeMin, chargeMax, charge + GetChargeRate(CameraState) * ETime.FRAME_TIME);
            if (!full && ChargeFull) {
                SFXService.Request(onFullCharge);
            }
            text.text = string.Format(textFormat, charge);
            text.color = TextColor;
        }
        //Log.Unity($"{CameraState} {charge} {PlayerInput.IsFiring}");
    }
    public override int UpdatePriority => UpdatePriorities.PLAYER2;

    private static double GetChargeRate(State s) {
        if (s == State.NORMAL) return 12;
        else if (s == State.CHARGE) return 43;
        else return 0;
    }

    private const double chargeMin = 0;
    private const double chargeMax = 100;
    //0-100
    private double charge = 50;
    public bool ChargeFull => charge >= chargeMax;
    public bool InputCharging => player.IsFocus && player.IsFiring;
    private IEnumerator UpdateNormal() {
        viewfinder.gameObject.layer = lowCameraLayer;
        CameraState = State.NORMAL;
        bool alreadyCharging = player.IsFiring;
        while (true) {
            alreadyCharging &= player.IsFiring;
            if (ChargeFull && !player.IsFocus && player.IsFiring && !alreadyCharging) {
                RunDroppableRIEnumerator(UpdateFire());
                yield break;
            } else if (InputCharging) {
                RunDroppableRIEnumerator(UpdateCharge());
                yield break;
            }
            yield return null;
        }
    }

    private CRect ViewfinderRect(float scale) =>
        new CRect(location.x, location.y, CameraHalfBounds.x * scale, CameraHalfBounds.y * scale, angle);
    private IEnumerator UpdateFire() {
        CameraState = State.FIRING;
        var slowdownToken = ETime.Slowdown.CreateMultiplier(0.5f);
        viewfinder.gameObject.layer = highCameraLayer;
        var sfx = SFXService.RequestSource(whileFire);
        void Cancel() {
            if (sfx != null) {
                sfx.Stop();
            }
            slowdownToken.TryRevoke();
        }
        for (float t = 0f; t < cameraLerpDownTime; t += ETime.FRAME_TIME) {
            float scale = Mathf.Lerp(cameraFireSize, 1f, M.EInSine(t / cameraLerpDownTime));
            charge = 100 * (1 - M.EInSine(t / cameraLerpDownTime));
            viewfinder.localScale = new Vector3(scale, scale, scale);
            tr.position = location += cameraFireControlSpeed * ETime.FRAME_TIME * player.LastDelta;
            var vf = ViewfinderRect(scale);
            //take shot by letting go of fire key
            if (!player.IsFiring) {
                Cancel();
                RunDroppableRIEnumerator(TakePictureAndRefractor(scale));
                yield break;
            } else {
                var enemies = Enemy.FrozenEnemies;
                for (int ii = 0; ii < enemies.Count; ++ii) {
                    enemies[ii].enemy.ShowCrosshairIfViewfinderHits(vf);
                }
            }
            yield return null;
        }
        var _enemies = Enemy.FrozenEnemies;
        for (int ii = 0; ii < _enemies.Count; ++ii) {
            _enemies[ii].enemy.HideViewfinderCrosshair();
        }
        Cancel();
        SFXService.Request(onTimeout);
        RunDroppableRIEnumerator(UpdateNormal());
    }
    private IEnumerator UpdateCharge() {
        CameraState = State.CHARGE;
        var sfx = SFXService.RequestLoopingSource(whileCharge);
        while (InputCharging) yield return null;
        if (sfx != null) sfx.Stop();
        RunDroppableRIEnumerator(UpdateNormal());
    }

    private bool TakePicture_Freeze(float scale) {
        if (!GameStateManager.TemporaryEffectPause(out Action cb)) return false;
        WaitingUtils.WaitThenCB(GlobalDuringPause, Cancellable.Null, freezeTime, false, cb);
        return true;
    }
    private bool TakePicture_Enemies(float scale) {
        var vf = ViewfinderRect(scale);
        var enemies = Enemy.FrozenEnemies;
        bool hitEnemy = false;
        for (int ii = 0; ii < enemies.Count; ++ii) {
            //TODO may need more generalized capturing logic
            hitEnemy |= enemies[ii].enemy.FireViewfinder(vf) && enemies[ii].enemy.Beh is BossBEH;
        }
        return hitEnemy;
    }
    private void TakePicture_Delete(float scale) {
        var rect = ViewfinderRect(scale);
        BulletManager.Autodelete("cwheel", "red/b", b => Collision.PointInRect(b.loc, rect));
    }
    private IEnumerator TakePictureAndRefractor(float scale) {
        bool success = false;
        if (TakePicture_Freeze(scale)) {
            Vector2? targetLoc = null;
            success = TakePicture_Enemies(scale);
            viewfinderSR.enabled = false;
            text.enabled = false;
            var photo = MainCamera.main.RequestAyaPhoto(ViewfinderRect(scale));
            var pphoto = GameObject.Instantiate(pinnedPhotoPrefab).GetComponent<AyaPinnedPhoto>();
            if (success) {
                if (StaticNullableStruct.LastGame?.replay == null) photo.KeepAlive = true;
                targetLoc = AyaPhotoBoard.NextPinLoc(pphoto) ?? new Vector2(-6, 0);
                ChallengeManager.SubmitPhoto(photo);
            }
            pphoto.Initialize(photo, location, targetLoc);
            viewfinderSR.enabled = true;
            text.enabled = true;
            GlobalDuringPause.RunDroppableRIEnumerator(DoFlash(flashTime, success));
            //Wait until after the freeze to delete bullets
            yield return null;
            TakePicture_Delete(scale);
        }
        RunDroppableRIEnumerator(UpdateRefractory(success));
    }
    private IEnumerator UpdateRefractory(bool shotHit) {
        viewfinder.gameObject.layer = lowCameraLayer;
        float t = shotHit ? 1.2f : 0.4f;
        CameraState = State.REFRACTORY;
        charge = Math.Min(charge, shotHit ? 0 : 50);
        viewfinder.localScale = new Vector3(1f, 1f, 1f);
        for (float elapsed = 0f; elapsed < t; elapsed += ETime.FRAME_TIME) {
            yield return null;
        }
        RunDroppableRIEnumerator(UpdateNormal());
    }

    private IEnumerator DoFlash(float time, bool success) {
        SFXService.Request(onFlash);
        flash.enabled = true;
        Color c = flash.color;
        c.a = 1;
        flash.color = c;
        for (float t = 0; t < time; t += ETime.FRAME_TIME) {
            c.a = 1 - M.EInSine(t / time);
            flash.color = c;
            yield return null;
        }
        c.a = 0;
        flash.color = c;
        flash.enabled = false;
        SFXService.Request(success ? onPictureSuccess : onPictureMiss);
    }
    

    protected override void OnDisable() {
        base.OnDisable();
        ResetCameraState();
    }
}
}