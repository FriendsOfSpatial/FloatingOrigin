using UnityEngine;
using UnityEngine.Profiling;
using System.Linq;
using Improbable.Math;

namespace FriendsOfSpatial.FloatingOrigin {
    /// <summary>
	/// The Floating Origin script will try change the change the origin offset so
	/// that all game objects that are not contained within the dimensions fit with it.
	/// </summary>
	public class FloatingOrigin : MonoBehaviour {
		[Tooltip("The area in which all GameObjects should fall; the origin will shift to have all gameobjects within this area.")]
		public Vector2 Dimensions = new Vector2(10000, 10000);

        [Tooltip("Which layers to consider when shifting the origin; all other layers are ignored.")]
		public LayerMask Layers;

        [Tooltip("The offset of the World Origin from Unity's Origin")]
        public static Coordinates Origin = Coordinates.ZERO;

        [Tooltip("The number of frames to wait in between adjusting the origin")]
		public int Interval = 30;

        /// <summary>
        /// The number of frames that have occurred since the last repositioning of the origin.
        /// </summary>
		private int _intervalCounter;

        /// <summary>
        /// A cached array that can contain the particles of one particle system.
        /// <para>
        /// By keeping an array around with the largest amount of particles in the game you prevent
        /// performance drops due to array resizing.
        /// </para>
        /// </summary>
        ParticleSystem.Particle[] _parts;

        /// <summary>
        /// Reset the offset to prevent display errors in the inspector
        /// </summary>
		void OnEnable() {
			Origin = Coordinates.ZERO;
		}

        /// <summary>
        /// Reset the offset to prevent display errors in the inspector
        /// </summary>
		void OnDisable() {
			Origin = Coordinates.ZERO;
		}

        /// <summary>
        /// Perform the origin shift after other frame activities have happened and all
        /// game objects are in their respective places.
        /// </summary>
		void LateUpdate () {
			if (!IsTimeToShift()) {
				return;
			}

			Profiler.BeginSample("CalculateShift");
			Transform[] transforms = FindObjectsOfType<Transform>();
			Vector3 shift = CalculateShift(transforms);
			Profiler.EndSample();

			if (HasNotShifted(shift)) {
				return;
			}

			Profiler.BeginSample("PerformShift");
			MoveOrigin(shift);
			ShiftGameObjects(transforms, shift);
			Profiler.EndSample();
		}

        /// <summary>
        /// Moves the World Offset in the direction of the given vector.
        /// </summary>
		public void MoveOrigin(Vector3 shift) {
			Origin = new Coordinates(Origin.X + shift.x, Origin.Y + shift.y, Origin.Z + shift.z);
		}

        /// <summary>
        /// Determines whether a shift should be attempted again.
        /// </summary>
		bool IsTimeToShift() {
			if (++_intervalCounter < Interval) {
				return false;
			}
			_intervalCounter = 0;

			return true;
		}

        /// <summary>
        /// Check if a significant shift has occurred.
        /// </summary>
		bool HasNotShifted(Vector3 shift) {
			return Mathf.Approximately(shift.x, 0f)
               && Mathf.Approximately(shift.y, 0f)
               && Mathf.Approximately(shift.z, 0f);
		}

        /// <summary>
        /// Calculate the amount of shift may be applied by determining if any of the tracked
        /// game objects, including their children if they are in the right layer, are outside
        /// of the Dimensions of this origin rect.
        /// </summary>
		Vector3 CalculateShift(Transform[] transforms) {
			// Find transforms outside of the current dimension and determine how much they deviate
			float largestX = 0, largestZ = 0, smallestX = 0, smallestZ = 0;

			// TODO: Investigate whether linq's ForEach also causes memory leakage similar to Foreach
			transforms
				.Where(entityTransform => !In(entityTransform.position, Dimensions) && (!InLayerMask(entityTransform.gameObject.layer, Layers)))
				.ToList()
				.ForEach(transformChild => {
					largestX = Mathf.Max(transformChild.position.x - Dimensions.x / 2f, largestX);
					largestZ = Mathf.Max(transformChild.position.z - Dimensions.y / 2f, largestZ);
					smallestX = Mathf.Min(transformChild.position.x + Dimensions.x / 2f, smallestX);
					smallestZ = Mathf.Min(transformChild.position.z + Dimensions.y / 2f, smallestZ);
				});

			if (!Mathf.Approximately(largestX, 0) && !Mathf.Approximately(smallestX, 0)) {
				// TODO: balance the X's so that the origin is in the middle
				Debug.LogWarning("GameObjects exceed bounds on the X-axis when determining a new Floating Origin position, averaging");
			}
			if (!Mathf.Approximately(largestZ, 0) && !Mathf.Approximately(smallestZ, 0)) {
				// TODO: balance the Z's so that the origin is in the middle
				Debug.LogWarning("GameObjects exceed bounds on the Z-axis when determining a new Floating Origin position, averaging");
			}

			// calculate the shift; since the largest and smallest numbers are how much this exceeds the boundaries 
			// and never go beyond 0 we can add them and just see what the end result is
			// Y never shifts because the workers positioning is 2 dimensional
			return new Vector3(-(largestX + smallestX), 0, -(largestZ + smallestZ));
		}

