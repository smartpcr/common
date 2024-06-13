// -----------------------------------------------------------------------
// <copyright file="RuleEvaluatorSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Tests.Steps
{
    using System.Linq;
    using FluentAssertions;
    using Models;
    using Reqnroll;

    [Binding]
    public class PersonSteps
    {
        private readonly ScenarioContext scenarioContext;

        public PersonSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        [Given(@"a person")]
        public void GivenAPerson(Table table)
        {
            var person = table.CreateInstance<Person>();
            scenarioContext.Set(person, "me");
        }

        [Given(@"his wife")]
        public void GivenHisWife(Table table)
        {
            var wife = table.CreateInstance<Person>();
            var me = scenarioContext.Get<Person>("me");
            me.Spouse = wife;
        }

        [Given(@"their children")]
        public void GivenTheirChildren(Table table)
        {
            var children = table.CreateSet<Person>();
            var me = scenarioContext.Get<Person>("me");
            me.Children = children.ToArray();
        }

        [When(@"evaluate int value with following property path ""(.*)""")]
        public void WhenEvaluateTheFollowingPropertyPath(string propPath)
        {
            var func = Evaluator.Evaluate<Person, int>(propPath);
            var me = scenarioContext.Get<Person>("me");
            var age = func(me);
            scenarioContext.Set(age, "actual");
        }

        [Then(@"the result should be (.*)")]
        public void ThenTheResultShouldBe(int expected)
        {
            var actual = scenarioContext.Get<int>("actual");
            actual.Should().Be(expected);
        }

        [Then(@"person should be")]
        public void ThenPersonShouldBe(Table table)
        {
            var me = scenarioContext.Get<Person>("me");
            var expected = table.CreateInstance<Person>();
            me.FirstName.Should().Be(expected.FirstName);
            me.LastName.Should().Be(expected.LastName);
            me.Age.Should().Be(expected.Age);
        }
    }
}