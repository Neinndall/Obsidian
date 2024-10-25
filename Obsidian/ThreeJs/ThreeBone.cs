using System;
using System.Numerics;

namespace Obsidian.BabylonJs
{
    /// <summary>
    /// Represents a bone in the 3D model with transformation and binding information.
    /// </summary>
    public struct ThreeBone
    {
        /// <summary>
        /// Name of the bone.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Unique identifier for the bone.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identifier of the parent bone. -1 if this bone has no parent.
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// Local translation vector of the bone.
        /// </summary>
        public float[] LocalTranslation
        {
            get => _localTranslation;
            set
            {
                ValidateVectorArray(value, 3);
                _localTranslation = value;
            }
        }

        /// <summary>
        /// Local rotation quaternion of the bone.
        /// </summary>
        public float[] LocalRotation
        {
            get => _localRotation;
            set
            {
                ValidateQuaternionArray(value);
                _localRotation = value;
            }
        }

        /// <summary>
        /// Local scale vector of the bone.
        /// </summary>
        public float[] LocalScale
        {
            get => _localScale;
            set
            {
                ValidateVectorArray(value, 3);
                _localScale = value;
            }
        }

        /// <summary>
        /// Inverse bind translation vector of the bone.
        /// </summary>
        public float[] InverseBindTranslation
        {
            get => _inverseBindTranslation;
            set
            {
                ValidateVectorArray(value, 3);
                _inverseBindTranslation = value;
            }
        }

        /// <summary>
        /// Inverse bind rotation quaternion of the bone.
        /// </summary>
        public float[] InverseBindRotation
        {
            get => _inverseBindRotation;
            set
            {
                ValidateQuaternionArray(value);
                _inverseBindRotation = value;
            }
        }

        /// <summary>
        /// Inverse bind scale vector of the bone.
        /// </summary>
        public float[] InverseBindScale
        {
            get => _inverseBindScale;
            set
            {
                ValidateVectorArray(value, 3);
                _inverseBindScale = value;
            }
        }

        private float[] _localTranslation;
        private float[] _localRotation;
        private float[] _localScale;
        private float[] _inverseBindTranslation;
        private float[] _inverseBindRotation;
        private float[] _inverseBindScale;

        /// <summary>
        /// Validates that the vector array has the correct length (3 for vectors, 4 for quaternions).
        /// </summary>
        /// <param name="array">The array to validate.</param>
        /// <param name="expectedLength">The expected length of the array.</param>
        /// <exception cref="ArgumentException">Thrown when the array length is incorrect.</exception>
        private static void ValidateVectorArray(float[] array, int expectedLength)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (array.Length != expectedLength)
                throw new ArgumentException($"Array length must be {expectedLength}.", nameof(array));
        }

        /// <summary>
        /// Validates that the quaternion array has the correct length (4).
        /// </summary>
        /// <param name="array">The array to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the array length is incorrect.</exception>
        private static void ValidateQuaternionArray(float[] array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (array.Length != 4)
                throw new ArgumentException("Quaternion array length must be 4.", nameof(array));
        }
    }
}
