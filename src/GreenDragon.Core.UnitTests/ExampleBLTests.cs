using GreenDragon.Core.BusinessLogic;
using Shouldly;

namespace GreenDragon.Core.UnitTests;

public sealed class ExampleBLTests
{
    [Theory]
    [InlineData(0, 32)]
    [InlineData(100, 212)]
    [InlineData(-40, -40)]
    public void ConvertsValidTemperatures(double celsius, double expected)
    {
        new ExampleBL().ToFahrenheit(celsius).ShouldBe(expected);
    }

    [Fact]
    public void RejectsTemperaturesBelowAbsoluteZero()
    {
        Should.Throw<BusinessLogicException>(() => new ExampleBL().ToFahrenheit(-274));
    }
}
