// -----------------------------------------------------------------------
// <copyright file="CircularBufferSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache.Tests.Steps;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

[Binding]
public class CircularBufferSteps
{
    private readonly ScenarioContext scenarioContext;

    public CircularBufferSteps(ScenarioContext scenarioContext)
    {
        this.scenarioContext = scenarioContext;
    }

    [Given(@"capacity of (.*)")]
    public void GivenCapacityOf(int capacity)
    {
        IBoundedQueue<string> circularBuffer = new CircularBuffer<string>(capacity);
        scenarioContext.Set(circularBuffer);
    }

    [When(@"add (.*) message")]
    public async Task GivenAddMessage(int total)
    {
        var circularBuffer = scenarioContext.Get<IBoundedQueue<string>>();
        for (var i = 0; i < total; i++)
        {
            await circularBuffer.WriteAsync($"message {i}");
        }
    }

    [When(@"perform the following iterations")]
    public async Task WhenPerformTheFollowingIterations(Table table)
    {
        var iterations = table.CreateSet<CircularBufferIteration>();
        var circularBuffer = scenarioContext.Get<IBoundedQueue<string>>();
        foreach (var iteration in iterations.OrderBy(it => it.Step))
        {
            for (var i = 0; i < Math.Max(iteration.ProducerRate, iteration.ConsumerRate); i++)
            {
                if (i < iteration.ProducerRate)
                {
                    await circularBuffer.WriteAsync(iteration.Message);
                }

                if (i < iteration.ConsumerRate)
                {
                    await circularBuffer.TakeAsync(1);
                }
            }
        }
    }

    [Then(@"buffer count should be (.*)")]
    public void ThenBufferCountShouldBe(int expectedCount)
    {
        var circularBuffer = scenarioContext.Get<IBoundedQueue<string>>();
        circularBuffer.Count.Should().Be(expectedCount);
    }

    [Then(@"dropped message count should be (.*)")]
    public void ThenDroppedMessageCountShouldBe(int expectedDropped)
    {
        var circularBuffer = scenarioContext.Get<IBoundedQueue<string>>();
        circularBuffer.TotalDropped.Should().Be(expectedDropped);
    }

    [Then(@"the messages in buffer should be")]
    public async Task ThenTheMessagesInBufferShouldBe(Table table)
    {
        var expectedMessages = table.CreateSet<ExpectedMessagesInBuffer>();
        var expectedMessagesInBuffers = expectedMessages.ToList();
        var expectedCount = expectedMessagesInBuffers.Sum(m => m.Count);
        var circularBuffer = scenarioContext.Get<IBoundedQueue<string>>();
        circularBuffer.Count.Should().Be(expectedCount);
        var messages = await circularBuffer.TakeAsync(expectedCount);
        var messageList = messages.ToList();
        messageList.Count.Should().Be(expectedCount);
        foreach (var expectedMsgCount in expectedMessagesInBuffers)
        {
            var foundList = messageList.Where(m => m != null && m.Equals(expectedMsgCount.Message, StringComparison.OrdinalIgnoreCase)).ToList();
            foundList.Count.Should().Be(expectedMsgCount.Count);
        }
    }
}

public class CircularBufferIteration
{
    public int Step { get; set; }
    public int ProducerRate { get; set; }
    public int ConsumerRate { get; set; }
    public string Message { get; set; }
}

public class ExpectedMessagesInBuffer
{
    public string Message { get; set; }
    public int Count { get; set; }
}