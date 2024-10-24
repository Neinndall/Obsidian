using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obsidian.ThreeJs
{
    /// <summary>
    /// Represents a complete animation with multiple clips for different joints.
    /// </summary>
    public struct ThreeAnimation
    {
        /// <summary>
        /// Name of the animation.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Dictionary of animation clips for different joints.
        /// </summary>
        public Dictionary<string, ThreeAnimationClip> Clips { get; set; }
    }

    /// <summary>
    /// Represents an animation clip for a specific joint.
    /// </summary>
    public struct ThreeAnimationClip
    {
        /// <summary>
        /// Name of the joint this clip is for.
        /// </summary>
        public string JointName { get; set; }

        /// <summary>
        /// Track of translations for the joint.
        /// </summary>
        public ThreeAnimationTrack Translations { get; set; }

        /// <summary>
        /// Track of rotations for the joint.
        /// </summary>
        public ThreeAnimationTrack Rotations { get; set; }

        /// <summary>
        /// Track of scales for the joint.
        /// </summary>
        public ThreeAnimationTrack Scales { get; set; }
    }

    /// <summary>
    /// Represents a track of animation data over time.
    /// </summary>
    public struct ThreeAnimationTrack
    {
        private float[] _keyTimes;
        private float[] _values;

        /// <summary>
        /// Times at which the keyframe values are defined.
        /// </summary>
        public float[] KeyTimes
        {
            get => _keyTimes;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _keyTimes = value;
            }
        }

        /// <summary>
        /// Values corresponding to the keyframe times.
        /// </summary>
        public float[] Values
        {
            get => _values;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value.Length != _keyTimes.Length) throw new ArgumentException("KeyTimes and Values must have the same length.");
                _values = value;
            }
        }

        /// <summary>
        /// Interpolates between keyframe values.
        /// </summary>
        /// <param name="t">The time to interpolate at.</param>
        /// <returns>The interpolated value.</returns>
        public float Interpolate(float t)
        {
            if (_keyTimes.Length == 0) return 0f;

            // Find the interval
            int index = Array.BinarySearch(_keyTimes, t);
            if (index >= 0) return _values[index];
            index = ~index;

            if (index == 0) return _values[0];
            if (index >= _keyTimes.Length) return _values[_values.Length - 1];

            // Linear interpolation
            float t0 = _keyTimes[index - 1];
            float t1 = _keyTimes[index];
            float v0 = _values[index - 1];
            float v1 = _values[index];
            return v0 + (v1 - v0) * (t - t0) / (t1 - t0);
        }
    }
}