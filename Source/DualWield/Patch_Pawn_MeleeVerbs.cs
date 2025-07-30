using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList))]
    class Patch_Pawn_MeleeVerbs_GetUpdatedAvailableVerbsList
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static void Postfix(List<VerbEntry> __result)
        {
            //remove all offHand verbs so they're not used by for mainhand melee attacks.
            for (var i = __result.Count; i-- > 0;)
            {
                var ve = __result[i];
                if (ve.verb.EquipmentSource.IsOffHandedWeapon()) __result.Remove(ve);
            }
        }
    }

    // Patch TryMeleeAttack to use can main hand attack
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
    class Patch_Pawn_MeleeVerbs_TryMeleeAttack
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var fullBodyBusyMethod = AccessTools.Property(
                typeof(Pawn_StanceTracker), 
                nameof(Pawn_StanceTracker.FullBodyBusy)
            ).GetGetMethod();
            var currentHandBusy = AccessTools.Method(
                typeof(DualWieldUtility), 
                nameof(DualWieldUtility.CurrentHandBusy)
            );
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.OperandIs(fullBodyBusyMethod))
                {
                    // yield return new CodeInstruction(OpCodes.Pop);
                    // yield return new CodeInstruction(OpCodes.Ldarg_0);
                    // yield return new CodeInstruction(OpCodes.Ldfld, 
                    //     AccessTools.Field(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.pawn))
                    // );
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        currentHandBusy
                        );

                }
                else yield return instruction;
            }
            
        }
        
    }
}