﻿using System;
using Danmokou.Core;
using Danmokou.Services;
using Newtonsoft.Json;
using Suzunoya.Data;
using UnityEngine;

namespace Danmokou.ADV {

/// <summary>
/// All instance data for a (saveable) in-progress ADV game instance.
/// <br/>This class should be derived for game-specific data.
/// </summary>
[Serializable]
public record ADVData(InstanceData VNData) {
    public string CurrentMap { get; set; } = "";
    //Json usage
    [Obsolete]
    public ADVData() : this(default(InstanceData)!) {}
}

[Serializable]
public record SerializedSave {
    public SerializedImage Image { get; init; } = null!;
    public string GameIdentifier { get; init; }
    public DateTime SaveTime { get; init; }
    public string Filename { get; init; }
    public int Slot { get; init; }
    public string Description { get; init; }

    //Don't store this. We need a new object every time we query to avoid sharing references
    //private DMKVNData? data;
    [JsonIgnore] public string SaveLocation => $"{SaveLocationNoExt}.txt";
    [JsonIgnore] public string SaveLocationNoExt => $"{FileUtils.INSTANCESAVEDIR}{Filename}";

    /// <summary>
    /// Json constructor, do not use
    /// </summary>
    [Obsolete]
#pragma warning disable 8618
    public SerializedSave() { }
#pragma warning restore 8618

    public SerializedSave(ADVData data, DateTime saveTime, Texture2D tex, int slot, string descr) {
        //this.data = data;
        GameIdentifier = GameManagement.References.gameIdentifier;
        SaveTime = saveTime;
        Filename = $"{slot.PadLZero(3)}-{SaveTime.FileableTime()}";
        FileUtils.WriteJson($"{SaveLocationNoExt}.dat", data);
        Image = new($"{SaveLocationNoExt}.jpg", tex, true);
        Slot = slot;
        Description = descr;
    }

    public ADVData GetData() {
        var save = FileUtils.ReadJson<ADVData>($"{SaveLocationNoExt}.dat") ?? 
                   throw new Exception($"Couldn't read save data at {SaveLocationNoExt}");
        save.VNData._SetGlobalData_OnlyUseForInitialization(SaveData.r.GlobalVNData);
        return save;
    }

    public void RemoveFromDisk() {
        FileUtils.Delete($"{SaveLocationNoExt}.dat");
        Image.RemoveFromDisk();
    }
}

}