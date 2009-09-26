﻿using System;
using System.Collections.Generic;

using Cuke4Nuke.Core;
using Cuke4Nuke.Framework;

using LitJson;

using NUnit.Framework;
using System.Text;
using System.Reflection;

namespace Cuke4Nuke.Specifications.Core
{
    [TestFixture]
    public class Processor_Specification
    {
        StepDefinition _stepDefinition;
        StepDefinition _exceptionDefinition;
        StepDefinition _stepDefinitionWithOneStringParameter;
        StepDefinition _stepDefinitionWithMultipleStringParameters;
        StepDefinition _stepDefinitionWithOneIntParameter;
        StepDefinition _stepDefinitionWithOneDoubleParameter;
        StepDefinition _stepDefinitionWithIntDoubleAndStringParameters;

        List<StepDefinition> _stepDefinitions;

        Processor _processor;

        static bool _methodCalled;
        static object[] _receivedParameters; // Methods that take parameters should copy the parameter values into this array, so that the test can verify the values
        
        [SetUp]
        public void SetUp()
        {
            _stepDefinition = new StepDefinition(Reflection.GetMethod(GetType(), "Method"));
            _exceptionDefinition = new StepDefinition(Reflection.GetMethod(GetType(), "ThrowsException"));
            _stepDefinitionWithOneStringParameter = new StepDefinition(GetType().GetMethod("OneStringParameter"));
            _stepDefinitionWithMultipleStringParameters = new StepDefinition(GetType().GetMethod("MultipleStringParameters", new Type[] { typeof(string), typeof(string) }));
            _stepDefinitionWithOneIntParameter = new StepDefinition(GetType().GetMethod("OneIntParameter"));
            _stepDefinitionWithOneDoubleParameter = new StepDefinition(GetType().GetMethod("OneDoubleParameter"));
            _stepDefinitionWithIntDoubleAndStringParameters = new StepDefinition(GetType().GetMethod("IntDoubleAndStringParameters"));
            _stepDefinitions = new List<StepDefinition> { _stepDefinition, _exceptionDefinition, _stepDefinitionWithOneStringParameter, _stepDefinitionWithMultipleStringParameters, _stepDefinitionWithOneIntParameter, _stepDefinitionWithOneDoubleParameter, _stepDefinitionWithIntDoubleAndStringParameters };

            var loader = new MockLoader(_stepDefinitions);
            _processor = new Processor(loader);

            _methodCalled = false;
            _receivedParameters = null;
        }

        [Test]
        public void List_step_definitions_should_return_a_json_formatted_list()
        {
            var response = _processor.Process("list_step_definitions");

            Assert.That(response, Is.EqualTo(new Formatter().Format(_stepDefinitions)));
        }

        [Test]
        public void Invoke_with_a_valid_id_should_invoke_step_definition_method()
        {
            var request = CreateInvokeRequest(_stepDefinition.Id);
            var response = _processor.Process(request);

            AssertOkResponse(response);
            Assert.That(_methodCalled, Is.True);
        }

        [Test]
        public void Invoke_with_a_missing_id_should_return_failed_response()
        {
            var response = _processor.Process(@"invoke:{ }");

            AssertFailResponse(response, "Missing 'id' in request");
        }

        [Test]
        public void Invoke_with_malformed_json_should_return_failed_response()
        {
            var response = _processor.Process(@"invoke:{a}");

            AssertFailResponse(response, "Invalid json in request 'invoke:{a}': Invalid character 'a' in input string");
        }

        [Test]
        public void Invoke_with_an_invalid_id_should_return_failed_response()
        {
            var request = CreateInvokeRequest("invalid_id");

            var response = _processor.Process(request);

            AssertFailResponse(response, "Could not find step with id 'invalid_id'");
        }

