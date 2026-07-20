using UnityEngine;

[CreateAssetMenu(fileName = "AdoboMode", menuName = "Cooking/Modes/Adobo")]
public class AdoboModeSO : CookingModeSO
{
    public override IngredientRole[] UnlockRoles => new[]
    {
        IngredientRole.FryingOil,
        IngredientRole.AdoboOnionGarlic
    };

    public override IngredientRole[] PostBowlRoles => new[]
    {
        IngredientRole.AdoboSoySauce,
        IngredientRole.AdoboVinegar,
        IngredientRole.AdoboSugar,
        IngredientRole.AdoboLaurel
    };

    public override IngredientRole[] ConflictRoles => new[] { IngredientRole.CookingWater };

    public override string SeasoningNotification =>
        "Add soy sauce, vinegar, sugar, and laurel to the pan!";

    public override bool CanCook(GameObject o)         => o.GetComponent<IAdoboable>()?.CanBeAdobo()      ?? false;
    public override void StartCooking(GameObject o)    => o.GetComponent<IAdoboable>()?.StartAdobo();
    public override void StopCooking(GameObject o)     => o.GetComponent<IAdoboable>()?.StopAdobo();
    public override void CompleteCooking(GameObject o) => o.GetComponent<IAdoboable>()?.CompleteAdobo();
}
