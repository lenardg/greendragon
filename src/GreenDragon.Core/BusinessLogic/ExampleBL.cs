namespace GreenDragon.Core.BusinessLogic;

/// <inheritdoc />
public sealed class ExampleBL : IExampleBL
{
    /// <inheritdoc />
    public double ToFahrenheit(double celsius)
    {
        if (!double.IsFinite(celsius) || celsius < BusinessRules.Example.AbsoluteZeroCelsius)
        {
            throw new BusinessLogicException("The temperature must be finite and at or above absolute zero.");
        }
        return celsius * BusinessRules.Example.FahrenheitScale + BusinessRules.Example.FahrenheitOffset;
    }
}
