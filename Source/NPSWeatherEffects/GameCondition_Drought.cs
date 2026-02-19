using RimWorld;

namespace NPSWeather;

public class GameCondition_Drought : GameCondition
{
    public readonly FloodType floodOverride = FloodType.Low;
    public int tempAdjust = 10;
}