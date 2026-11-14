// Telefon açıldığından beri geçen süre (ms), uyku dahil. Kullanıcı saati değiştirse de etkilenmez.
// Assets/Scripts/Core/DeviceClock.cs çağırır.
#include <mach/mach_time.h>
#include <stdint.h>

int64_t PatiClock_ElapsedMs(void)
{
    static mach_timebase_info_data_t timebase;
    if (timebase.denom == 0) mach_timebase_info(&timebase);
    // mach_continuous_time uykuda da sayar (mach_absolute_time saymaz)
    uint64_t ticks = mach_continuous_time();
    unsigned __int128 nanos = (unsigned __int128)ticks * timebase.numer / timebase.denom;
    return (int64_t)(nanos / 1000000u);
}
