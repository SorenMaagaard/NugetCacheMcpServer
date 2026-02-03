namespace TestLibrary;

// =============================================================================
// BASE TYPES AND INTERFACES
// =============================================================================

/// <summary>
/// Base class used for inheritance testing.
/// </summary>
public class BaseClass1
{
    /// <summary>
    /// A virtual method in the base class.
    /// </summary>
    public virtual void BaseMethod() { }
}

/// <summary>
/// Alternative base class for testing base class changes.
/// </summary>
public class BaseClass2
{
    /// <summary>
    /// A different base method.
    /// </summary>
    public virtual void DifferentBaseMethod() { }
}

/// <summary>
/// First test interface.
/// </summary>
public interface IInterface1
{
    /// <summary>
    /// Method from interface 1.
    /// </summary>
    void InterfaceMethod1();
}

/// <summary>
/// Second test interface that will be removed in V2.
/// </summary>
public interface IInterface2
{
    /// <summary>
    /// Method from interface 2.
    /// </summary>
    void InterfaceMethod2();
}

// =============================================================================
// TYPE REMOVAL TEST
// =============================================================================

/// <summary>
/// This class will be completely removed in V2.
/// Used to test type removal detection.
/// </summary>
public class ClassToRemove
{
    /// <summary>
    /// A method that will disappear with the class.
    /// </summary>
    public void DoSomething() { }
}

// =============================================================================
// MEMBER TYPE CHANGE TEST
// =============================================================================

/// <summary>
/// Tests detection of property and field type changes.
/// </summary>
public class ClassWithMemberTypeChange
{
    /// <summary>
    /// This property type will change from int to long in V2.
    /// </summary>
    public int IntProperty { get; set; }

    /// <summary>
    /// This nullable property will become non-nullable in V2.
    /// </summary>
    public int? NullableProperty { get; set; }

    /// <summary>
    /// This field type will change.
    /// </summary>
    public int IntField;
}

// =============================================================================
// MEMBER REMOVAL TEST
// =============================================================================

/// <summary>
/// Tests detection of removed members.
/// </summary>
public class ClassWithMemberRemoval
{
    /// <summary>
    /// This method will be removed in V2.
    /// </summary>
    public void MethodToRemove() { }

    /// <summary>
    /// This property will be removed in V2.
    /// </summary>
    public string PropertyToRemove { get; set; } = string.Empty;

    /// <summary>
    /// This field will be removed in V2.
    /// </summary>
    public int FieldToRemove;

    /// <summary>
    /// This method will remain in V2.
    /// </summary>
    public void MethodToKeep() { }
}

// =============================================================================
// METHOD SIGNATURE CHANGE TEST
// =============================================================================

/// <summary>
/// Tests detection of method signature changes.
/// </summary>
public class ClassWithMethodSignatureChange
{
    /// <summary>
    /// This method's parameter type will change.
    /// </summary>
    /// <param name="value">The input value.</param>
    public void MethodWithParamChange(int value) { }

    /// <summary>
    /// This method's return type will change.
    /// </summary>
    /// <returns>An integer value.</returns>
    public int MethodWithReturnChange() => 0;

    /// <summary>
    /// This method will have a parameter removed.
    /// </summary>
    /// <param name="a">First parameter.</param>
    /// <param name="b">Second parameter to be removed.</param>
    public void MethodWithParamRemoval(int a, int b) { }
}

// =============================================================================
// VIRTUAL METHOD REMOVAL TEST
// =============================================================================

/// <summary>
/// Tests detection of virtual modifier removal.
/// </summary>
public class ClassWithVirtualRemoval
{
    /// <summary>
    /// This virtual method will become non-virtual in V2.
    /// </summary>
    public virtual void VirtualMethod() { }

    /// <summary>
    /// This virtual method will remain virtual.
    /// </summary>
    public virtual void StillVirtualMethod() { }
}

// =============================================================================
// BASE CLASS CHANGE TEST
// =============================================================================

/// <summary>
/// This class's base class will change in V2.
/// </summary>
public class ClassWithBaseChange : BaseClass1
{
    /// <summary>
    /// Override of base method.
    /// </summary>
    public override void BaseMethod() { }
}

// =============================================================================
// INTERFACE REMOVAL TEST
// =============================================================================

/// <summary>
/// This class will lose IInterface2 in V2.
/// </summary>
public class ClassWithInterfaceRemoval : IInterface1, IInterface2
{
    /// <inheritdoc />
    public void InterfaceMethod1() { }

    /// <inheritdoc />
    public void InterfaceMethod2() { }
}

// =============================================================================
// SEALED/ABSTRACT ADDITION TEST
// =============================================================================

