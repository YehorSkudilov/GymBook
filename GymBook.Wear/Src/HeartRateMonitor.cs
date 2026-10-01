using Android.Hardware;
using Android.Runtime;

namespace GymBook.Wear;

/// <summary>
/// The watch's heart rate sensor, read through Android's sensor API while a workout is on screen. Needs the body sensors
/// permission, asked for the first time it starts.
/// </summary>
public class HeartRateMonitor : Java.Lang.Object, ISensorEventListener
{
    SensorManager? _sensors;
    bool _running;

    /// <summary>Beats per minute, raised on the main thread whenever the sensor has a reading.</summary>
    public event Action<int>? Changed;

    /// <summary>Starts reading. False when there's no sensor or the permission was refused.</summary>
    public async Task<bool> StartAsync()
    {
        if (_running)
            return true;
        // Turned off in Settings, or not allowed: no heart rate.
        if (!WatchSettings.HeartRate || await Permissions.RequestAsync<HeartRatePermission>() != PermissionStatus.Granted)
            return false;
        _sensors ??= (SensorManager?)Android.App.Application.Context.GetSystemService(Android.Content.Context.SensorService);
        if (_sensors?.GetDefaultSensor(SensorType.HeartRate) is not { } sensor)
            return false;
        _running = _sensors.RegisterListener(this, sensor, SensorDelay.Normal);
        return _running;
    }

    public void Stop()
    {
        if (!_running)
            return;
        _sensors?.UnregisterListener(this);
        _running = false;
    }

    public void OnSensorChanged(SensorEvent? e)
    {
        // 0 while the sensor is still finding a pulse.
        if (e?.Values is { Count: > 0 } values && values[0] > 0)
        {
            var bpm = (int)Math.Round(values[0]);
            MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke(bpm));
        }
    }

    public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
    {
    }
}

/// <summary>
/// Reading the heart rate: BODY_SENSORS up to Wear OS 5; from Android 16 (API 36, Wear OS 6) it's the finer
/// health.READ_HEART_RATE instead, and BODY_SENSORS is no longer granted.
/// </summary>
public class HeartRatePermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        OperatingSystem.IsAndroidVersionAtLeast(36)
            ? [("android.permission.health.READ_HEART_RATE", true)]
            : [(Android.Manifest.Permission.BodySensors, true)];
}
