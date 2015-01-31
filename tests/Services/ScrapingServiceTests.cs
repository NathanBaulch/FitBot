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
            var workout = workouts[0];
            Assert.That(workout.Id, Is.EqualTo(123));
            Assert.That(workout.Date, Is.EqualTo(new DateTime(2015, 1, 1)));
            Assert.That(workout.Points, Is.EqualTo(321));
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

        [Test]
        public void Points_With_Space_Thousands_Separator_Test()
        {
            const string html = @"<html>
  <div data-ag-type='workout'>
    <a data-item-id='1' />
    <a class='action_time gray_link'>2015-01-01</a>
    <span class='stream_total_points'>1 234 pts</span>
  </div>
</html>";
            var workouts = new ScrapingService().ExtractWorkouts(new MemoryStream(Encoding.UTF8.GetBytes(html)));

            Assert.That(workouts.Count, Is.EqualTo(1));
            Assert.That(workouts[0].Points, Is.EqualTo(1234));
        }

        [Test]
        public void Points_With_NonBreakingSpace_Thousands_Separator_Test()
        {
            const string html = @"<html>
  <div data-ag-type='workout'>
    <a data-item-id='1' />
    <a class='action_time gray_link'>2015-01-01</a>
    <span class='stream_total_points'>1" + "\xa0" + @"234 pts</span>
  </div>
</html>";
            var workouts = new ScrapingService().ExtractWorkouts(new MemoryStream(Encoding.UTF8.GetBytes(html)));

            Assert.That(workouts.Count, Is.EqualTo(1));
            Assert.That(workouts[0].Points, Is.EqualTo(1234));
        }

        [Test]
        public void Round_Values_To_Two_Decimal_Places_Test()
        {
            const string html = @"<html>
  <div data-ag-type='workout'>
    <a data-item-id='123' />
    <a class='action_time gray_link'>2015-01-01</a>
    <span class='stream_total_points'>4 pts</span>
    <ul class='action_detail'>
      <li>
        <div class='action_prompt'>Dummy</div>
        <li> 1 lb | 1 yd | 1 mph <span class='action_prompt_points'>1</span></li>
      </li>
    </ul>
  </div>
</html>";
            var workouts = new ScrapingService().ExtractWorkouts(new MemoryStream(Encoding.UTF8.GetBytes(html)));

            Assert.That(workouts.Count, Is.EqualTo(1));
            var workout = workouts[0];
            Assert.That(workout.Activities.Count, Is.EqualTo(1));
            var activity = workout.Activities[0];
            Assert.That(activity.Sets.Count, Is.EqualTo(1));
            var set = activity.Sets[0];
            Assert.That(set.Weight, Is.Not.Null);
            Assert.That(set.Distance, Is.Not.Null);
            Assert.That(set.Speed, Is.Not.Null);
            Assert.That(BitConverter.GetBytes(decimal.GetBits(set.Weight.Value)[3])[2], Is.EqualTo(2));
            Assert.That(BitConverter.GetBytes(decimal.GetBits(set.Distance.Value)[3])[2], Is.EqualTo(2));
            Assert.That(BitConverter.GetBytes(decimal.GetBits(set.Speed.Value)[3])[2], Is.EqualTo(2));
        }
    }
}