        [Test]
        public void Invoke_with_a_method_throws_should_return_failed_response()
        {
            var request = CreateInvokeRequest(_exceptionDefinition.Id);

            var response = _processor.Process(request);

            AssertFailResponse(response, "inner test Exception", typeof(Exception));
        }

        [Test]
        public void Unknown_request_should_return_failed_json_message()
        {
            var response = _processor.Process("invalid_request");

            AssertFailResponse(response, "Invalid request 'invalid_request'");
        }

        [Test]
        public void Invoke_with_a_step_taking_one_string_parameter_should_pass_the_correct_parameter_value()
        {
            var request = CreateInvokeRequest(_stepDefinitionWithOneStringParameter.Id, "first");
            var response = _processor.Process(request);
            AssertOkResponse(response);
            Assert.That(_receivedParameters.Length, Is.EqualTo(1));
            Assert.That(_receivedParameters[0], Is.InstanceOf(typeof(string)));
            Assert.That(_receivedParameters[0], Is.EqualTo("first"));
        }

        [Test]
        public void Invoke_with_a_step_taking_two_string_parameters_should_pass_the_correct_parameter_values()
        {
            var request = CreateInvokeRequest(_stepDefinitionWithMultipleStringParameters.Id, "first", "second");
            var response = _processor.Process(request);
            AssertOkResponse(response);
            Assert.That(_receivedParameters.Length, Is.EqualTo(2));
            Assert.That(_receivedParameters[0], Is.InstanceOf(typeof(string)));
            Assert.That(_receivedParameters[0], Is.EqualTo("first"));
            Assert.That(_receivedParameters[1], Is.InstanceOf(typeof(string)));
            Assert.That(_receivedParameters[1], Is.EqualTo("second"));
        }

        [Test]
        public void Invoke_without_arguments_if_the_step_takes_arguments_should_return_failed_response()
        {
            var request = CreateInvokeRequest(_stepDefinitionWithOneStringParameter.Id);
            var response = _processor.Process(request);
            AssertFailResponse(response, "Expected 1 argument(s); got 0", typeof(ArgumentException));
        }

        [Test]
        public void Invoke_with_arguments_if_the_step_does_not_take_arguments_should_return_failed_response()
        {
            var request = CreateInvokeRequest(_stepDefinition.Id, "first");
            var response = _processor.Process(request);
            AssertFailResponse(response, "Expected 0 argument(s); got 1", typeof(ArgumentException));
        }

        [Test]
        public void Invoke_with_wrong_number_of_arguments_should_return_failed_response()
        {
            var request = CreateInvokeRequest(_stepDefinitionWithOneStringParameter.Id, "first", "second");
            var response = _processor.Process(request);
            AssertFailResponse(response, "Expected 1 argument(s); got 2", typeof(ArgumentException));
        }

        [Test]
        public void Invoke_with_a_step_taking_an_int_should_convert_and_pass_the_correct_value()
        {
            var request = CreateInvokeRequest(_stepDefinitionWithOneIntParameter.Id, "42");
            var response = _processor.Process(request);
            AssertOkResponse(response);
            Assert.That(_receivedParameters.Length, Is.EqualTo(1));
            Assert.That(_receivedParameters[0], Is.InstanceOf(typeof(int)));
            Assert.That(_receivedParameters[0], Is.EqualTo(42));
        }

        [Test]
        public void Invoke_with_a_step_taking_a_double_should_convert_and_pass_the_correct_value()
        {
            var request = CreateInvokeRequest(_stepDefinitionWithOneDoubleParameter.Id, "3.14");
            var response = _processor.Process(request);
            AssertOkResponse(response);
            Assert.That(_receivedParameters.Length, Is.EqualTo(1));
            Assert.That(_receivedParameters[0], Is.InstanceOf(typeof(double)));
            Assert.That(_receivedParameters[0], Is.EqualTo(3.14));
        }

