namespace LoggingApi
{
    /// <summary>
    /// Flags to filter which type memebers are included in a search.
    /// </summary>
    public enum SearchFlags
    {
        /// <summary>
        /// Default = Declared | Public
        /// </summary>
        Default = 0,
        /// <summary>
        /// Members the type inherited.
        /// </summary>
        Inherited = 1,
        /// <summary>
        /// Members the type declared.
        /// </summary>
        Declared = 2,
        /// <summary>
        /// Instance members of the type.
        /// </summary>
        Instance = 4,
        /// <summary>
        /// Static members of the type.
        /// </summary>
        Static = 8,
        /// <summary>
        /// Public members of the type.
        /// </summary>
        Public = 16,
        /// <summary>
        /// Non-Public members of the type.
        /// </summary>
        NonPublic = 32,
        /// <summary>
        /// The type's constructors.
        /// </summary>
        Constructor = 64,
        /// <summary>
        /// The type's methods.
        /// </summary>
        Method = 128,
        /// <summary>
        /// The type's operators.
        /// </summary>
        Operator = 256,
        /// <summary>
        /// The type's properties.
        /// </summary>
        Property = 512,
        /// <summary>
        /// All memebers of the type.
        /// </summary>
        All = 1023
    }
}
