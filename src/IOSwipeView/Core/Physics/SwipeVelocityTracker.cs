using System.Diagnostics;

namespace IOSwipeView;

/// <summary>
/// Tracks drag velocity in pixels per second across pointer samples.
/// </summary>
/// <remarks>
/// <para>
/// IOGesture surfaces a <c>VelocityX</c>, but it is the raw pixel delta between two consecutive
/// pointer events with no division by elapsed time. That makes it frame-rate dependent — the same
/// physical gesture reports roughly half the value on a 120Hz screen as on a 60Hz one — so it
/// cannot be used to project where a flick would come to rest.
/// </para>
/// <para>
/// This tracker divides by measured elapsed time instead, giving a device-independent px/s figure.
/// It deliberately keeps only the last two samples: a longer window smooths away the flick at the
/// very end of the gesture, which is precisely the part that should decide the outcome.
/// </para>
/// </remarks>
public sealed class SwipeVelocityTracker
{
    private double _previousTranslation;
    private long _previousTimestamp;
    private double _currentTranslation;
    private long _currentTimestamp;
    private int _sampleCount;

    /// <summary>
    /// The current velocity in pixels per second, or <c>0</c> until two samples have been recorded.
    /// </summary>
    public double Velocity
    {
        get
        {
            if (_sampleCount < 2)
            {
                return 0;
            }

            var elapsed = Stopwatch.GetElapsedTime(_previousTimestamp, _currentTimestamp).TotalSeconds;

            // Two samples in the same tick carry no timing information to divide by.
            return elapsed <= 0 ? 0 : (_currentTranslation - _previousTranslation) / elapsed;
        }
    }

    /// <summary>
    /// Records a pointer sample, timestamped now.
    /// </summary>
    /// <param name="translation">Total distance dragged since the gesture began, in pixels.</param>
    public void Add(double translation) => Add(translation, Stopwatch.GetTimestamp());

    /// <summary>
    /// Records a pointer sample with an explicit timestamp. Intended for tests.
    /// </summary>
    /// <param name="translation">Total distance dragged since the gesture began, in pixels.</param>
    /// <param name="timestamp">A <see cref="Stopwatch"/> timestamp for the sample.</param>
    public void Add(double translation, long timestamp)
    {
        _previousTranslation = _currentTranslation;
        _previousTimestamp = _currentTimestamp;
        _currentTranslation = translation;
        _currentTimestamp = timestamp;

        if (_sampleCount < 2)
        {
            _sampleCount++;
        }
    }

    /// <summary>
    /// Clears all samples, ready for the next gesture.
    /// </summary>
    public void Reset()
    {
        _previousTranslation = 0;
        _previousTimestamp = 0;
        _currentTranslation = 0;
        _currentTimestamp = 0;
        _sampleCount = 0;
    }
}
