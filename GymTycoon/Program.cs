using GymTycoon.Code.Common;
using GymTycoon.Code.Data;

string test = "Maps\\Test.tmx";
// string occlusionTest = "Maps\\OcclusionTest.tmx";

Scenario scenario = new Scenario()
{
    Name = new ScopedName("Scenario.Default.Test"),
    Map = test,
    StartingCapital = 500,
    InfiniteMoney = false,
    StartDate = new System.DateOnly(2024, 1, 1),
    InitialGymRating = 600,
    InitialBoost = 200,
    InitialBoostDecayRate = 1f,
    MarketingDecayRate = 1f,
    NewEquipmentDecayRate = 1f,
};

using var game = GymTycoon.Code.GameFactory.CreateTiledMapGameInstance(scenario); 
game.Run();
