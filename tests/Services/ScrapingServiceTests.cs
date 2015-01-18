using System;
using System.IO;
using System.Text;
using FitBot.Services;
using NUnit.Framework;

namespace FitBot.Test.Services
{
    [TestFixture]
    public class ScrapingServiceTests
    {
        [Test]
        public void Empty_Test()
        {
            const string html = @"<html>
  <div data-ag-type='workout'>
  <a data-item-id='123' />
  <a class='action_time gray_link'>2015-01-01</a>
  <span class='stream_total_points'>321 pts</span>
  </div>
</html>";
            var workouts = new ScrapingService().ExtractWorkouts(new MemoryStream(Encoding.UTF8.GetBytes(html)));

            Assert.That(workouts.Count, Is.EqualTo(1));
            Assert.That(workouts[0].Id, Is.EqualTo(123));
            Assert.That(workouts[0].Date, Is.EqualTo(new DateTime(2015, 1, 1)));
            Assert.That(workouts[0].Points, Is.EqualTo(321));
        }

        [Test]
        public void Points_With_Dot_Thousands_Separator_Test()
        {
            const string html = @"<html>
  <div data-ag-type='workout'>
  <a data-item-id='1' />
  <a class='action_time gray_link'>2015-01-01</a>
  <span class='stream_total_points'>1.234 pts</span>
  </div>
</html>";
            var workouts = new ScrapingService().ExtractWorkouts(new MemoryStream(Encoding.UTF8.GetBytes(html)));

            Assert.That(workouts.Count, Is.EqualTo(1));
            Assert.That(workouts[0].Points, Is.EqualTo(1234));
        }
    }
}