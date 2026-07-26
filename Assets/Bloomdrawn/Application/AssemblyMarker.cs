namespace Bloomdrawn.Application
{
    /// <summary>
    /// Marks the application boundary. Unity-facing adapters are introduced only when a task requires them.
    /// </summary>
    public static class ApplicationAssemblyMarker
    {
        public const string Name = "Bloomdrawn.Application";
    }
}
