namespace Shared.Domain.Primitives;

// A Value Object is defined by its VALUES, not an identity.
// Two Money(100, "USD") objects are equal even if they are different instances.
// Examples in our app: Money, Email, Address, Coordinates.
//
// Why use value objects?
// - They encapsulate validation (Email can't be constructed with an invalid format)
// - They make the domain model expressive: Property.Price is Money, not decimal
// - They are immutable — once created, they cannot change (you create a new one instead)
public abstract class ValueObject
{
    // Each subclass defines which properties make up its equality.
    // Example: Money returns [Amount, Currency]
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        return GetEqualityComponents()
            .SequenceEqual(((ValueObject)obj).GetEqualityComponents());
    }

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(0, (hash, component) =>
                HashCode.Combine(hash, component?.GetHashCode() ?? 0));

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is not null && right is not null && left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}
