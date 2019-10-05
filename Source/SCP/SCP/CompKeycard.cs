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
    public class CompKeycard : ThingComp
    {
        public bool Locked
        {
            get
            {
                return this.lockedInt;
            }
            set
            {
                if (value == this.lockedInt) return;
                this.lockedInt = value;
                if (this.parent.Spawned)
                {
                    if (this.parent is Building_Door)
                    {
                        this.parent.Map.reachability.ClearCache();
                    }
                }
            }
        }

        public int Level
        {
            get
            {
                return this.levelAccess = this.levelAccess > 5 ? 5 : this.levelAccess < 1 ? 1 : this.levelAccess;
            }
            set
            {
                if (value == this.levelAccess) return;
                this.levelAccess = value;
                if(this.parent is Building_Door)
                {
                    this.parent.Map.reachability.ClearCache();
                }
            }
        }

        public CompProperties_Keycard Props
        {
            get
            {
                return (CompProperties_Keycard)this.props;
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref this.lockedInt, "locked", false, false);
            Scribe_Values.Look<int>(ref this.levelAccess, "levelAccess", 1, false);
        }

        //Future work
        public override void PostDraw()
        {
            if (this.lockedInt)
            {
                if (this.parent.def.category == ThingCategory.Building)
                {
                    this.parent.Map.overlayDrawer.DrawOverlay(this.parent, OverlayTypes.ForbiddenBig);
                }
            }
        }

        public override void PostSplitOff(Thing piece)
        {
            //Implement Here
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (this.parent is Verse.Building && this.parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            yield return new Command_SetLevelAccess
            {
                keycard = this,
                defaultLabel = "SCP_SetLevelAccess".Translate(),
                defaultDesc = "SCP_SetLevelAccessDesc".Translate(),
                icon = TexCommand.PauseCaravan
            };
            yield break;
        }



        private bool lockedInt = false;

        private int levelAccess = 1;
    }
}
