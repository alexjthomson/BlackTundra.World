# TARGETTING
A set of scripts that assist in targetting objects.

# IMPLEMENTATION
A custom set of flags should be defined as an enum:
```csharp
[Flags]
public enum TargettingFlags : int {
	None  = 0,
	Flag1 = 1,
	Flag2 = 2,
	Flag3 = 4,
	Flag4 = 8
}
```
The value for each flag should only contain one bit. These can be combied and used by `ITargetable` instances.

To make an object targetable, the object must implement `ITargetable`. The object should call `Register()` to register as a targetable object. It should call `Deregsiter()` before destruction to unregister as a targetable object.