        [Test]
        public void Invoke_with_a_step_taking_parameters_of_several_types_should_convert_and_pass_the_correct_values()
        {
            var request = CreateInvokeRequest(_stepDefinitionWithIntDoubleAndStringParameters.Id, "42", "3.14", "foo");
            var response = _processor.Process(request);
            AssertOkResponse(response);
            Assert.That(_receivedParameters.Length, Is.EqualTo(3));
            Assert.That(_receivedParameters[0], Is.InstanceOf(typeof(int)));
            Assert.That(_receivedParameters[0], Is.EqualTo(42));
            Assert.That(_receivedParameters[1], Is.InstanceOf(typeof(double)));
            Assert.That(_receivedParameters[1], Is.EqualTo(3.14));
            Assert.That(_receivedParameters[2], Is.InstanceOf(typeof(string)));
            Assert.That(_receivedParameters[2], Is.EqualTo("foo"));
        }

        static string CreateInvokeRequest(string id)
        {
            return @"invoke:{ ""id"" : """ + id + @""" }";
        }

        static string CreateInvokeRequest(string id, params object[] args)
        {
            StringBuilder builder = new StringBuilder("invoke:{ 'id' : '");
            builder.Append(id);
            builder.Append("', 'args' : [");
            for (int i = 0; i < args.Length; ++i)
            {
                builder.Append("'");
                builder.Append(args[i]);
                builder.Append("'");
                if (i != args.Length - 1)
                {
                    builder.Append(", ");
                }
            }
            builder.Append("] }");
            return builder.ToString();
        }

        static void AssertOkResponse(string response)
        {
            if (response != "OK") throw new Exception(response);
            Assert.That(response, Is.EqualTo("OK"));
        }

        static void AssertFailResponse(string response, string message)
        {
            StringAssert.StartsWith("FAIL:", response);
            var jsonData = JsonMapper.ToObject(response.Substring(5));
            JsonAssert.IsObject(jsonData);
            JsonAssert.HasString(jsonData, "message", message);
        }

        static void AssertFailResponse(string response, string message , Type exceptionType)
        {
            AssertFailResponse(response, message);
            var jsonData = JsonMapper.ToObject(response.Substring(5));
            JsonAssert.IsObject(jsonData);
            JsonAssert.HasString(jsonData, "exception", exceptionType.ToString());
            JsonAssert.HasString(jsonData, "backtrace");
        }

        [Given("")]
        public static void Method()
        {
            _methodCalled = true;
        }

        [Given("")]
        public static void ThrowsException()
        {
            throw new Exception("inner test Exception");
        }

        [Given("^The regex group '(.*)' should be captured$")]
        public static void OneStringParameter(string str)
        {
            _receivedParameters = new object[] { str };
        }

        [Given("^The regex groups '(.*)' and '(.*)' should be captured$")]
        public static void MultipleStringParameters(string firstStr, string secondStr)
        {
            _receivedParameters = new object[] { firstStr, secondStr };
        }

        [Given(@"^The number ([+-]?\d+) is an int$")]
        public static void OneIntParameter(int number) {
            _receivedParameters = new object[] { number };
        }

        [Given(@"^The number ([+-]?\d+\.\d*) is a double$")]
        public static void OneDoubleParameter(double number)
        {
            _receivedParameters = new object[] { number };
        }

        [Given(@"^The values ([+-]?\d+), ([+-]?\d+\.\d*), and '(.*)' are an int, a double and a string$")]
        public static void IntDoubleAndStringParameters(int intValue, double doubleValue, string stringValue)
        {
            _receivedParameters = new object[] { intValue, doubleValue, stringValue };
        }

        class MockLoader : Loader
        {
            internal List<StepDefinition> StepDefinitions { get; private set; }

            public MockLoader(List<StepDefinition> stepDefinitions)
                : base(null)
            {
                StepDefinitions = stepDefinitions;
            }

            public override List<StepDefinition> Load()
            {
                return StepDefinitions;
            }
        }
    }
}