using System;

public static class TutorialEvents
{
    // Chicken events
    public static event Action OnChickenPickedUp;
    public static event Action OnChickenWashed;
    public static event Action<string> OnChickenEnteredBowl; // bowlTag

    // Cooking station events
    public static event Action<string> OnBowlDroppedOnStation; // stationType (Pot/Pan)
    public static event Action OnStirCompleted;
    public static event Action OnAllStirsCompleted;
    public static event Action OnTossCompleted;
    public static event Action OnAllTossesCompleted;

    // Ingredient events
    public static event Action OnOilAdded;
    public static event Action OnSinigangMixAdded;
    public static event Action OnSinigangCompleted;

    // Serving events
    public static event Action OnDishPlated;
    public static event Action OnDishServed;
    public static event Action OnCustomerArrived;
    public static event Action OnCustomerWaiting; // Customer is at slot and ready for order to be taken
    public static event Action OnOrderSlotClicked; // Player clicked on an order slot

    // Screen navigation events
    public static event Action OnWashScreenEntered;
    public static event Action OnFaucetOpened;
    public static event Action OnFaucetClosed;
    public static event Action OnCookScreenEntered;
    public static event Action OnServeScreenEntered;
    public static event Action OnBoilScreenEntered;
    public static event Action OnCutScreenEntered;

    // Progress events
    public static event Action OnThreeChickensWashed;

    // Transfer events
    public static event Action OnChickenTransferred;

    // Invoke methods
    public static void ChickenPickedUp() => OnChickenPickedUp?.Invoke();
    public static void ChickenWashed() => OnChickenWashed?.Invoke();
    public static void ChickenEnteredBowl(string bowlTag) => OnChickenEnteredBowl?.Invoke(bowlTag);
    public static void BowlDroppedOnStation(string stationType) => OnBowlDroppedOnStation?.Invoke(stationType);
    public static void StirCompleted() => OnStirCompleted?.Invoke();
    public static void AllStirsCompleted() => OnAllStirsCompleted?.Invoke();
    public static void TossCompleted() => OnTossCompleted?.Invoke();
    public static void AllTossesCompleted() => OnAllTossesCompleted?.Invoke();
    public static void OilAdded() => OnOilAdded?.Invoke();
    public static void SinigangMixAdded() => OnSinigangMixAdded?.Invoke();
    public static void SinigangCompleted() => OnSinigangCompleted?.Invoke();
    public static void DishPlated() => OnDishPlated?.Invoke();
    public static void DishServed() => OnDishServed?.Invoke();
    public static void CustomerArrived() => OnCustomerArrived?.Invoke();
    public static void CustomerWaiting() => OnCustomerWaiting?.Invoke();
    public static void OrderSlotClicked() => OnOrderSlotClicked?.Invoke();
    public static void WashScreenEntered() => OnWashScreenEntered?.Invoke();
    public static void FaucetOpened() => OnFaucetOpened?.Invoke();
    public static void FaucetClosed() => OnFaucetClosed?.Invoke();
    public static void CookScreenEntered() => OnCookScreenEntered?.Invoke();
    public static void ServeScreenEntered() => OnServeScreenEntered?.Invoke();
    public static void BoilScreenEntered() => OnBoilScreenEntered?.Invoke();
    public static void CutScreenEntered() => OnCutScreenEntered?.Invoke();
    public static void ThreeChickensWashed() => OnThreeChickensWashed?.Invoke();
    public static void ChickenTransferred() => OnChickenTransferred?.Invoke();
}
