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
/// Second test interface - still exists in V2.
/// </summary>
public interface IInterface2
{
    /// <summary>
    /// Method from interface 2.
    /// </summary>
    void InterfaceMethod2();
}

// =============================================================================
// TYPE REMOVAL TEST - ClassToRemove is DELETED (breaking change)
// =============================================================================

// ClassToRemove has been intentionally removed to test type removal detection

// =============================================================================
// MEMBER TYPE CHANGE TEST - BREAKING CHANGES
// =============================================================================

/// <summary>
/// Tests detection of property and field type changes.
/// </summary>
public class ClassWithMemberTypeChange
{
    /// <summary>
    /// BREAKING: Property type changed from int to long.
    /// </summary>
    public long IntProperty { get; set; }

    /// <summary>
    /// BREAKING: Property changed from nullable to non-nullable.
    /// </summary>
    public int NullableProperty { get; set; }

    /// <summary>
    /// BREAKING: Field type changed from int to long.
    /// </summary>
    public long IntField;
}

// =============================================================================
// MEMBER REMOVAL TEST - BREAKING CHANGES
// =============================================================================

/// <summary>
/// Tests detection of removed members.
/// </summary>
public class ClassWithMemberRemoval
{
    // MethodToRemove has been removed (breaking change)
    // PropertyToRemove has been removed (breaking change)
    // FieldToRemove has been removed (breaking change)

    /// <summary>
    /// This method remains in V2.
    /// </summary>
    public void MethodToKeep() { }
}

// =============================================================================
// METHOD SIGNATURE CHANGE TEST - BREAKING CHANGES
// =============================================================================

/// <summary>
/// Tests detection of method signature changes.
/// </summary>
public class ClassWithMethodSignatureChange
{
    /// <summary>
    /// BREAKING: Parameter type changed from int to string.
    /// </summary>
    /// <param name="value">The input value (now string).</param>
    public void MethodWithParamChange(string value) { }

    /// <summary>
    /// BREAKING: Return type changed from int to string.
    /// </summary>
    /// <returns>A string value.</returns>
    public string MethodWithReturnChange() => string.Empty;

    /// <summary>
    /// BREAKING: Parameter b was removed.
    /// </summary>
    /// <param name="a">First parameter.</param>
    public void MethodWithParamRemoval(int a) { }
}

// =============================================================================
// VIRTUAL METHOD REMOVAL TEST - BREAKING CHANGE
// =============================================================================

/// <summary>
/// Tests detection of virtual modifier removal.
/// </summary>
public class ClassWithVirtualRemoval
{
    /// <summary>
    /// BREAKING: Virtual modifier removed.
    /// </summary>
    public void VirtualMethod() { }

    /// <summary>
    /// This method remains virtual.
    /// </summary>
    public virtual void StillVirtualMethod() { }
}

// =============================================================================
// BASE CLASS CHANGE TEST - BREAKING CHANGE
// =============================================================================

/// <summary>
/// BREAKING: Base class changed from BaseClass1 to BaseClass2.
/// </summary>
public class ClassWithBaseChange : BaseClass2
{
    /// <summary>
    /// Override of base method (different base now).
    /// </summary>
    public override void DifferentBaseMethod() { }
}

// =============================================================================
// INTERFACE REMOVAL TEST - BREAKING CHANGE
// =============================================================================

/// <summary>
/// BREAKING: IInterface2 has been removed.
/// </summary>
public class ClassWithInterfaceRemoval : IInterface1
{
    /// <inheritdoc />
    public void InterfaceMethod1() { }

    // InterfaceMethod2 is no longer required since IInterface2 is removed
}

// =============================================================================
// SEALED/ABSTRACT ADDITION TEST - BREAKING CHANGES
// =============================================================================

/// <summary>
/// BREAKING: Class is now sealed.
/// </summary>
public sealed class ClassToBeSealed
{
    /// <summary>
    /// Method is no longer virtual (sealed class).
    /// </summary>
    public void SomeMethod() { }
}

/// <summary>
/// BREAKING: Class is now abstract.
/// </summary>
public abstract class ClassToBeAbstract
{
    /// <summary>
    /// BREAKING: Method is now abstract.
    /// </summary>
    public abstract void SomeMethod();
}

