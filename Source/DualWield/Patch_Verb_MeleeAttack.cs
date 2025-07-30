using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(Verb_MeleeAttack), nameof(Verb_MeleeAttack.TryCastShot))]
    class Patch_Verb_MeleeAttack_TryCastShot
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            var method = AccessTools.Property(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.FullBodyBusy)).GetGetMethod();
            var currentHandBusy = AccessTools.Method(
                typeof(DualWieldUtility), 
                nameof(DualWieldUtility.CurrentHandBusy)
            );
            foreach (CodeInstruction instruction in instructions)
            {
                if (!found && instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(method))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call,
                        currentHandBusy
                    );
                }
                else
                {
                    yield return instruction;
                }
            }
            if (!found) Log.Error("[Tacticowl] Patch_Verb_MeleeAttack_TryCastShot transpiler failed to find its target. Did RimWorld update?");
        }
    }
}