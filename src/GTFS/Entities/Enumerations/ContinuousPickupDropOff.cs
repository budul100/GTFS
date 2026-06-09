namespace GTFS.Entities.Enumerations
{
    /// <summary>
    /// Indicates whether a rider can board or alight a transit vehicle at any point
    /// along the vehicle's travel path.
    /// </summary>
    public enum ContinuousPickupDropOff
    {
        /// <summary>
        /// Continuous stopping pickup or drop off.
        /// </summary>
        Continuous = 0,

        /// <summary>
        /// No continuous stopping pickup or drop off.
        /// </summary>
        NoContinuous = 1,

        /// <summary>
        /// Must phone agency to arrange continuous pickup or drop off.
        /// </summary>
        PhoneAgency = 2,

        /// <summary>
        /// Must coordinate with driver to arrange continuous pickup or drop off.
        /// </summary>
        CoordinateWithDriver = 3
    }
}