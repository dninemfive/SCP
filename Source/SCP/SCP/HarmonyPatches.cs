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
            harmony.Patch(original: AccessTools.Method(type: typeof(Building_Door), name: nameof(Building_Door.Draw)),
                prefix: new HarmonyMethod(type: typeof(HarmonyPatches), name: nameof(SCPDoorDraw)));
            harmony.Patch(original: AccessTools.Property(type: typeof(CompPowerTrader), name: nameof(CompPowerTrader.PowerOn)).GetSetMethod(),
                prefix: null,
                postfix: null,
                transpiler: new HarmonyMethod(type: typeof(HarmonyPatches), name: nameof(SCPPowerOnTranspiler)));

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
                    return false;
                }
            }
            return true;
        }

        public static bool EnterRoomKeycard(Thing t, Pawn pawn, ref bool __result)
        {
            if(t.Spawned && !((t as Building_Door) is null))
            {
                CompKeycard key = (t as ThingWithComps).GetComp<CompKeycard>();
                __result = !(key is null);
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

        public static bool SCPDoorDraw(Building_Door __instance, ref int ___visualTicksOpen)
        {
            if(!(__instance.GetComp<CompKeycard>() is null))
            {
                __instance.Rotation = Building_Door.DoorRotationAt(__instance.Position, __instance.Map);
                float num = Mathf.Clamp01((float)___visualTicksOpen / (float)__instance.TicksToOpenNow);
                Vector3 vector = default(Vector3);
                Mesh mesh;

                vector = new Vector3(0f, 0f, -1f);
                mesh = MeshPool.plane10;

                Rot4 rotation = __instance.Rotation;
                rotation.Rotate(RotationDirection.Clockwise);
                vector = rotation.AsQuat * vector;
                Vector3 vector2 = __instance.DrawPos;
                vector2.y = AltitudeLayer.DoorMoveable.AltitudeFor();
                vector2 += vector * num * 0.95f;
                Graphics.DrawMesh(mesh, vector2, __instance.Rotation.AsQuat, __instance.Graphic.MatAt(__instance.Rotation, null), 0);
                
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> SCPPowerOnTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            MethodInfo cacheMethod = AccessTools.Method(type: typeof(CompKeycard), name: nameof(CompKeycard.ClearCache));
            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if(instruction.opcode == OpCodes.Stfld)
                {
                    i++;
                    yield return instruction;

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: cacheMethod);
                    instruction = instructionList[i];
                }
                yield return instruction;
            }
        }
        #endregion LevelAccess(Doors)

        #region LevelAccess(Pawns)
        private static void PawnLevelAccess(Pawn_DraftController __instance, ref IEnumerable<Gizmo> __result)
        {
            if(!__instance.Drafted)
            {
                List<Gizmo> gizmos = __result.ToList();
                Command_KeycardPawn keyGizmo = new Command_KeycardPawn();
                keyGizmo.pawn = __instance.pawn;
                keyGizmo.icon = ContentFinder<Texture2D>.Get("Keycards/Gizmo/KeycardIcon/KeyCardIcon", true);
                keyGizmo.defaultLabel = "SCP_SetLevelAccess".Translate();
                keyGizmo.defaultDesc = "SCP_SetLevelAccessDesc".Translate();
                gizmos.Insert(0, keyGizmo);
                __result = gizmos;
            }
        }

        #endregion LevelAccess(Pawns)

    }
}
