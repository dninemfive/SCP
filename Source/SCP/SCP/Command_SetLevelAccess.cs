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
    public class Command_SetLevelAccess : Command
    {
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (this.keycards is null) this.keycards = new List<CompKeycard>();
            if (!this.keycards.Contains(this.keycard)) this.keycards.Add(this.keycard);

            int startLevel = this.keycards is null ? 1 : keycards.Min(x => x.Level);

            Func<int, string> textGetter;
            textGetter = ((int x) => "SCP_SetKeycard".Translate(x));
            Dialog_Keycard window = new Dialog_Keycard(textGetter, 1, 5, delegate (int value)
            {
                foreach (CompKeycard key in this.keycards)
                {
                    key.Level = (int)value;
                }
            }, startLevel);
            Find.WindowStack.Add(window);
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            if(this.keycards is null)
            {
                this.keycards = new List<CompKeycard>();
            }
            this.keycards.Add(((Command_SetLevelAccess)other).keycard);
            return false;
        }

        public CompKeycard keycard;

        private List<CompKeycard> keycards;
    }
}
