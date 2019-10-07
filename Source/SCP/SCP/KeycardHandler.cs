using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;


namespace SCP
{
    public class KeycardHandler : DefModExtension
    {
        public int AccessLevel
        {
            get
            {
                return this.levelAccess > 5 ? 5 : this.levelAccess < 1 ? 1 : this.levelAccess;
            }
            set
            {
                if (value == this.levelAccess) return;
                this.levelAccess = value;
                Find.CurrentMap.reachability.ClearCache();
            }
        }

        public int levelAccess;
    }
}
