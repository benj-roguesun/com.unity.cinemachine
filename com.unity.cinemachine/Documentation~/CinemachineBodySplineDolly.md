# Spline Dolly

This camera __Body__ algorithm restricts the Cinemachine Camera to move along a predefined spline. Use the __Camera Position__ property to specify where to put the Camera on the spline.

Enable __Automatic Dolly__ to move the camera to a position on the spline that is closest to the __Tracking Target__. When enabled, __Automatic Dolly__ moves the camera to the position on the spline that’s closest to the target.

**Tip**: Choose your spline shapes with care when using Automatic Dolly. This becomes problematic on splines that form an arc around some point.  As an extreme illustration, consider a perfectly circular spline with the __Tracking Target__ at the center. The closest point on the spline is unstable because all points on the circular spline are equally close to the target. In this situation, moving the __Tracking Target__ small distances can cause the camera to move large distances on the spline.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Spline__ || The spline that the camera moves along.  |
| __Camera Position__ || The position along the spline at which to place the camera. Animate this property directly or enable Automatic Dolly. The value is in the units specified by Position Units. |
| __Position Units__ || The unit of measure for Path Position.  |
| | _Knot_ | The value is the knot index. The value 0 represents the first knot on the spline, 1 is the second knot, and so on. Non-integer values represent points in between the knots |
| | _Distance_ | Distance along the spline, in normal distance units. 0 is the beginning of the spline. |
| | _Normalized_ | The value 0 represents the beginning of the spline, 1 is the end of the spline. |
| __Spline Offset__ || The position of the camera relative to the point on the spline. X is perpendicular to the spline, Y is up, and Z is parallel to the spline. Use this property to offset the camera from the spline itself. |
| __Camera Up__ || How to set the up vector for the camera. This affects the screen composition because the camera Aim algorithms try to respect the up direction. |
| | _Default_ | Do not modify the up direction of the camera. Instead, use the World Up Override property in Cinemachine Brain. |
| | _Path_ | Use the spline’s up vector at the current point. |
| | _Path No Roll_ | Use the spline’s up vector at the current point, but with the roll set to zero. |
| | _Follow Target_ | Use the up vector from the Tracking target’s transform. |
| | _Follow Target No Roll_ | Use the up vector from the Tracking target’s transform, but with the roll zeroed out. |
| __Auto Dolly__ || Controls how automatic dollying occurs. A Tracking target is necessary to use this feature. |
| | _Enabled_ | Enable the automatic dolly feature. Note: this can have some performance impact. |
| | _Position Offset_ | Offset, in position units, from the closest point on the spline to the tracking target. |
| __Damping__ || Controls how aggressively the camera moves to its desired point on the spline.  Smaller values produce a faster-moving camera, larger values produce a heavier, more slowly-moving camera. |
| | _Enabled_ | Enable the automatic dolly feature. Note: this can have some performance impact. |
| | _Position_ | How aggressively the camera tries to maintain the offset along the x, y, or z directions in spline local space. X represents the axis that is perpendicular to the spline. Use this to smooth out imperfections in the path. This may move the camera off the spline. Y represents the axis that is defined by the spline-local up direction. Use this to smooth out imperfections in the path. This may move the camera off the spline. Z represents the axis that is parallel to the spline. This won't move the camera off the spline. |
| | _Angular Damping_ | How to set the virtual camera's Up vector.  This will affect the screen composition, because the camera Aim behaviours will always try to respect the Up direction.  This is only used when the Camera Up is taking its value from the Tracking Target |