// =============================================================================
// GENERIC PARAMETER CHANGE TEST - BREAKING CHANGE
// =============================================================================

/// <summary>
/// BREAKING: Added a second type parameter.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <typeparam name="TResult">The result type (new in V2).</typeparam>
public class GenericClass<T, TResult>
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Processes the value with a new return type.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <returns>The processed result.</returns>
    public TResult? Process(T input) => default;
}

// =============================================================================
// ENUM VALUE CHANGE TEST - BREAKING CHANGE
// =============================================================================

/// <summary>
/// BREAKING: Value2 has been removed.
/// </summary>
public enum EnumWithValueRemoval
{
    /// <summary>First value.</summary>
    Value1 = 0,
    // Value2 has been removed (breaking change)
    /// <summary>Third value.</summary>
    Value3 = 2,
    /// <summary>New value added in V2 (non-breaking).</summary>
    Value4 = 4
}

// =============================================================================
// OBSOLETE MEMBER TEST - NON-BREAKING
// =============================================================================

/// <summary>
/// Tests detection of newly obsoleted members.
/// </summary>
public class ClassWithObsolete
{
    /// <summary>
    /// NON-BREAKING: Method is now obsolete.
    /// </summary>
    [Obsolete("Use NormalMethod instead.")]
    public void MethodToBeObsoleted() { }

    /// <summary>
    /// This method remains non-obsolete.
    /// </summary>
    public void NormalMethod() { }
}

// =============================================================================
// OPTIONAL PARAMETER CHANGE TEST - POTENTIALLY BREAKING
// =============================================================================

/// <summary>
/// Tests detection of optional parameter changes.
/// </summary>
public class ClassWithDefaultChange
{
    /// <summary>
    /// Default value changed from 10 to 20.
    /// </summary>
    /// <param name="value">Value with new default.</param>
    public void MethodWithDefault(int value = 20) { }
}

// =============================================================================
// NEW MEMBERS TEST (non-breaking additions)
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

    /// <summary>
    /// NON-BREAKING: New method added in V2.
    /// </summary>
    public void NewMethodInV2() { }

    /// <summary>
    /// NON-BREAKING: New property added in V2.
    /// </summary>
    public string NewPropertyInV2 { get; set; } = string.Empty;
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

    /// <summary>
    /// NON-BREAKING: New static method in V2.
    /// </summary>
    /// <param name="input">Input to reverse.</param>
    /// <returns>Reversed string.</returns>
    public static string ReverseString(string input) =>
        new string(input.Reverse().ToArray());
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

/// <summary>
/// NON-BREAKING: New delegate added in V2.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="value">The value.</param>
/// <returns>The result.</returns>
public delegate T NewDelegate<T>(T value);

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

    /// <summary>
    /// NON-BREAKING: New method added in V2.
    /// </summary>
    /// <returns>String representation.</returns>
    public override string ToString() => $"({X}, {Y})";
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

    /// <summary>
    /// NON-BREAKING: New abstract method in V2.
    /// </summary>
    /// <returns>Configuration value.</returns>
    public abstract string GetConfiguration();
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

    /// <summary>NON-BREAKING: New method in V2.</summary>
    public void TransformData() { }
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

// =============================================================================
// NEW TYPES IN V2 (non-breaking additions)
// =============================================================================

/// <summary>
/// NON-BREAKING: New class added in V2.
/// </summary>
public class NewClassInV2
{
    /// <summary>
    /// A method in the new class.
    /// </summary>
    public void DoWork() { }
}

/// <summary>
/// NON-BREAKING: New interface added in V2.
/// </summary>
public interface INewInterface
{
    /// <summary>
    /// New interface method.
    /// </summary>
    void NewMethod();
}

/// <summary>
/// NON-BREAKING: New enum added in V2.
/// </summary>
public enum NewEnumInV2
{
    /// <summary>Option A.</summary>
    OptionA,
    /// <summary>Option B.</summary>
    OptionB,
    /// <summary>Option C.</summary>
    OptionC
}

/// <summary>
/// NON-BREAKING: New struct added in V2.
/// </summary>
public struct NewStructInV2
{
    /// <summary>The value.</summary>
    public int Value { get; set; }
}
