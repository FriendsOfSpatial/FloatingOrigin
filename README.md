Floating Origin Component
=========================

The Floating Origin component is meant to shift the X and Z coordinate's 
origin point (0,0) based on whether game objects are inside or outside of
a predetermined area or dimensions.

Verified on Spatial OS versions: `9.1.0`, `10.0.0`, `10.1.0`

Usage
-----

1. Add the Floating Origin component to a pre-existing GameObject in your 
   scene such as the GameEntry object.
2. Define which layers should be tracked by this component; it is recommended
   to place all SpatialOS EntityPrefabs on their own layer, for example 
   'entities', and use that to track which entities' their origin should be 
   moved.
3. On all locations where you pass coordinates to workers, first use the 
   `Unshift` method of the `Vector3` class to ensure you are using global 
   coordinates.
4. On all location where you receive coordinates from workers, first use the 
   `Shift` method of the `Coordinates` class to change the position to use the 
   shifted offset.

Tips
----

- Always use the Coordinates class to maintain your World Coordinates; this is
  a `double` and as such can manage larger world positions without suffering
  from floating point imprecisions.
- Always use Vector3 for your local coordinates in the Client or Worker where
  you use this component; the Vector3 class has an extension method to work 
  with this component and interacts seamlessly with Unity.