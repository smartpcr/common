# Rule Expression

## Expression Compiler

### null propagation

when right side is not checking empty or null (i.e. `isEmpty`, `isNull`), left side automatically added null check to reference target

### empty propagation

when calculate aggregate function (sum, avg, min and max), the target source cannot be null or empty

- expression
    ```txt
    LastReadings.Where(DataPoint,Contains,Pwr.kW tot).Average(Value)
    ```

- generated lambda expression:
    ```CLR
    .Block() {
        .If (
            .Call System.Linq.Enumerable.Where(
                $ctx.LastReadings,
                .Lambda #Lambda1<System.Func`2[DataCenterHealth.Models.Devices.ZenonLastReading,System.Boolean]>) != null && .Call System.Linq.Enumerable.Any(.Call System.Linq.Enumerable.Where(
                    $ctx.LastReadings,
                    .Lambda #Lambda1<System.Func`2[DataCenterHealth.Models.Devices.ZenonLastReading,System.Boolean]>))
        ) {
            .Return #Label1 { .Call System.Linq.Enumerable.Average(
                .Call System.Linq.Enumerable.Where(
                    $ctx.LastReadings,
                    .Lambda #Lambda1<System.Func`2[DataCenterHealth.Models.Devices.ZenonLastReading,System.Boolean]>),
                .Lambda #Lambda2<System.Func`2[DataCenterHealth.Models.Devices.ZenonLastReading,System.Double]>) }
        } .Else {
            .Return #Label1 { .Default(System.Double) }
        };
        .Label
            .Default(System.Double)
        .LabelTarget #Label1:
    }

    .Lambda #Lambda1<System.Func`2[DataCenterHealth.Models.Devices.ZenonLastReading,System.Boolean]>(DataCenterHealth.Models.Devices.ZenonLastReading $s)
    {
        .Call ($s.DataPoint).Contains("Pwr.kW tot")
    }

    .Lambda #Lambda2<System.Func`2[DataCenterHealth.Models.Devices.ZenonLastReading,System.Double]>(DataCenterHealth.Models.Devices.ZenonLastReading $p)
    {
        $p.Value
    }
    ```

- generated lambda expression (debug view):
    ```c#
    if ((ctx.LastReadings.Where(s => s.DataPoint.Contains("Pwr.kW tot")) != null) AndAlso ctx.LastReadings.Where(s => s.DataPoint.Contains("Pwr.kW tot")).Any()) {
        return ctx.LastReadings.Where(s => s.DataPoint.Contains("Pwr.kW tot")).Average(p => p.Value);
    } else {
        return default(Double);
    }
    ```

## How to add new operator

Here is an eample adding string operator `Matches` that uses regex `IsMatch` method:

1. add enum member to `Operator`
    ```c#
    namespace Rules.Expressions
    {
        public enum Operator
        {
            ...
            Matches,
            NotMatches,
            ...
        }
    }
    ```

    ```typescript
    // Filters.ts
    export enum Operator {
        // ...
        matches = "matches",
        notMatches = "notMatches",
        // ...
    }
    ```

2. create a class that extends `OperatorExpression` and create expresion we need
    ```c#
    namespace Rules.Expressions.Operators
    {
        using System;
        using System.Linq.Expressions;
        using System.Text.RegularExpressions;

        public class Matches : OperatorExpression
        {
            private const string methodName = "IsMatch";

            public Matches(Expression leftExpression, Expression rightExpression) : base(leftExpression, rightExpression)
            {
                if (leftExpression.Type != typeof(string) || rightExpression.Type != typeof(string))
                {
                    throw new InvalidOperationException($"both left side and right side must be type string for method '{methodName}'");
                }
            }

            public override Expression Create()
            {
                var regexOptionExpr = Expression.Constant(RegexOptions.IgnoreCase, typeof(RegexOptions));
                var regex = Expression.Call(
                    typeof(Regex),
                    methodName,
                    null,
                    LeftExpression,
                    RightExpression,
                    regexOptionExpr);
                return regex;
            }
        }
    }
    ```

3. use the newly created method within `LeafExpression.cs`, which is the place to build leaf expression
    ```c#
    namespace Rules.Expressions
    {
        public class LeafExpression : IConditionExpression
        {
            public Expression Process(ParameterExpression ctxExpression, Type parameterType)
            {
                ...
                switch (Operator)
                {
                    case Operator.Matches:
                        generatedExpression = new Matches(leftExpression, rightExpression).Create();
                        break;
                    case Operator.NotMatches:
                        generatedExpression = Expression.Not(new Matches(leftExpression, rightExpression).Create());
                        break;
                }
                ...
            }
        }
    }
    ```

4. update `ScoreExtension.cs` to decide how we calculate score with new operator
    ```c#
    namespace Rules.Expressions.Eval
    {
        public static class ScoreExtension
        {
            public static Func<T, double> GetScore<T>(this LeafExpression leafExpression) where T : class
            {
                ...
                if (targetExpression.Type == typeof(string))
                {
                    ...

                    double getScore(T t)
                    {
                        var actual = getValue(t);
                        switch (leafExpression.Operator)
                        {
                            ...
                            case Operator.Matches:
                                return Regex.IsMatch(actual, expectedString, RegexOptions.IgnoreCase) ? 1.0 : 0.0;
                            case Operator.NotMatches:
                                return !Regex.IsMatch(actual, expectedString, RegexOptions.IgnoreCase) ? 1.0 : 0.0;
                            ...
                        }
                    }

                    ...
                }
            }
        }
    }
    ```

5. update `PropertyPathBuilder.cs` to include newly added operator for `string` type, this is used in rule editor UI:
    ```c#
    namespace Rules.Expressions.Builders
    {
        public class PropertyPathBuilder<T>: IExpressionBuilder<T> where T: class
        {
            ...
            private List<string> GetOperatorsForType(Type type)
            {
                ...
                if (type == typeof(string))
                {
                    return new List<Operator>()
                    {
                        ...
                        Operator.Matches,
                        Operator.NotMatches,
                        ...
                    }.Select(op => op.ToString()).ToList();
                }
                ...
            }
            ...
        }
    }
    ```

6. add newly added operator to tests
- operators coverage: `Parser_feature.cs`
    ```c#
        [Scenario]
        ...
        [DataRow("DeviceType", "matches", "Breaker", false, Operator.Matches)]
        [DataRow("DeviceType", "notMatches", "Breaker", false, Operator.NotMatches)]
        ...
        public void Should_support_list_of_operators(
            string left,
            string actualOp,
            string right,
            bool rightIsExpr,
            Operator expectedOp,
            Type exType = null,
            string errorMessage = null)
        {
            ...
        }
    ```

- test match operator: `Evaluator_feature.cs`
    ```c#
    [Scenario]
    public void Verify_context_with_match_operator()
    {
        IConditionExpression filter = new AllOfExpression()
        {
            AllOf = new IConditionExpression[]
            {
                new LeafExpression()
                {
                    Left = "FirstName",
                    Operator = Operator.Matches,
                    Right = @"^dO[\w]+d$"
                }
            }
        };
        Runner.RunScenario(
            given => An_evaluation_context<Person>("donald_trump"),
            when => I_evaluate_context_with_filter(filter),
            then => Evaluation_results_should_be(true));

        var filter2 = new AllOfExpression()
        {
            AllOf = new IConditionExpression[]
            {
                new LeafExpression()
                {
                    Left = "LastName",
                    Operator = Operator.Matches,
                    Right = @"^d[\w]+d$"
                }
            }
        };
        Runner.RunScenario(
            given => An_evaluation_context<Person>("donald_trump"),
            when => I_evaluate_context_with_filter(filter2),
            then => Evaluation_results_should_be(false));
    }
    ```

## How to add new macro

Here is an eample to macro `NotInMaintenance`:

1. add enum member to `Operator`
    ```c#
    namespace Rules.Expressions
    {
        public enum Operator
        {
            ...
            NotInMaintenance,
            ...
        }
    }
    ```

    ```typescript
    // Filters.ts
    export enum Operator {
        // ...
        NotInMaintenance = "notInMaintenance",
        // ...
    }
    ```

2. create a class that extends `OperatorExpression` and create expresion we need
    ```c#
    namespace DataCenterHealth.Models.Devices.Macros
    {

        public static class QualityCheck
        {
            [MethodDescription("ensure device is not in maintenance mode")]
            public static bool NotInMaintenance(this PowerDevice device)
            {
                var isInMaint = device.LastReadings?.Where(r => r.IsInMaint == true).Any() == true;
                if (isInMaint)
                {
                    var maintenanceReason = device.LastReadings?.Where(r => r.IsInMaint == true).FirstOrDefault()?.MaintenanceReason;
                    device.AddEvaluationEvidence(new DeviceValidationEvidence()
                    {
                        Actual = maintenanceReason,
                        Expected = "!IsInMaint",
                        Score = 0,
                        Error = $"device is in maintenance mode",
                        Operator = "",
                        PropertyPath = "Device.LastReadings[].Quality"
                    });
                    return false;
                }

                return true;
            }
        }
    }
    ```

3. use the newly created method within `LeafExpression.cs`, which is the place to build leaf expression
    ```c#
    namespace Rules.Expressions
    {
        public class LeafExpression : IConditionExpression
        {
            public Expression Process(ParameterExpression ctxExpression, Type parameterType)
            {
                ...
                switch (Operator)
                {
                    case Operator.NotInMaintenance:
                    case Operator.AboveHierarchy:
                        // the following should be already added
                        var methodName = Operator.ToString();
                        var extensionMethod = leftExpression.Type.GetExtensionMethods().First(m => m.Name == methodName);
                        if (extensionMethod == null)
                        {
                            throw new InvalidOperationException($"operator not mapped to extension method: {methodName}");
                        }
                        var macroExpression = new MacroExpressionCreator(leftExpression, extensionMethod, OperatorArgs).CreateMacroExpression();
                        generatedExpression = Expression.Equal(macroExpression, rightExpression);
                        break;
                }
                ...
            }
        }
    }
    ```

4. when there is no argument for extension method, no need to specify arg count on `ConditionContextApi`

