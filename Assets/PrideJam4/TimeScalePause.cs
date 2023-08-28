using System;
using System.Threading.Tasks;
using BagoumLib;
using BagoumLib.DataStructures;
using BagoumLib.Events;
using Danmokou.DMath;
using JetBrains.Annotations;
using UnityEngine;
using Danmokou.Core;

namespace Danmokou.Behavior.Functions
{
    public class TimeScalePause : RegularUpdater
    {
        private float t = 0;
        public float maxTime = 6f;
        // Start is called before the first frame update
        void Awake()
        {
            Pause();
        }

        public void Pause()
        {
            EngineStateManager.RequestState(EngineState.EFFECT_PAUSE);
        }

        public void Unpause()
        {
            EngineStateManager.RequestState(EngineState.RUN);

        }


        public override void RegularUpdate()
        {
            t += ETime.FRAME_TIME;
            if (t > maxTime) Unpause();
        }
    }
}