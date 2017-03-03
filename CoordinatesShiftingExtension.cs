using Improbable.Math;
using UnityEngine;

namespace FriendsOfSpatial.FloatingOrigin
{
    /// <summary>
    /// Extension class for the Vector3 and Coordinates class to shift from global
    /// Coordinates to a local Vector3.
    /// </summary>
    public static class CoordinatesShiftingExtension {

        /// <summary>
        /// Unshifts a local Vector3 back to global Coordinates.
        /// </summary>
        public static Coordinates Unshift(this Vector3 floatingPosition) {
            return new Coordinates(
                floatingPosition.x - FloatingOrigin.Origin.X,
                floatingPosition.y - FloatingOrigin.Origin.Y,
                floatingPosition.z - FloatingOrigin.Origin.Z
            );
        }

        /// <summary>
        /// Shifts the global Coordinates to a local Vector3 position.
        /// <para>
        /// Technically there could precision loss with the following but we assume that due
        /// to the way Floating Origins work that the Doubles below always fall in the range of
        /// a float.
        /// </para>
        /// </summary>
        public static Vector3 Shift(this Coordinates globalPosition) {
            return new Vector3(
                (float)(globalPosition.X + FloatingOrigin.Origin.X),
                (float)(globalPosition.Y + FloatingOrigin.Origin.Y),
                (float)(globalPosition.Z + FloatingOrigin.Origin.Z)
            );
        }
    }
}