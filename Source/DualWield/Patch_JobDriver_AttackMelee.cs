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
    [HarmonyPatch]
    public class Patch_JobDriver_AttackMelee
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }

        public static MethodBase TargetMethod()
        {
            // 
            var type = typeof(JobDriver_AttackMelee);
            var method = type.GetMethods(AccessTools.all).First(
                method => (
                    method.Name.Contains("<MakeNewToils>") 
                    && method.ReturnType == typeof(void)
                    && method.IsHideBySig 
                    // There should be a better way than selecting by the auto-generated name...
                    && method.Name.Contains("b__4_2") 
                )
            );
            Log.Message($"{type}::{method}");
            return method;
        }
    
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
            var tryMeleeAttack = AccessTools.Method(
                typeof(Pawn_MeleeVerbs), 
                nameof(Pawn_MeleeVerbs.TryMeleeAttack)
            );
            var tryMeleeAttackBothHands = AccessTools.Method(
                typeof(DualWieldUtility),
                nameof(DualWieldUtility.TryMeleeAttackBothHands)
            );
            bool found = false;
            foreach (var instruction in instructions)
            {
                if (instruction.OperandIs(tryMeleeAttack))
                {
                    yield return new CodeInstruction(OpCodes.Call, tryMeleeAttackBothHands);
                    Log.Message("Replacing TryMeleeAttack");
                    found = true;
                }
                else yield return instruction;

            }

            if (!found)
            {
                Log.Message("TryMeleeAttack not found");
            }
        
    }
    }
}