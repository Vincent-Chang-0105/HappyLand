using UnityEngine;

[CreateAssetMenu(fileName = "NoodleMode", menuName = "Cooking/Modes/Noodle")]
public class NoodleModeSO : CookingModeSO
{
    public override IngredientRole[] UnlockRoles => new[]
    {
        IngredientRole.CookingWater,
        IngredientRole.NoodleIngredient
    };

    public override IngredientRole[] ConflictRoles => new[] { IngredientRole.FryingOil };

    public override bool CanCook(GameObject o)         => o.GetComponent<INoodleable>()?.CanBeNoodled()      ?? false;
    public override void StartCooking(GameObject o)    => o.GetComponent<INoodleable>()?.StartNoodling();
    public override void StopCooking(GameObject o)     => o.GetComponent<INoodleable>()?.StopNoodling();
    public override void CompleteCooking(GameObject o) => o.GetComponent<INoodleable>()?.CompleteNoodling();
}
