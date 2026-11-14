using System;
using NUnit.Framework;

namespace PatiDostum.Tests
{
    public class GameClockTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
        private const long Boot = 1_000_000; // ilk ölçümde telefon 1000 sn'dir açık

        private static ClockState Started()
        {
            var state = new ClockState();
            GameClock.Measure(state, T0, Boot);
            return state;
        }

        [Test]
        public void FirstMeasureReturnsZero()
        {
            var state = new ClockState();
            Assert.AreEqual(0.0, GameClock.Measure(state, T0, Boot));
            Assert.AreEqual(T0.Ticks, state.trustedUtcTicks);
        }

        [Test]
        public void SameBoot_ClockSetBack_StillCountsRealTime()
        {
            ClockState state = Started();
            double seconds = GameClock.Measure(state, T0.AddDays(-5), Boot + 3_600_000);
            Assert.AreEqual(3600.0, seconds, 0.001);
        }

        [Test]
        public void SameBoot_ClockSetForward_IsIgnored()
        {
            ClockState state = Started();
            double seconds = GameClock.Measure(state, T0.AddDays(10), Boot + 60_000);
            Assert.AreEqual(60.0, seconds, 0.001);
        }

        [Test]
        public void Reboot_UsesWallClock()
        {
            ClockState state = Started();
            double seconds = GameClock.Measure(state, T0.AddHours(5), 120_000);
            Assert.AreEqual(5 * 3600.0, seconds, 0.001);
        }

        [Test]
        public void Reboot_ClockSetBack_CountsAtLeastUptime()
        {
            ClockState state = Started();
            double seconds = GameClock.Measure(state, T0.AddDays(-2), 600_000);
            Assert.AreEqual(600.0, seconds, 0.001);
        }

        [Test]
        public void ForwardCheatInOneBoot_DoesNotLeakIntoNextBoot()
        {
            ClockState state = Started();
            double first = GameClock.Measure(state, T0.AddDays(10), Boot + 60_000);
            // Saat düzeltilip telefon yeniden başlatıldı: toplamda gerçekten 1 saat geçti
            double second = GameClock.Measure(state, T0.AddHours(1), 10_000);
            Assert.AreEqual(3600.0, first + second, 0.001);
        }

        [Test]
        public void CatchUp_IsCappedAt72Hours()
        {
            Assert.AreEqual(72 * 3600.0, GameClock.CatchUpSeconds(10 * 24 * 3600.0), 0.001);
            Assert.AreEqual(5.0, GameClock.CatchUpSeconds(5.0), 0.001);
            Assert.AreEqual(0.0, GameClock.CatchUpSeconds(-30.0), 0.001);
        }

        [Test]
        public void FastMode_IsFiveTimes()
        {
            Assert.AreEqual(3600.0, GameClock.GameSeconds(3600.0, SpeedMode.Real), 0.001);
            Assert.AreEqual(5 * 3600.0, GameClock.GameSeconds(3600.0, SpeedMode.Fast), 0.001);
        }

        [TestCase(22, true)]
        [TestCase(23, true)]
        [TestCase(3, true)]
        [TestCase(6, true)]
        [TestCase(7, false)]
        [TestCase(12, false)]
        [TestCase(21, false)]
        public void NightSlowsNeeds(int hour, bool night)
        {
            Assert.AreEqual(night, GameClock.IsNight(hour));
            Assert.AreEqual(night ? GameClock.NightNeedsFactor : 1.0, GameClock.NeedsFactor(hour), 0.0001);
        }
    }
}
