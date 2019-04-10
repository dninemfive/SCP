﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SCP
{
    public static class SCPUtility
    {
        public static void SpawnFirstSCPGroup(Map map)
        {
            Predicate<IntVec3> validator = (IntVec3 c) => map.reachability.CanReachColony(c) && !c.Fogged(map);
            IntVec3 spawnSpotOne;
            IntVec3 spawnSpotTwo;
            CellFinder.TryFindRandomEdgeCellWith(validator, map, CellFinder.EdgeRoadChance_Neutral, out spawnSpotOne);
            CellFinder.TryFindRandomEdgeCellWith(validator, map, CellFinder.EdgeRoadChance_Neutral, out spawnSpotTwo);

            Pawn pawnA = PawnGenerator.GeneratePawn(PawnKindDef.Named("SCP_ONE_THREE_ONE_A"));
            Pawn pawnB = PawnGenerator.GeneratePawn(PawnKindDef.Named("SCP_ONE_THREE_ONE_B"));
            GenSpawn.Spawn(pawnA, spawnSpotOne, map);
            GenSpawn.Spawn(pawnB, spawnSpotTwo, map);
        }
    }
}
