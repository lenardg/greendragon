namespace GreenDragon.Core;

/// <summary>Represents a failure handled at the application's business boundary.</summary>
public class BusinessLogicException : Exception
{
    /// <summary>Creates a failure with a safe description.</summary>
    /// <param name="message">A description of the failed operation.</param>
    public BusinessLogicException(string message) : base(message)
    {
    }
}
