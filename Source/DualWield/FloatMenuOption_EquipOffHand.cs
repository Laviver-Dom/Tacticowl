using RimWorld;
using Verse;
using Verse.AI;
using System;
using System.Collections.Generic;
using LocalTargetInfo = Verse.LocalTargetInfo;
using TaggedString = Verse.TaggedString;

namespace Tacticowl.DualWield;

public class FloatMenuOption_EquipOffHand : FloatMenuOptionProvider
{
  public override bool Drafted => true;
  public override bool Undrafted => true;
  public override bool Multiselect => false;

  // What does this method actually do?
  public override bool AppliesInt(FloatMenuContext context)
  {
    return context.FirstSelectedPawn.equipment != null;
  }

  public override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
  {
    // What goes here???
    return AddOffHandWeaponOption(clickedThing, context);
  }

  /*
   * Ripped from RimWorld.FloatMenuOptionProvider_Equip.GetSingleOptionFor
   * In an attempt to keep the off-hand checks as close to main hand checks but with a little extra specialization
   * done in the caller's logic.
   */
  protected FloatMenuOption AddOffHandWeaponOption(
    Thing clickedThing,
    FloatMenuContext context)
  {
    var pawn = context.FirstSelectedPawn;
    /*
     * What does this actually do???
     * We check if the pawn can use items.
     * We check tha the pawn already has some equipment for some reason
     * Then if those are good we check the map location where the click happened and determine if any items
     * at that location are equippable.
     * If the pawn can equip things at that location, all equippable things will show up with an option.
     * Why? Default RimWorld does not do this.
     */
    // if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && pawn.equipment != null)
    // {
    //   List<Thing> thingList = IntVec3.FromVector3(clickPos).GetThingList(pawn.Map);
    //   for (int i = thingList.Count; i-- > 0;)
    //   {
    //     if (thingList[i] is ThingWithComps equipment && equipment.GetComp<CompEquippable>() != null)
    //     {
    //       opts.Add(GetEquipOffHandOption(pawn, equipment));
    //     }
    //   }
    // }

    if (!clickedThing.HasComp<CompEquippable>())
      return (FloatMenuOption)null;

    string labelShort = clickedThing.LabelShort;
    string reason = null;

    // These are the regular RimWorld reasons
    if (clickedThing.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
      reason = "IsIncapableOfViolenceLower".Translate(
        (NamedArgument)pawn.LabelShort,
        (NamedArgument)(Thing)pawn);
    else if (clickedThing.def.IsRangedWeapon && pawn.WorkTagIsDisabled(WorkTags.Shooting))
      reason = "IsIncapableOfShootingLower".Translate((NamedArgument)(Thing)pawn);
    else if (!pawn.CanReach((LocalTargetInfo)clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
      reason = "NoPath".Translate().CapitalizeFirst();
    else if (clickedThing.IsBurning())
      reason = "BurningLower".Translate();
    else if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(clickedThing, pawn))
      reason = "QuestRelated".Translate().CapitalizeFirst();
    else if (!EquipmentUtility.CanEquip(clickedThing, pawn, out reason, false))
      reason = reason.CapitalizeFirst();
    else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
      reason = "Incapable".Translate();
    // These are our custom dual wield reasons
    else if (pawn.HasMissingArmOrHand())
      reason = "DW_MissArmOrHand".Translate();
    else if (pawn.equipment != null && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsTwoHanded())
      reason = "DW_WieldingTwoHanded".Translate();


    if (!reason.NullOrEmpty())
      return new FloatMenuOption(
        (string)"DW_AsOffHand".Translate(
          (NamedArgument)"CannotEquip".Translate((NamedArgument)labelShort),
          (NamedArgument)reason),
        (Action)null
      );


    // At this point we know whether something _can_ be equipped by the pawn.
    TaggedString text = (string)"DW_AsOffHand".Translate(
      (NamedArgument)"Equip".Translate((NamedArgument)labelShort)
    );

    if (clickedThing.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
      text = $"{text} " + "EquipWarningBrawler".Translate();

    // This part is a mess...
    // Check is a unbonded weapon should become bonded
    if (!EquipmentUtility.AlreadyBondedToWeapon(clickedThing, pawn))
      return FloatMenuUtility.DecoratePrioritizedTask(
        new FloatMenuOption(text,
          (Action)(() =>
          {
            string confirmationText = EquipmentUtility.GetPersonaWeaponConfirmationText(clickedThing, pawn);
            if (!confirmationText.NullOrEmpty())
              Find.WindowStack.Add(
                (Window)new Dialog_MessageBox(
                  (TaggedString)confirmationText,
                  (string)"Yes".Translate(),
                  (Action)(() => EquipOffHand()),
                  (string)"No".Translate()
                )
              );
            else
              EquipOffHand();
          }), MenuOptionPriority.High),
        pawn,
        (LocalTargetInfo)clickedThing
      );
    string label2 = (string)(text + (" " + "BladelinkAlreadyBonded".Translate()));
    TaggedString dialogText = "BladelinkAlreadyBondedDialog".Translate(pawn.Named("PAWN"),
      clickedThing.Named("WEAPON"), pawn.equipment.bondedWeapon.Named("BONDEDWEAPON"));
    return FloatMenuUtility.DecoratePrioritizedTask(
      new FloatMenuOption(label2, (Action)(() => Find.WindowStack.Add((Window)new Dialog_MessageBox(dialogText))),
        MenuOptionPriority.High), pawn, (LocalTargetInfo)clickedThing);

    void EquipOffHand()
    {
      // Original RimWorld Code
      // clickedThing.SetForbidden(false);
      // pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, (LocalTargetInfo) clickedThing));
      // FleckMaker.Static(clickedThing.DrawPos, clickedThing.MapHeld, FleckDefOf.FeedbackEquip);
      // PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
      // Off Version
      clickedThing.SetForbidden(false, true);
      pawn.jobs.TryTakeOrderedJob(new Job(ResourceBank.JobDefOf.DW_EquipOffHand, (LocalTargetInfo)clickedThing),
        JobTag.Misc);
      FleckMaker.Static(clickedThing.DrawPos, clickedThing.Map, FleckDefOf.FeedbackEquip, 1f);
      PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
    }
  }
}