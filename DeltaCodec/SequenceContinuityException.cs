using System;

namespace DeltaCodec
{
    /// <summary>
    /// Thrown when <see cref="DeltaDecoder"/>'s built-in sequence continuity check fails
    /// </summary>
    public class SequenceContinuityException : Exception
    {
        internal SequenceContinuityException(string expectedId, string actualId)
            : base($"Sequence continuity check failed - the provided id ({expectedId}) does not match the last preserved sequence id ({actualId})")
        {
        }
    }
}
