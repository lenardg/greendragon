namespace GreenDragon.Core.BusinessLogic;

/// <summary>Provides the skill's independently testable conversion example.</summary>
public interface IExampleBL
{
    /// <summary>Converts a physically valid Celsius temperature to Fahrenheit.</summary>
    /// <param name="celsius">The input temperature.</param>
    /// <returns>The temperature in Fahrenheit.</returns>
    /// <exception cref="BusinessLogicException">The input is below absolute zero or not finite.</exception>
    double ToFahrenheit(double celsius);
}
