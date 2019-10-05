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
using OpCodes = System.Reflection.Emit.OpCodes;

namespace SCP
{
    [StaticConstructorOnStartup]
    internal static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = HarmonyInstance.Create("ods.scp.harmony");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //HarmonyInstance.DEBUG = true;

            #region PatchDefs
            //Level Access (Doors)
            harmony.Patch(original: AccessTools.Method(type: typeof(ForbidUtility), parameters: new Type[] { typeof(Thing), typeof(Faction) }, name: nameof(ForbidUtility.IsForbidden)),
                prefix: new HarmonyMethod(type: typeof(HarmonyPatches), name: nameof(EnterCellKeycard)));
            harmony.Patch(original: AccessTools.Method(type: typeof(ForbidUtility), name: nameof(ForbidUtility.IsForbiddenToPass)),
                prefix: new HarmonyMethod(type: typeof(HarmonyPatches), name: nameof(KeycardToPass)));
            harmony.Patch(original: AccessTools.Method(type: typeof(ForbidUtility), parameters: new Type[] { typeof(Thing), typeof(Pawn) }, name: nameof(ForbidUtility.IsForbidden)),
                prefix: new HarmonyMethod(type: typeof(HarmonyPatches), name: nameof(EnterRoomKeycard)));
            harmony.Patch(original: AccessTools.Method(type: typeof(GenPath), name: "ShouldNotEnterCell"),
                prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(HarmonyPatches), name: nameof(EnterCellKeycardTranspiler)));

            //Level Access (Pawns)
            harmony.Patch(original: AccessTools.Method(type: typeof(Pawn_DraftController), name: "GetGizmos"),
                prefix: null,
                postfix: new HarmonyMethod(type: typeof(HarmonyPatches), name: nameof(PawnLevelAccess)));
            #endregion PatchDefs
        }

        #region LevelAccess(Doors)
        public static bool KeycardToPass(Building_Door t, Pawn pawn, ref bool __result)
        {
            if(!(t.GetComp<CompKeycard>() is null) && (pawn.Spawned))
            {
                __result = !(KeycardUtility.KeyCardAccessToPass(t, pawn));
                Log.Message("Test: " + __result);
                return false;
            }
           return true;
        }

        public static bool EnterCellKeycard(Thing t, Faction faction, ref bool __result)
        {
            if (!((t as ThingWithComps) is null))
            {
                Building_Door b = t as Building_Door;
                if(!(b is null) && !((t as ThingWithComps).GetComp<CompKeycard>() is null))
                {
                    CompKeycard key = (t as ThingWithComps).GetComp<CompKeycard>();
                    __result = !(key is null);
                    Log.Message("Pass? " + __result);
                    return false;
                }
            }
            return true;
        }

        public static bool EnterRoomKeycard(Thing t, Pawn pawn, ref bool __result)
        {
            if(t.Spawned && !((t as Building_Door) is null))
            {
                Log.Message("Check");
                CompKeycard key = (t as ThingWithComps).GetComp<CompKeycard>();
                __result = !(key is null);
                Log.Message("EnterRoom: " + __result);
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> EnterCellKeycardTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            MethodInfo keycardMethod = AccessTools.Method(type: typeof(KeycardUtility), name: nameof(KeycardUtility.HasKeycardAccess));
            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if(instruction.opcode == OpCodes.Call && instruction.operand == AccessTools.Method(type: typeof(ForbidUtility), 
                    parameters: new Type[] { typeof(Building_Door), typeof(Pawn)}, name: "IsForbidden"))
                {
                    //Skip to end of return after method call
                    bool flag = false;
                    while(!flag)
                    {
                        if (instructionList[i].opcode == OpCodes.Ret) flag = true;
                        yield return instructionList[i];
                        i++;
                    }
                    Label label = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: keycardMethod);
                    yield return new CodeInstruction(opcode: OpCodes.Brtrue, operand: label);

                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(opcode: OpCodes.Ret);
                    instruction = instructionList[i];
                    instruction.labels.Add(label);
                }
                yield return instruction;
            }
        }
        #endregion LevelAccess(Doors)

        #region LevelAccess(Pawns)
        private static void PawnLevelAccess(Pawn_DraftController __instance, ref IEnumerable<Gizmo> __result)
        {
            List<Gizmo> gizmos = __result.ToList();
            //Add more
        }

        #endregion LevelAccess(Pawns)
    }
}
