This project corrected an assumption I held about how `WithComparer` and `Collect` interact.

```
someValuesProvider
    .WithComparer(...)
    .Collect()
```

Despite how it looks, and despite the fact that `WithComparer` receives an `IEqualityComparer<T>` rather than an `IEqualityComparer<ImmutableArray<T>>`, this *does not* compare individual values from the `IncrementalValuesProvider<T>` before aggregating them.

When `Collect` converts the `IncrementalValuesProvider<T>` to a `IncrementalValueProvider<ImmutableArray<T>>`, the new provider essentially inherits the old provider's comparer and uses it with `Enumerable.SequenceEqual` *after* aggregating the outputs of the old provider.

In other words, the result is exactly the same as if you wrote this and supplied an `IEqualityComparer<ImmutableArray<T>>` that uses `Enumerable.SequenceEqual`:

```
someValuesProvider
    .Collect()
    .WithComparer(...)
```

This violates the Principle of Least Astonishment in a big way, but it is what it is.