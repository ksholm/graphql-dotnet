using System.Threading.Tasks;
using GraphQL.Introspection;
using GraphQL.NewtonsoftJson;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Introspection
{
    public class SchemaIntrospectionTests
    {
        [Fact]
        public async Task validate_core_schema()
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = await documentExecuter.ExecuteAsync(_ =>
            {
                _.Schema = new Schema
                {
                    Query = new TestQuery()
                };
                _.Query = SchemaIntrospection.IntrospectionQuery;
            });

            var json = await new DocumentWriter(true).WriteToStringAsync(executionResult);

            ShouldBe(json, IntrospectionResult.Data);
        }

        public class TestQuery : ObjectGraphType
        {
            public TestQuery() => Name = "TestQuery";
        }

        [Fact]
        public async Task validate_non_null_schema()
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = await documentExecuter.ExecuteAsync(_ =>
            {
                _.Schema = new TestSchema();
                _.Query = InputObjectBugQuery;
            });

            var json = await new DocumentWriter(true).WriteToStringAsync(executionResult);
            executionResult.Errors.ShouldBeNull();

            ShouldBe(json, InputObjectBugResult);
        }

        private static void ShouldBe(string actual, string expected)
        {
            Assert.Equal(
                expected.Replace("\\r", "").Replace("\\n", ""),
                actual.Replace("\\r", "").Replace("\\n", ""),
                ignoreLineEndingDifferences: true,
                ignoreWhiteSpaceDifferences: true);
        }

        public static readonly string InputObjectBugQuery = @"
query test {
    __type(name:""SomeInput"") {
        inputFields {
            type {
                name,
                description
                ofType {
                    kind,
                    name
                }
            }
        }
    }
}";

        public static readonly string InputObjectBugResult = "{\r\n \"data\": {\r\n  \"__type\": {\r\n    \"inputFields\": [\r\n      {\r\n        \"type\": {\r\n          \"name\": \"String\",\r\n          \"description\": null,\r\n          \"ofType\": null\r\n        }\r\n      }\r\n    ]\r\n  }\r\n }\r\n}";

        public class SomeInputType : InputObjectGraphType
        {
            public SomeInputType()
                : base()
            {
                Name = "SomeInput";
                Description = "Input values for a patient's demographic information";

                Field<StringGraphType>("address");
            }
        }

        public class RootMutation : ObjectGraphType
        {
            public RootMutation()
            {
                Field<StringGraphType>(
                    "test",
                    arguments: new QueryArguments(new QueryArgument(typeof(SomeInputType)) { Name = "some" }));
            }
        }

        public class TestSchema : Schema
        {
            public TestSchema()
            {
                Mutation = new RootMutation();
            }
        }
    }
}
