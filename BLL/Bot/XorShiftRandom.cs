/*===============================[ XorShiftPlus ]==============================
  ==-------------[ (c) 2018 R. Wildenhaus - Licensed under MIT ]-------------==
  ============================================================================= */
// http://codingha.us/2018/12/17/xorshift-fast-csharp-random-number-generator/

using System;

namespace Haus.Math
{
    /// <summary>
    ///   Generates pseudorandom primitive types with a 64-bit implementation
    ///   of the XorShift algorithm.
    /// </summary>
    public class XorShiftRandom
    {
        #region Data Members

        // State Fields
        private ulong x_;
        private ulong y_;

        // Buffer for optimized bit generation.
        private ulong buffer_;
        private ulong bufferMask_;

        #endregion

        #region Constructor

        /// <summary>
        ///   Constructs a new  generator using two
        ///   random Guid hash codes as a seed.
        /// </summary>
        public XorShiftRandom()
        {
            x_ = (ulong)Guid.NewGuid().GetHashCode();
            y_ = (ulong)Guid.NewGuid().GetHashCode();
        }

        /// <summary>
        ///   Constructs a new  generator
        ///   with the supplied seed.
        /// </summary>
        /// <param name="seed">
        ///   The seed value.
        /// </param>
        public XorShiftRandom(ulong seed)
        {
            x_ = seed << 3; y_ = seed >> 3;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Generates a pseudorandom byte.
        /// </summary>
        /// <returns>
        ///   A pseudorandom byte.
        /// </returns>
        public byte NextByte()
        {
            if (bufferMask_ >= 8)
            {
                byte _ = (byte)buffer_;
                buffer_ >>= 8;
                bufferMask_ >>= 8;
                return _;
            }

            ulong temp_x, temp_y;
            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            buffer_ = temp_y + y_;
            x_ = temp_x;
            y_ = temp_y;

            bufferMask_ = 0x8000000000000;
            return (byte)(buffer_ >>= 8);
        }

        /// <summary>
        ///   Generates a pseudorandom 32-bit unsigned integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 32-bit unsigned integer.
        /// </returns>
        public uint NextUInt32()
        {
            uint _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (uint)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        #endregion
    }
}