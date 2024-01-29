using circuitbreaker.library.helpers;
using System.Linq;
using FluentAssertions;

namespace circuitbreaker.library.tests.helpers;

public class FixedSizeQueueTests {

    [Fact]
    public void When_EnqueueItem_Should_AppearInsideTheQueue(){
        var queue = new FixedSizeQueue<int>(2);

        queue.Enqueue(1);
        queue.Count.Should().Be(1);
        queue.First().Should().Be(1);
    }

    [Fact]
    public void When_EnqueueItemsUpToFull_Should_ReturnFull(){
        var queue = new FixedSizeQueue<int>(2);

        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Count.Should().Be(2);
        queue.Full.Should().BeTrue();
    }

    [Fact]
    public void When_EnqueueItemsAboveSize_Should_RemoveItemsAndMaintainLatestItems(){
        var queue = new FixedSizeQueue<int>(2);

        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);
        queue.Enqueue(4);

        queue.Count.Should().Be(2);
        queue.Full.Should().BeTrue();
        queue.Any(x => x == 1).Should().BeFalse();
        queue.Any(x => x == 2).Should().BeFalse();
    }
}