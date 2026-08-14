using NUnit.Framework;

namespace SoftGames.Common.Tests
{
    public class FpsSamplerTests
    {
        [Test]
        public void ReadingHoldsUntilTheWindowCloses()
        {
            FpsSampler sampler = new(0.5f);

            for (int i = 0; i < 29; i++)
            {
                Assert.IsFalse(sampler.AddFrame(1f / 60f), $"Window closed early on frame {i}.");
            }

            Assert.AreEqual(0f, sampler.Fps);
        }

        [Test]
        public void ReportsTheRateOverTheWindow()
        {
            FpsSampler sampler = new(0.5f);

            for (int i = 0; i < 30; i++)
            {
                sampler.AddFrame(1f / 60f);
            }

            Assert.AreEqual(60f, sampler.Fps, 0.01f);
        }

        // A stutter must not follow the reading into the next window.
        [Test]
        public void WindowsDoNotCarryOver()
        {
            FpsSampler sampler = new(0.5f);
            sampler.AddFrame(0.5f);

            for (int i = 0; i < 30; i++)
            {
                sampler.AddFrame(1f / 60f);
            }

            Assert.AreEqual(60f, sampler.Fps, 0.01f);
        }

        [Test]
        public void NonPositiveIntervalFallsBackToHalfASecond()
        {
            FpsSampler sampler = new(0f);

            Assert.IsFalse(sampler.AddFrame(0.4f), "0.4s should not close a half second window.");
            Assert.IsTrue(sampler.AddFrame(0.1f), "0.5s should close it.");
        }
    }
}
