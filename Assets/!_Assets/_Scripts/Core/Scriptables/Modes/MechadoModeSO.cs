using UnityEngine;

[CreateAssetMenu(fileName = "MechadoMode", menuName = "Cooking/Modes/Mechado")]
public class MechadoModeSO : CookingModeSO
{
    public override IngredientRole[] UnlockRoles => new[]
    {
        IngredientRole.CookingWater,
        IngredientRole.MechadoCatsup,
        IngredientRole.MechadoPowderedMilk
    };

    // Crackers = generic FryingSalt role reused as the post-bowl seasoning
    public override IngredientRole[] PostBowlRoles => new[] { IngredientRole.FryingSalt };
    public override IngredientRole[] ConflictRoles => new[] { IngredientRole.FryingOil };
    public override string SeasoningNotification   => "Add crackers to the pan!";

    public override bool CanCook(GameObject o)         => o.GetComponent<IMechadoable>()?.CanBeMechado()      ?? false;
    public override void StartCooking(GameObject o)    => o.GetComponent<IMechadoable>()?.StartMechado();
    public override void StopCooking(GameObject o)     => o.GetComponent<IMechadoable>()?.StopMechado();
    public override void CompleteCooking(GameObject o) => o.GetComponent<IMechadoable>()?.CompleteMechado();
}
