using UnityEngine;

[CreateAssetMenu(fileName = "FryingMode", menuName = "Cooking/Modes/Frying")]
public class FryingModeSO : CookingModeSO
{
    public override IngredientRole[] UnlockRoles   => new[] { IngredientRole.FryingOil };
    public override IngredientRole[] PostBowlRoles => new[] { IngredientRole.FryingSalt };
    public override IngredientRole[] ConflictRoles => new[] { IngredientRole.CookingWater };
    public override string SeasoningNotification   => "Add salt to the pan before tossing!";

    public override bool CanCook(GameObject o)      => o.GetComponent<IFryable>()?.CanBeFried()     ?? false;
    public override void StartCooking(GameObject o) => o.GetComponent<IFryable>()?.StartFrying();
    public override void StopCooking(GameObject o)  => o.GetComponent<IFryable>()?.StopFrying();
    public override void CompleteCooking(GameObject o) => o.GetComponent<IFryable>()?.CompleteFrying();
}
