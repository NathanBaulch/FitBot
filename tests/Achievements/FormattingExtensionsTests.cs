using System;
using FitBot.Achievements;
using NUnit.Framework;

namespace FitBot.Test.Achievements
{
    [TestFixture]
    public class FormattingExtensionsTests
    {
        [Test]
        public void Distance_Test()
        {
            Assert.That(((decimal?) 123000).FormatDistance(), Is.EqualTo("123 km"));
            Assert.That(((decimal?) 123456).FormatDistance(), Is.EqualTo("123.5 km"));
        }

        [Test]
        public void Distance_Imperial_Test()
        {
            Assert.That(((decimal?) 123000).FormatDistance(true), Is.EqualTo("76.4 mi"));
            Assert.That(((decimal?) 123456).FormatDistance(true), Is.EqualTo("76.7 mi"));
        }

        [Test]
        public void Weight_Test()
        {
            Assert.That(((decimal?) 123).FormatWeight(), Is.EqualTo("123 kg"));
            Assert.That(((decimal?) 123.456).FormatWeight(), Is.EqualTo("123.5 kg"));
        }

        [Test]
        public void Weight_Imperial_Test()
        {
            Assert.That(((decimal?) 123).FormatWeight(true), Is.EqualTo("271.2 lb"));
            Assert.That(((decimal?) 123.456).FormatWeight(true), Is.EqualTo("272.2 lb"));
        }

        [Test]
        public void Speed_Test()
        {
            Assert.That(((decimal?) 100).FormatSpeed(), Is.EqualTo("360 km/h"));
            Assert.That(((decimal?) 123).FormatSpeed(), Is.EqualTo("442.8 km/h"));
        }

        [Test]
        public void Speed_Imperial_Test()
        {
            Assert.That(((decimal?) 100).FormatSpeed(true), Is.EqualTo("223.7 mph"));
            Assert.That(((decimal?) 123).FormatSpeed(true), Is.EqualTo("275.1 mph"));
        }

        [Test]
        public void Repetitions_Test()
        {
            Assert.That(((decimal?) 1).FormatRepetitions(), Is.EqualTo("1 rep"));
            Assert.That(((decimal?) 123).FormatRepetitions(), Is.EqualTo("123 reps"));
            Assert.That(((decimal?) 123.456).FormatRepetitions(), Is.EqualTo("123 reps"));
        }

        [Test]
        public void Duration_Test()
        {
            Assert.That(((decimal?) new TimeSpan(0, 0, 0, 1).TotalSeconds).FormatDuration(), Is.EqualTo("1 second"));
            Assert.That(((decimal?) new TimeSpan(0, 0, 0, 4).TotalSeconds).FormatDuration(), Is.EqualTo("4 seconds"));
            Assert.That(((decimal?) new TimeSpan(0, 0, 1, 0).TotalSeconds).FormatDuration(), Is.EqualTo("1 minute"));
            Assert.That(((decimal?) new TimeSpan(0, 0, 3, 0).TotalSeconds).FormatDuration(), Is.EqualTo("3 minutes"));
            Assert.That(((decimal?) new TimeSpan(0, 0, 3, 4).TotalSeconds).FormatDuration(), Is.EqualTo("3:04 minutes"));
            Assert.That(((decimal?) new TimeSpan(0, 1, 0, 0).TotalSeconds).FormatDuration(), Is.EqualTo("1 hour"));
            Assert.That(((decimal?) new TimeSpan(0, 2, 0, 0).TotalSeconds).FormatDuration(), Is.EqualTo("2 hours"));
            Assert.That(((decimal?) new TimeSpan(0, 2, 3, 0).TotalSeconds).FormatDuration(), Is.EqualTo("2:03 hours"));
            Assert.That(((decimal?) new TimeSpan(0, 2, 3, 4).TotalSeconds).FormatDuration(), Is.EqualTo("2:03:04 hours"));
            Assert.That(((decimal?) new TimeSpan(1, 2, 0, 0).TotalSeconds).FormatDuration(), Is.EqualTo("26 hours"));
            Assert.That(((decimal?) new TimeSpan(1, 2, 3, 0).TotalSeconds).FormatDuration(), Is.EqualTo("26:03 hours"));
            Assert.That(((decimal?) new TimeSpan(1, 2, 3, 4).TotalSeconds).FormatDuration(), Is.EqualTo("26:03:04 hours"));
            Assert.That(((decimal?) new TimeSpan(50, 0, 0, 0).TotalSeconds).FormatDuration(), Is.EqualTo("1,200 hours"));
        }
    }
}