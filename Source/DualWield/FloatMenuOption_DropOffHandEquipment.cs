using RimWorld;
using Verse;
using Verse.AI;
using System;
using UnityEngine;

namespace Tacticowl.DualWield;

// The majority of this code is ripped straight from the main hand version.
public class FloatMenuOption_DropOffHandEquipment : FloatMenuOptionProvider
{
    public override bool Drafted => true;
    public override bool Undrafted => true;
    public override bool Multiselect => false;
    public override bool CanSelfTarget => true;
    public override bool AppliesInt(FloatMenuContext context)
    {
        return context.FirstSelectedPawn.equipment?.Primary != null;
    }

    public override FloatMenuOption GetSingleOptionFor(
        Pawn clickedPawn,
        FloatMenuContext context)
    {
        if (clickedPawn != context.FirstSelectedPawn)
            return (FloatMenuOption) null;
        ThingWithComps offhander = null;
        // No off hand to drop
        if (!clickedPawn.GetOffHander(out offhander))
            return (FloatMenuOption)null;
        // if (clickedPawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanUnequip((Thing) clickedPawn.equipment.Primary, clickedPawn))
        if (clickedPawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanUnequip(offhander, clickedPawn))
            return new FloatMenuOption((string) ("CannotDrop".Translate((NamedArgument) offhander.Label, (NamedArgument) (Thing) offhander) + ": " + "QuestRelated".Translate().CapitalizeFirst()), (Action) null);
        Action action = (Action) (() => clickedPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, (LocalTargetInfo) (Thing) offhander)));
        return new FloatMenuOption((string) "DW_DropOffHand".Translate((NamedArgument) offhander.Label, (NamedArgument) (Thing) offhander), action, (Thing) clickedPawn.equipment.Primary, Color.white, revalidateClickTarget: (Thing) clickedPawn);
    }
}