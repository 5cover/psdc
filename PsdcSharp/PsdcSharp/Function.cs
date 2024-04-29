namespace Scover.Psdc;

internal static class Function
{
    /// <summary>
    /// Creates an action that executes the specified action for each item in the given collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="action">The action to be executed for each item.</param>
    /// <returns>An action that can be used to iterate over a collection and execute the specified action for each item.</returns>
    public static Action<IEnumerable<T>> Foreach<T>(this Action<T> action)
         => items => {
             foreach (var item in items) {
                 action(item);
             }
         };
}
