using Verse.AI;

using HarmonyLib;
using Settings = Tacticowl.ModSettings_Tacticowl;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using UnityEngine;
using Verse;

namespace Tacticowl.DualWield
{
    // [HarmonyPatch(typeof(JobDriver_AttackStatic), nameof("<MakeNewToils>b__2"))]
    [HarmonyPatch]
    public class Patch_JobDriver_AttackStatic
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }

        public static MethodBase TargetMethod()
        {
            // 
            var type = AccessTools.FirstInner(
                typeof(JobDriver_AttackStatic),
                t => t.Name.Contains("DisplayClass")
                );
            var method = type.GetMethods(AccessTools.all).First(
                method => (
                    method.Name.Contains("<MakeNewToils>") 
                    && method.ReturnType == typeof(void)
                    && method.IsHideBySig 
                    // There should be a better way than selecting by the auto-generated name...
                    && method.Name.Contains("b__2") 
                )
            );
            Log.Message($"{type}::{method}");
            return method;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // TryStartAttack lines need to be expanded to attempt both attacks
            // Patch FullBodyBusy to allow for attacking with both weapons
            int found = 0;
            // For a new try attack method
            var TryStartAttackMethod = AccessTools.Method(typeof(Pawn), nameof(Pawn.TryStartAttack));
            var TryStartBothAttacks = AccessTools.Method(typeof(DualWieldUtility), nameof(DualWieldUtility.TryStartBothAttacks));
            foreach (var instruction in instructions)
            {
                // Replace TryStartAttack with TryStartBothAttacks
                if (instruction.OperandIs(TryStartAttackMethod))
                {
                    // Log.Message("Found TryStartAttack, replacing.");
                    found++;
                    yield return new CodeInstruction(OpCodes.Call, TryStartBothAttacks);
                }
                else yield return instruction;

            }
            Log.Message($"Found {found} many instances");
        }
        
        static void Prefix( )
        {
            Log.Message("tickAction anon method");
        }

    }
    
}