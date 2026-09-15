using NUnit.Framework;

namespace CozyGibberish.Tests
{
    public sealed class GibberishEventBusTests
    {
        [Test]
        public void Dispose_RemovesOnlyOwnedSubscription()
        {
            var eventBus = new GibberishEventBus();
            var firstCount = 0;
            var secondCount = 0;
            var first = eventBus.Subscribe<int>(value => firstCount += value);
            eventBus.Subscribe<int>(value => secondCount += value);

            eventBus.Publish(2);
            first.Dispose();
            eventBus.Publish(3);

            Assert.That(firstCount, Is.EqualTo(2));
            Assert.That(secondCount, Is.EqualTo(5));
        }
    }
}
