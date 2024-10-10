namespace Rule.Expressions.Tests.Steps
{
    using System;
    using System.Linq;
    using Data;
    using Evaluators;
    using FluentAssertions;
    using Models;
    using Newtonsoft.Json.Linq;
    using Reqnroll;

    [Binding]
    public class OperatorSteps
    {
        private readonly ScenarioContext scenarioContext;

        public OperatorSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        [Given(@"a context of type ""(.*)"" from json file ""(.*)""")]
        public void GivenAContextOfTypeFromJsonFile(string typeName, string jsonFileName)
        {
            this.scenarioContext.Set(typeName, "typeName");
            switch (typeName)
            {
                case "Location":
                    var location = new JsonFixtureFile(jsonFileName).JObjectOf<Location>();
                    this.scenarioContext.Set(location, "context");
                    break;
                case "TreeNode":
                    var treeNode = new JsonFixtureFile(jsonFileName).JObjectOf<TreeNode>();
                    this.scenarioContext.Set(treeNode, "context");
                    break;
                case "Person":
                    var person = new JsonFixtureFile(jsonFileName).JObjectOf<Person>();
                    this.scenarioContext.Set(person, "context");
                    break;
            }
        }

        [Given(@"an array of type ""(.*)"" from json file ""(.*)""")]
        public void GivenAnArrayOfTypeFromJsonFile(string typeName, string jsonFileName)
        {
            this.scenarioContext.Set(typeName, "typeName");
            switch (typeName)
            {
                case "Location":
                    var location = new JsonFixtureFile(jsonFileName).JObjectOf<Location[]>();
                    this.scenarioContext.Set(location, "context");
                    break;
                case "TreeNode":
                    var treeNode = new JsonFixtureFile(jsonFileName).JObjectOf<TreeNode[]>();
                    this.scenarioContext.Set(treeNode, "context");
                    break;
                case "Person":
                    var person = new JsonFixtureFile(jsonFileName).JObjectOf<Person[]>();
                    this.scenarioContext.Set(person, "context");
                    break;
            }
        }

        [When(@"I evaluate the rule expression")]
        public void WhenIEvaluateTheRuleExpression(Table table)
        {
            var left = table.Rows[0]["Field"];
            var op =(Operator) Enum.Parse(typeof(Operator), table.Rows[0]["Operator"], true);
            var right = table.Rows[0]["Value"];
            IConditionExpression expression = new LeafExpression()
            {
                Left = left,
                Operator = op,
                Right = right
            };
            this.scenarioContext.Set(expression);
        }

        [When(@"I evaluate context with JSON filter")]
        public void WhenIEvaluateContextWithJSONFilter(string jsonData)
        {
            IConditionExpression expression = ExpressionParser.Parse(JToken.Parse(jsonData));
            this.scenarioContext.Set(expression);
        }

        [Then(@"evaluation result should be ""(.*)""")]
        public void ThenEvaluationResultShouldBeTrue(string expected)
        {
            var expectedResult = bool.Parse(expected);
            var expression = this.scenarioContext.Get<IConditionExpression>();
            var evaluator = new ExpressionEvaluator();
            var actual = false;
            switch (this.scenarioContext.Get<string>("typeName"))
            {
                case "Location":
                    var location = this.scenarioContext.Get<Location>("context");
                    var lambda1 = evaluator.Evaluate<Location>(expression);
                    actual = lambda1(location);
                    break;
                case "TreeNode":
                    var treeNode = this.scenarioContext.Get<TreeNode>("context");
                    var lambda2 = evaluator.Evaluate<TreeNode>(expression);
                    actual = lambda2(treeNode);
                    break;
                case "Person":
                    var person = this.scenarioContext.Get<Person>("context");
                    var lambda3 = evaluator.Evaluate<Person>(expression);
                    actual = lambda3(person);
                    break;
            }

            actual.Should().Be(expectedResult);
        }

        [Then(@"collection evaluation result should be")]
        public void ThenCollectionEvaluationResultShouldBe(Table table)
        {
            var expectedFirstNames = table.Rows.Select(r => r[0].Trim()).ToList();
            var expression = this.scenarioContext.Get<IConditionExpression>();
            var evaluator = new ExpressionEvaluator();
            var people = this.scenarioContext.Get<Person[]>("context");
            var predicate = evaluator.Evaluate<Person>(expression);
            var actual = people.Where(p => predicate(p)).Select(p => p.FirstName).ToList();
            actual.Should().BeEquivalentTo(expectedFirstNames);
        }
    }
}