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
    [StaticConstructorOnStartup]
    public class Command_KeycardPawn : Command
    {
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (this.pawns is null) this.pawns = new List<Pawn>();
            if (!this.pawns.Contains(this.pawn)) this.pawns.Add(this.pawn);

            int level = pawn.def.GetModExtension<KeycardHandler>().AccessLevel;

            Func<int, string> textGetter;
            textGetter = ((int x) => "SCP_SetKeycard".Translate(x));
            Dialog_Keycard window = new Dialog_Keycard(textGetter, 1, 5, delegate (int value)
            {
                foreach(Pawn p in pawns)
                {
                    pawn.def.GetModExtension<KeycardHandler>().AccessLevel = (int)value;
                }
            }, level);
            Find.WindowStack.Add(window);
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            if(this.pawns is null)
            {
                this.pawns = new List<Pawn>();
            }
            this.pawns.Add(((Command_KeycardPawn)other).pawn);
            return false;
        }

        public Pawn pawn;

        private List<Pawn> pawns;
    }
}
