using UnityEngine;

[CreateAssetMenu(fileName = "SinigangMode", menuName = "Cooking/Modes/Sinigang")]
public class SinigangModeSO : CookingModeSO
{
    public override IngredientRole[] UnlockRoles => new[]
    {
        IngredientRole.CookingWater,
        IngredientRole.SinigangMix
    };

    public override IngredientRole[] ConflictRoles => new[] { IngredientRole.FryingOil };

    public override bool CanCook(GameObject o)         => o.GetComponent<ISinigangable>()?.CanBeSiniganged()      ?? false;
    public override void StartCooking(GameObject o)    => o.GetComponent<ISinigangable>()?.StartSiniganging();
    public override void StopCooking(GameObject o)     => o.GetComponent<ISinigangable>()?.StopSiniganging();
    public override void CompleteCooking(GameObject o) => o.GetComponent<ISinigangable>()?.CompleteSiniganging();
}