        /// <summary>
        /// Perform the shift on all transforms that are tracked by this component and that do
        /// not have any parent. Child transforms automatically shift with their parents.
        /// </summary>
		void ShiftGameObjects(Transform[] transforms, Vector3 shift) {
			int transformCount = transforms.Count(); 
			for (int i = 0; i < transformCount; i++) {
				MoveTransformBy(transforms[i], shift);

			    var childParticleSystem = transforms[i].GetComponent<ParticleSystem>();
			    if (childParticleSystem != null) {
			        MoveParticlesBy(childParticleSystem, shift);
			    }
			}
		}

        /// <summary>
        /// Check if the given layer is contained in the given mask.
        /// </summary>
		bool InLayerMask(int layer, LayerMask mask) {
			return ((mask.value & (1 << layer)) > 0);
		}

        /// <summary>
        /// Moves a transform by the given shift if it is a top-level GameObject
        /// </summary>
		void MoveTransformBy(Transform t, Vector3 shift) {
			if (t.parent == null) {
	            t.position += shift;
	        }
		}

        /// <summary>
        /// Moves the particles of the given particle system with the given shift.
        /// </summary>
		void MoveParticlesBy(ParticleSystem sys, Vector3 shift) {
			int maxNumberOfParticles = sys.main.maxParticles;
			if (sys.main.simulationSpace != ParticleSystemSimulationSpace.World || maxNumberOfParticles <= 0) {
				return;
			}
	 
			bool wasPlaying = sys.isPlaying;
			if (!sys.isPaused) {
				sys.Pause ();
			}
	 
			ResizeParticleBufferWhenNeeded(maxNumberOfParticles);
	 
			int numberOfParticles = sys.GetParticles(_parts);
			for (int i = 0; i < numberOfParticles; i++) {
				 _parts[i].position += shift;
			}
			sys.SetParticles(_parts, numberOfParticles);
	 
			if (wasPlaying) sys.Play ();
		}

        /// <summary>
        /// Resizes the particles buffer of this object if the number of particles exceed its bounds.
        /// </summary>
		void ResizeParticleBufferWhenNeeded(int particlesNeeded) {
			if (_parts == null || _parts.Length < particlesNeeded) {
				_parts = new ParticleSystem.Particle[particlesNeeded];
			}
		}

        /// <summary>
        /// Show the covered dimensions in the editor.
        /// </summary>
		void OnDrawGizmos() {
			Gizmos.color = new Color(1f, 1f, 1f, .1f);
			Gizmos.DrawCube(Vector3.zero, new Vector3(Dimensions.x, 1, Dimensions.y));
		}

        /// <summary>
        /// Checks if the given point's X/Z position lies within the given dimensions X/Y plane.
        /// <para>
        /// This method looks a bit weird but that has to do with the translation of a three dimensional
        /// point to a flat two dimensional plane. This floating origin script ignores the Y axis because
        /// workers ignore it.
        /// </para>
        /// </summary>
		bool In(Vector3 point, Vector2 dimensions) {
			return point.x > -(dimensions.x / 2f) 
				&& point.x < (dimensions.x / 2f) 
				&& point.z > -(dimensions.y / 2f)
				&& point.z < (dimensions.y / 2f);
		}
	}

}