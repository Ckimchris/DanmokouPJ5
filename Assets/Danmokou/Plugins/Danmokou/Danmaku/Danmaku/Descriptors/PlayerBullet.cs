﻿using Danmokou.Player;
using Danmokou.Scriptables;

namespace Danmokou.Danmaku.Descriptors {

public readonly struct PlayerBullet {
    public readonly PlayerBulletCfg data;
    public readonly PlayerController firer;

    public PlayerBullet(PlayerBulletCfg data, PlayerController firer) {
        this.data = data;
        this.firer = firer;
    }
}
public readonly struct PlayerBulletCfg {
    public readonly int cdFrames;
    public readonly bool destructible;
    public readonly int bossDmg;
    public readonly int stageDmg;
    public readonly int statusDmg;
    public readonly EffectStrategy effect;

    public PlayerBulletCfg(int cd, bool destructible, int boss, int stage, int status, EffectStrategy eff) {
        cdFrames = cd;
        this.destructible = destructible;
        bossDmg = boss;
        stageDmg = stage;
        statusDmg = status;
        effect = eff;
    }

    public PlayerBullet Realize(PlayerController firer) => new(this, firer);
}
}