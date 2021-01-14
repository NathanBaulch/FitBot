using FitBot.Model;
using FitBot.Services;
using NUnit.Framework;

namespace FitBot.Test.Services
{
    [TestFixture]
    public class ActivityHashServiceTests
    {
        [Test]
        public void NormalizeDoubles_Test()
        {
            var hasher = new ActivityHashService();
            var h1 = hasher.Hash(new[] {new Activity {Sets = new[] {new Set {Speed = 12.3M}}}});
            var h2 = hasher.Hash(new[] {new Activity {Sets = new[] {new Set {Speed = 12.3000M}}}});
            Assert.That(h1, Is.EqualTo(h2));
        }
    }
}