﻿using RimWorld.Planet;
using Verse;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SCP
{
    public class WorldComponent_FactionsTracker : WorldComponent
    {
        public bool firstSCPSpawned = false;
        public List<CustomFactionDef> factionsLeftToSpawn = new List<CustomFactionDef>();
        public CustomFactionDef joinedFactionDef = null;
        public int ticksUntilNextReveal = int.MaxValue;
        public int ticksUntilSCPArrival = int.MaxValue;
        public int ticksUntilHostilities = int.MaxValue;
        public static int numOfSCPFactions = 1;
        public List<Pawn> activeRepresentatives = new List<Pawn>();
        private bool factionsSpawned;
        private DevSettings devSettings = DefDatabase<DevSettings>.AllDefs.First();

        private int GetTimeUntilHostilities =>
            new IntRange(
                (int)(GenDate.TicksPerDay * devSettings.daysUntilHostilities.min), 
                (int)(GenDate.TicksPerDay * devSettings.daysUntilHostilities.max))
            .RandomInRange;


        private int GetInitialSCPArrivalTime =>
            new IntRange(
                (int)(GenDate.TicksPerDay * devSettings.daysUntilSCPArrival.min),
                (int)(GenDate.TicksPerDay * devSettings.daysUntilSCPArrival.max))
            .RandomInRange;


        private int GetNextRevealInterval =>
             new IntRange(
                 (int)(GenDate.TicksPerDay * devSettings.daysBetweenRepresentatives.min),
                 (int)(GenDate.TicksPerDay * devSettings.daysBetweenRepresentatives.max))
            .RandomInRange;

        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame < 500)
            {
                base.WorldComponentTick();
                return;
            }
            if (!firstSCPSpawned &&
                Find.TickManager.TicksGame > ticksUntilSCPArrival)
            {
                firstSCPSpawned = true;
                SCPUtility.SpawnFirstSCPGroup(Find.AnyPlayerHomeMap);
                factionsLeftToSpawn = new List<CustomFactionDef>(DefDatabase<CustomFactionDef>.AllDefsListForReading);
                ticksUntilNextReveal = Find.TickManager.TicksGame + GetNextRevealInterval;
            }
            if (factionsLeftToSpawn?.Count > 0 && firstSCPSpawned &&
                Find.TickManager.TicksGame > ticksUntilNextReveal)
            {
                ticksUntilNextReveal = Find.TickManager.TicksGame + GetNextRevealInterval;
                CustomFactionDef toBeRevealed = factionsLeftToSpawn.Pop();
                if (factionsLeftToSpawn?.Count == 0)
                {
                    factionsSpawned = true;
                    ticksUntilHostilities = Find.TickManager.TicksGame + GetTimeUntilHostilities;
                }
                FactionUtility.IntroduceFaction(toBeRevealed);
            }
            if (Find.TickManager.TicksGame > ticksUntilHostilities)
            {
                ticksUntilHostilities = int.MaxValue;

                //Log.Message("1");
                var facDefs = new List<CustomFactionDef>(DefDatabase<CustomFactionDef>.AllDefsListForReading);
                //Log.Message("2");
                var facs = new List<Faction>(Find.FactionManager.AllFactions
                    .Where(f => f.def is CustomFactionDef sDef && facDefs.Contains(sDef)));
                //Log.Message("3");
                var s = new StringBuilder();
                //Log.Message("4");
                if (facs == null || facs?.Count() == 0)
                {
                    Log.Error("SCP :: Failed to find SCP-related factions for declaring hostilities.");
                    return;
                }
                //Log.Message("5");
                var hostileFacs = facs.Where(x => x.def is CustomFactionDef sDef && sDef.hostileByDefault && sDef != joinedFactionDef);
                //Log.Message("6");
                if (hostileFacs != null && hostileFacs?.Count() > 0)
                {
                    //Log.Message("6a");
                    foreach (var hFac in hostileFacs)
                    {
                        //Log.Message("6a-loop");
                        hFac.TrySetNotAlly(Faction.OfPlayer, true);
                        hFac.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Hostile);
                        s.AppendLine("SCP_Hostilities".Translate(hFac.Name, Faction.OfPlayer.Name));
                    }
                }
                Log.Message("7");
                foreach (var f in facs)
                {
                    //Log.Message("7a");
                    var fDef = f.def as CustomFactionDef;
                    foreach (var hostileFactionName in fDef.hostileToFactions)
                    {
                        //Log.Message("7aloop");
                        var hostileFaction = facs.First(ff => ff.def.defName == hostileFactionName);
                        if (hostileFaction == f)
                            continue;

                        f.TrySetNotAlly(hostileFaction, true);
                        f.TrySetRelationKind(hostileFaction, FactionRelationKind.Hostile);
                        s.AppendLine("SCP_Hostilities".Translate(f.Name, hostileFaction.Name));
                    }
                }
                //Log.Message("8");
                if (joinedFactionDef != null)
                {
                    //Log.Message("8a");
                    var joinedFaction = facs.First(ff => ff.def == joinedFactionDef);
                    joinedFaction.TrySetNotHostileTo(Faction.OfPlayer);
                    Faction.OfPlayer.TryAffectGoodwillWith(joinedFaction, 100, false);
                    joinedFaction.TryAffectGoodwillWith(Faction.OfPlayer, 100, false);
                    s.AppendLine();
                    s.AppendLine("SCP_Alliances".Translate(joinedFaction.Name, Faction.OfPlayer.HasName ? Faction.OfPlayer.Name : "Player".Translate()));
                }
                //Log.Message("9");
                Find.LetterStack.ReceiveLetter("SCP_HostilitiesDeclared".Translate(), "SCP_HostilitiesDeclaredDesc".Translate(s.ToString()), LetterDefOf.ThreatBig);
            }
            base.WorldComponentTick();
        }

        public WorldComponent_FactionsTracker(World world) : base(world)
        { 
            devSettings = DefDatabase<DevSettings>.AllDefs.First();        
            numOfSCPFactions = DefDatabase<CustomFactionDef>.AllDefsListForReading.Count();
            ticksUntilSCPArrival = Find.TickManager.TicksGame + GetInitialSCPArrivalTime;
            Log.Message($"Ticks until SCP arrival: {ticksUntilSCPArrival}");

            //ticksUntilHostilities = Find.TickManager.TicksGame + GetTimeUntilHostilities;

        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.firstSCPSpawned, "firstSCPSpawned", false);
            Scribe_Values.Look(ref this.factionsSpawned, "factionsSpawned", false);
            Scribe_Collections.Look(ref this.factionsLeftToSpawn, "factionsLeftToSpawn", LookMode.Def);
            Scribe_Defs.Look(ref this.joinedFactionDef, "joinedFaction");
            Scribe_Values.Look(ref this.ticksUntilNextReveal, "ticksUntilNextReveal", int.MaxValue);
            Scribe_Values.Look(ref this.ticksUntilHostilities, "ticksUntilHostilities", int.MaxValue);
            Scribe_Values.Look(ref this.ticksUntilSCPArrival, "ticksUntilSCPArrival", int.MaxValue);
            Scribe_Collections.Look(ref this.activeRepresentatives, "activeRepresentatives", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                activeRepresentatives.RemoveAll((Pawn x) => x == null);
            }
            base.ExposeData();
        }
    }
}
