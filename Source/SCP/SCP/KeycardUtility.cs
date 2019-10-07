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
    public static class KeycardUtility
    {
        public static void SetKeycard(this Thing t, int value, bool warnOnFail = true)
        {
            if(t is null)
            {
                if(warnOnFail)
                {
                    Log.Error("Tried to Set Keycard on null Thing.", false);
                }
                return;
            }
            ThingWithComps thing = t as ThingWithComps;
            if(thing is null)
            {
                if(warnOnFail)
                {
                    Log.Error("Tried to Set Keycard on non-ThingWithComps Thing " + t, false);
                }
                return;
            }
            CompKeycard comp = thing.GetComp<CompKeycard>();
            if(comp is null)
            {
                if(warnOnFail)
                {
                    Log.Error("Tried to Set Keycard on non-Keycard thing " + t, false);
                }
                return;
            }
            comp.Level = value;
        }

        public static bool HasKeycardAccess(this Thing t, Pawn pawn)
        {
            if (Faction.OfPlayer is null) return false;
            if (Faction.OfPlayer != t.Faction) return false;
            if (!pawn.RaceProps.Humanlike) return false;
            if (!(t as ThingWithComps).GetComp<CompPowerTrader>().PowerOn) return false;
            Log.Message("Test: " + (t as ThingWithComps).GetComp<CompPowerTrader>().PowerOn);
            ThingWithComps thingWithComps = t as ThingWithComps;
            if (thingWithComps is null) return true;
            CompKeycard comp = thingWithComps.GetComp<CompKeycard>();
            if (comp.Locked) return false;
            return comp.Level <= pawn.def.GetModExtension<KeycardHandler>().AccessLevel ? true : false;
        }

        public static bool KeyCardAccessToPass(this Building_Door t, Pawn pawn)
        {
            return t.HasKeycardAccess(pawn);
        }
    }
}