/// <summary>
/// This class will become sealed in V2.
/// </summary>
public class ClassToBeSealed
{
    /// <summary>
    /// A method in the soon-to-be-sealed class.
    /// </summary>
    public virtual void SomeMethod() { }
}

/// <summary>
/// This class will become abstract in V2.
/// </summary>
public class ClassToBeAbstract
{
    /// <summary>
    /// A method that will become abstract.
    /// </summary>
    public virtual void SomeMethod() { }
}

// =============================================================================
// GENERIC PARAMETER CHANGE TEST
// =============================================================================

/// <summary>
/// This generic class will have a type parameter added in V2.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public class GenericClass<T>
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Processes the value.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <returns>The processed value.</returns>
    public T? Process(T input) => input;
}

// =============================================================================
// ENUM VALUE CHANGE TEST
// =============================================================================

/// <summary>
/// This enum will have Value2 removed in V2.
/// </summary>
public enum EnumWithValueRemoval
{
    /// <summary>First value.</summary>
    Value1 = 0,
    /// <summary>Second value (will be removed).</summary>
    Value2 = 1,
    /// <summary>Third value.</summary>
    Value3 = 2
}

// =============================================================================
// OBSOLETE MEMBER TEST
// =============================================================================

/// <summary>
/// Tests detection of newly obsoleted members.
/// </summary>
public class ClassWithObsolete
{
    /// <summary>
    /// This method will be marked obsolete in V2.
    /// </summary>
    public void MethodToBeObsoleted() { }

    /// <summary>
    /// This method will remain non-obsolete.
    /// </summary>
    public void NormalMethod() { }
}

// =============================================================================
// OPTIONAL PARAMETER CHANGE TEST
// =============================================================================

/// <summary>
/// Tests detection of optional parameter changes.
/// </summary>
public class ClassWithDefaultChange
{
    /// <summary>
    /// This method's default value will change.
    /// </summary>
    /// <param name="value">Value with default.</param>
    public void MethodWithDefault(int value = 10) { }
}

// =============================================================================
// NEW MEMBERS TEST (non-breaking)
// =============================================================================

/// <summary>
/// Tests that new members are detected as non-breaking.
/// </summary>
public class ClassWithAdditions
{
    /// <summary>
    /// This method exists in both versions.
    /// </summary>
    public void ExistingMethod() { }
}

// =============================================================================
// STATIC CLASS TEST
// =============================================================================

/// <summary>
/// A static utility class for testing static class handling.
/// </summary>
public static class StaticUtilityClass
{
    /// <summary>
    /// A static method.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <returns>Processed string.</returns>
    public static string ProcessString(string input) => input.ToUpper();

    /// <summary>
    /// A constant value.
    /// </summary>
    public const int MaxValue = 100;
}

// =============================================================================
// DELEGATE TEST
// =============================================================================

/// <summary>
/// A delegate type for testing delegate handling.
/// </summary>
/// <param name="sender">The event sender.</param>
/// <param name="value">The value.</param>
public delegate void TestDelegate(object sender, int value);

// =============================================================================
// STRUCT TEST
// =============================================================================

/// <summary>
/// A struct for testing struct handling.
/// </summary>
public struct TestStruct
{
    /// <summary>
    /// The X coordinate.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// The Y coordinate.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Calculates the distance from origin.
    /// </summary>
    /// <returns>The distance.</returns>
    public double Distance() => Math.Sqrt(X * X + Y * Y);
}

// =============================================================================
// ABSTRACT CLASS TEST
// =============================================================================

/// <summary>
/// An abstract base class for testing abstract class handling.
/// </summary>
public abstract class AbstractService
{
    /// <summary>
    /// An abstract method that must be implemented.
    /// </summary>
    /// <returns>A result string.</returns>
    public abstract string Execute();

    /// <summary>
    /// A virtual method with a default implementation.
    /// </summary>
    /// <returns>The default value.</returns>
    public virtual int GetDefault() => 0;
}

// =============================================================================
// MEMBER FILTER TEST CLASSES
// =============================================================================

/// <summary>
/// Class for testing member name filters.
/// </summary>
public class ClassForMemberFilter
{
    /// <summary>Gets data from the source.</summary>
    public void GetData() { }

    /// <summary>Sets data to the destination.</summary>
    public void SetData() { }

    /// <summary>Processes the data.</summary>
    public void ProcessData() { }

    /// <summary>Validates the input.</summary>
    public void ValidateInput() { }
}

/// <summary>
/// Another class for testing type name filters.
/// </summary>
public class AnotherClassWithMembers
{
    /// <summary>Initializes the service.</summary>
    public void Initialize() { }

    /// <summary>Cleans up resources.</summary>
    public void Cleanup() { }
}
