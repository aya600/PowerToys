﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CmdPal.Ext.Calc.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CmdPal.Ext.Calc.UnitTests;

[TestClass]
public class ExtendedCalculatorParserTests
{
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    public void InputValid_ThrowError_WhenCalledNullOrEmpty(string input)
    {
        // Act
        Assert.IsTrue(!CalculateHelper.InputValid(input));
    }

    [DataTestMethod]
    [DataRow("test")]
    [DataRow("[10,10]")] // '[10,10]' is interpreted as array by mages engine
    public void Interpret_NoResult_WhenCalled(string input)
    {
        var settings = new SettingsManager();

        var result = CalculateEngine.Interpret(settings, input, CultureInfo.CurrentCulture, out _);

        // Assert
        Assert.AreEqual(default(CalculateResult), result);
    }

    private static IEnumerable<object[]> Interpret_NoErrors_WhenCalledWithRounding_Data =>
        [
            ["2 * 2", 4M],
            ["-2 ^ 2", -4M],
            ["-(2 ^ 2)", -4M],
            ["2 * pi", 6.28318530717959M],
            ["round(2 * pi)", 6M],

            // ["1 == 2", default(decimal)],
            ["pi * ( sin ( cos ( 2)))", -1.26995475603563M],
            ["5.6/2", 2.8M],
            ["123 * 4.56", 560.88M],
            ["1 - 9.0 / 10", 0.1M],
            ["0.5 * ((2*-395.2)+198.2)", -296.1M],
            ["2+2.11", 4.11M],
            ["8.43 + 4.43 - 12.86", 0M],
            ["8.43 + 4.43 - 12.8", 0.06M],
            ["exp(5)", 148.413159102577M],
            ["e^5", 148.413159102577M],
            ["e*2", 5.43656365691809M],
            ["ln(3)", 1.09861228866811M],
            ["log(3)", 0.47712125471966M],
            ["log2(3)", 1.58496250072116M],
            ["log10(3)", 0.47712125471966M],
            ["ln(e)", 1M],
            ["cosh(0)", 1M],
        ];

    [DataTestMethod]
    [DynamicData(nameof(Interpret_NoErrors_WhenCalledWithRounding_Data))]
    public void Interpret_NoErrors_WhenCalledWithRounding(string input, decimal expectedResult)
    {
        var settings = new SettingsManager();

        // Act
        // Using InvariantCulture since this is internal
        var result = CalculateEngine.Interpret(settings, input, CultureInfo.InvariantCulture, out _);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(CalculateEngine.FormatMax15Digits(expectedResult, new CultureInfo("en-US")), result.RoundedResult);
    }

    private static IEnumerable<object[]> Interpret_GreaterPrecision_WhenCalled_Data =>
        [
            ["0.100000000000000000000", 0.1M],
            ["0.200000000000000000000000", 0.2M],
        ];

    [DynamicData(nameof(Interpret_GreaterPrecision_WhenCalled_Data))]
    [DataTestMethod]
    public void Interpret_GreaterPrecision_WhenCalled(string input, decimal expectedResult)
    {
        // Arrange
        var settings = new SettingsManager();

        // Act
        // Using InvariantCulture since this is internal
        var result = CalculateEngine.Interpret(settings, input, CultureInfo.InvariantCulture, out _);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResult, result.Result);
    }

    private static IEnumerable<object[]> Interpret_DifferentCulture_WhenCalled_Data =>
        [
            ["4.5/3", 1.5M, "nl-NL"],
            ["4.5/3", 1.5M, "en-EN"],
            ["4.5/3", 1.5M, "de-DE"],
        ];

    [DataTestMethod]
    [DynamicData(nameof(Interpret_DifferentCulture_WhenCalled_Data))]
    public void Interpret_DifferentCulture_WhenCalled(string input, decimal expectedResult, string cultureName)
    {
        // Arrange
        var cultureInfo = CultureInfo.GetCultureInfo(cultureName);
        var settings = new SettingsManager();

        // Act
        var result = CalculateEngine.Interpret(settings, input, cultureInfo, out _);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(CalculateEngine.Round(expectedResult), result.RoundedResult);
    }

    [DataTestMethod]
    [DataRow("log(3)", true)]
    [DataRow("ln(3)", true)]
    [DataRow("log2(3)", true)]
    [DataRow("log10(3)", true)]
    [DataRow("log2", false)]
    [DataRow("log10", false)]
    [DataRow("log", false)]
    [DataRow("ln", false)]
    [DataRow("ceil(2 * (pi ^ 2))", true)]
    [DataRow("((1 * 2)", false)]
    [DataRow("(1 * 2)))", false)]
    [DataRow("abcde", false)]
    [DataRow("1 + 2 +", false)]
    [DataRow("1+2*", false)]
    [DataRow("1+2/", false)]
    [DataRow("1+2%", false)]
    [DataRow("1 && 3 &&", false)]
    [DataRow("sqrt( 36)", true)]
    [DataRow("max 4", false)]
    [DataRow("sin(0)", true)]
    [DataRow("sinh(1)", true)]
    [DataRow("tanh(0)", true)]
    [DataRow("artanh(pi/2)", true)]
    [DataRow("cosh", false)]
    [DataRow("cos", false)]
    [DataRow("abs", false)]
    [DataRow("1+1.1e3", true)]
    [DataRow("randi(8)", true)]
    [DataRow("randi()", false)]
    [DataRow("randi(0.5)", true)]
    [DataRow("rand()", true)]
    [DataRow("rand(0.5)", false)]
    [DataRow("0X78AD+0o123", true)]
    [DataRow("0o9", false)]
    public void InputValid_TestValid_WhenCalled(string input, bool valid)
    {
        // Act
        var result = CalculateHelper.InputValid(input);

        // Assert
        Assert.AreEqual(valid, result);
    }

    [DataTestMethod]
    [DataRow("1-1")]
    [DataRow("sin(0)")]
    [DataRow("sinh(0)")]
    public void Interpret_MustReturnResult_WhenResultIsZero(string input)
    {
        // Arrange
        var settings = new SettingsManager();

        // Act
        // Using InvariantCulture since this is internal
        var result = CalculateEngine.Interpret(settings, input, CultureInfo.InvariantCulture, out _);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0M, result.Result);
    }

    private static IEnumerable<object[]> Interpret_MustReturnExpectedResult_WhenCalled_Data =>
        [

            // ["factorial(5)", 120M], ToDo: this don't support now
            // ["sign(-2)", -1M],
            // ["sign(2)", +1M],
            ["abs(-2)", 2M],
            ["abs(2)", 2M],
            ["0+(1*2)/(0+1)", 2M], // Validate that division by "(0+1)" is not interpret as division by zero.
            ["0+(1*2)/0.5", 4M], // Validate that division by number with decimal digits is not interpret as division by zero.
        ];

    [DataTestMethod]
    [DynamicData(nameof(Interpret_MustReturnExpectedResult_WhenCalled_Data))]
    public void Interpret_MustReturnExpectedResult_WhenCalled(string input, decimal expectedResult)
    {
        // Arrange
        var settings = new SettingsManager();

        // Act
        // Using en-us culture to have a fixed number style
        var result = CalculateEngine.Interpret(settings, input, new CultureInfo("en-us", false), out _);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResult, result.Result);
    }

    private static IEnumerable<object[]> Interpret_TestScientificNotation_WhenCalled_Data =>
        [
            ["0.2E1", "en-US", 2M],
            ["0,2E1", "pt-PT", 2M],
        ];

    [DataTestMethod]
    [DynamicData(nameof(Interpret_TestScientificNotation_WhenCalled_Data))]
    public void Interpret_TestScientificNotation_WhenCalled(string input, string sourceCultureName, decimal expectedResult)
    {
        // Arrange
        var translator = NumberTranslator.Create(new CultureInfo(sourceCultureName, false), new CultureInfo("en-US", false));
        var settings = new SettingsManager();

        // Act
        // Using en-us culture to have a fixed number style
        var translatedInput = translator.Translate(input);
        var result = CalculateEngine.Interpret(settings, translatedInput, new CultureInfo("en-US", false), out _);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResult, result.Result);
    }

    [DataTestMethod]
    [DataRow("sin(90)", "sin((pi / 180) * (90))")]
    [DataRow("arcsin(0.5)", "(180 / pi) * (arcsin(0.5))")]
    [DataRow("sin(sin(30))", "sin((pi / 180) * (sin((pi / 180) * (30))))")]
    [DataRow("cos(tan(45))", "cos((pi / 180) * (tan((pi / 180) * (45))))")]
    [DataRow("arctan(sin(30))", "(180 / pi) * (arctan(sin((pi / 180) * (30))))")]
    [DataRow("sin(cos(tan(30)))", "sin((pi / 180) * (cos((pi / 180) * (tan((pi / 180) * (30))))))")]
    [DataRow("sin(arcsin(0.5))", "sin((pi / 180) * ((180 / pi) * (arcsin(0.5))))")]
    [DataRow("sin(30) + cos(60)", "sin((pi / 180) * (30)) + cos((pi / 180) * (60))")]
    [DataRow("sin(30 + 15)", "sin((pi / 180) * (30 + 15))")]
    [DataRow("sin(45) * cos(45) - tan(30)", "sin((pi / 180) * (45)) * cos((pi / 180) * (45)) - tan((pi / 180) * (30))")]
    [DataRow("arcsin(arccos(0.5))", "(180 / pi) * (arcsin((180 / pi) * (arccos(0.5))))")]
    [DataRow("sin(sin(sin(30)))", "sin((pi / 180) * (sin((pi / 180) * (sin((pi / 180) * (30))))))")]
    [DataRow("log(10)", "log(10)")]
    [DataRow("sin(30) + pi", "sin((pi / 180) * (30)) + pi")]
    [DataRow("sin(-30)", "sin((pi / 180) * (-30))")]
    [DataRow("sin((30))", "sin((pi / 180) * ((30)))")]
    [DataRow("arcsin(1) * 2", "(180 / pi) * (arcsin(1)) * 2")]
    [DataRow("cos(1/2)", "cos((pi / 180) * (1/2))")]
    [DataRow("sin ( 90 )", "sin ((pi / 180) * ( 90 ))")]
    [DataRow("cos(arcsin(sin(45)))", "cos((pi / 180) * ((180 / pi) * (arcsin(sin((pi / 180) * (45))))))")]
    public void UpdateTrigFunctions_Degrees(string input, string expectedResult)
    {
        // Call UpdateTrigFunctions in degrees mode
        var result = CalculateHelper.UpdateTrigFunctions(input, CalculateEngine.TrigMode.Degrees);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResult, result);
    }

    [DataTestMethod]
    [DataRow("sin(90)", "sin((pi / 200) * (90))")]
    [DataRow("arcsin(0.5)", "(200 / pi) * (arcsin(0.5))")]
    [DataRow("sin(sin(30))", "sin((pi / 200) * (sin((pi / 200) * (30))))")]
    [DataRow("cos(tan(45))", "cos((pi / 200) * (tan((pi / 200) * (45))))")]
    [DataRow("arctan(sin(30))", "(200 / pi) * (arctan(sin((pi / 200) * (30))))")]
    [DataRow("sin(cos(tan(30)))", "sin((pi / 200) * (cos((pi / 200) * (tan((pi / 200) * (30))))))")]
    [DataRow("sin(arcsin(0.5))", "sin((pi / 200) * ((200 / pi) * (arcsin(0.5))))")]
    [DataRow("sin(30) + cos(60)", "sin((pi / 200) * (30)) + cos((pi / 200) * (60))")]
    [DataRow("sin(30 + 15)", "sin((pi / 200) * (30 + 15))")]
    [DataRow("sin(45) * cos(45) - tan(30)", "sin((pi / 200) * (45)) * cos((pi / 200) * (45)) - tan((pi / 200) * (30))")]
    [DataRow("arcsin(arccos(0.5))", "(200 / pi) * (arcsin((200 / pi) * (arccos(0.5))))")]
    [DataRow("sin(sin(sin(30)))", "sin((pi / 200) * (sin((pi / 200) * (sin((pi / 200) * (30))))))")]
    [DataRow("log(10)", "log(10)")]
    [DataRow("sin(30) + pi", "sin((pi / 200) * (30)) + pi")]
    [DataRow("sin(-30)", "sin((pi / 200) * (-30))")]
    [DataRow("sin((30))", "sin((pi / 200) * ((30)))")]
    [DataRow("arcsin(1) * 2", "(200 / pi) * (arcsin(1)) * 2")]
    [DataRow("cos(1/2)", "cos((pi / 200) * (1/2))")]
    [DataRow("sin ( 90 )", "sin ((pi / 200) * ( 90 ))")]
    [DataRow("cos(arcsin(sin(45)))", "cos((pi / 200) * ((200 / pi) * (arcsin(sin((pi / 200) * (45))))))")]
    public void UpdateTrigFunctions_Gradians(string input, string expectedResult)
    {
        // Call UpdateTrigFunctions in gradians mode
        var result = CalculateHelper.UpdateTrigFunctions(input, CalculateEngine.TrigMode.Gradians);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResult, result);
    }

    [DataTestMethod]
    [DataRow("rad(30)", "(180 / pi) * (30)")]
    [DataRow("rad( 30 )", "(180 / pi) * ( 30 )")]
    [DataRow("deg(30)", "(30)")]
    [DataRow("grad(30)", "(9 / 10) * (30)")]
    [DataRow("rad(  30)", "(180 / pi) * (  30)")]
    [DataRow("rad(30  )", "(180 / pi) * (30  )")]
    [DataRow("rad(  30  )", "(180 / pi) * (  30  )")]
    [DataRow("rad(deg(30))", "(180 / pi) * ((30))")]
    [DataRow("deg(rad(30))", "((180 / pi) * (30))")]
    [DataRow("grad(rad(30))", "(9 / 10) * ((180 / pi) * (30))")]
    [DataRow("rad(grad(30))", "(180 / pi) * ((9 / 10) * (30))")]
    [DataRow("rad(30) + deg(45)", "(180 / pi) * (30) + (45)")]
    [DataRow("sin(rad(30))", "sin((180 / pi) * (30))")]
    [DataRow("cos( rad( 45 ) )", "cos( (180 / pi) * ( 45 ) )")]
    [DataRow("tan(rad(grad(90)))", "tan((180 / pi) * ((9 / 10) * (90)))")]
    [DataRow("rad(30) + rad(45)", "(180 / pi) * (30) + (180 / pi) * (45)")]
    [DataRow("rad(30) * grad(90)", "(180 / pi) * (30) * (9 / 10) * (90)")]
    [DataRow("rad(30)/rad(45)", "(180 / pi) * (30)/(180 / pi) * (45)")]
    public void ExpandTrigConversions_Degrees(string input, string expectedResult)
    {
        // Call ExpandTrigConversions in degrees mode
        var result = CalculateHelper.ExpandTrigConversions(input, CalculateEngine.TrigMode.Degrees);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResult, result);
    }

    [DataTestMethod]
    [DataRow("rad(30)", "(30)")]
    [DataRow("rad( 30 )", "( 30 )")]
    [DataRow("deg(30)", "(pi / 180) * (30)")]
    [DataRow("grad(30)", "(pi / 200) * (30)")]
    [DataRow("rad(  30)", "(  30)")]
    [DataRow("rad(30  )", "(30  )")]
    [DataRow("rad(  30  )", "(  30  )")]
    [DataRow("rad(deg(30))", "((pi / 180) * (30))")]
    [DataRow("deg(rad(30))", "(pi / 180) * ((30))")]
    [DataRow("grad(rad(30))", "(pi / 200) * ((30))")]
    [DataRow("rad(grad(30))", "((pi / 200) * (30))")]
    [DataRow("rad(30) + deg(45)", "(30) + (pi / 180) * (45)")]
    [DataRow("sin(rad(30))", "sin((30))")]
    [DataRow("cos( rad( 45 ) )", "cos( ( 45 ) )")]
    [DataRow("tan(rad(grad(90)))", "tan(((pi / 200) * (90)))")]
    [DataRow("rad(30) + rad(45)", "(30) + (45)")]
    [DataRow("rad(30) * grad(90)", "(30) * (pi / 200) * (90)")]
    [DataRow("rad(30)/rad(45)", "(30)/(45)")]
    public void ExpandTrigConversions_Radians(string input, string expectedResult)
    {
        // Call ExpandTrigConversions in radians mode
        var result = CalculateHelper.ExpandTrigConversions(input, CalculateEngine.TrigMode.Radians);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResult, result);
    }

    [DataTestMethod]
    [DataRow("rad(30)", "(200 / pi) * (30)")]
    [DataRow("rad( 30 )", "(200 / pi) * ( 30 )")]
    [DataRow("deg(30)", "(10 / 9) * (30)")]
    [DataRow("grad(30)", "(30)")]
    [DataRow("rad(  30)", "(200 / pi) * (  30)")]
    [DataRow("rad(30  )", "(200 / pi) * (30  )")]
    [DataRow("rad(  30  )", "(200 / pi) * (  30  )")]
    [DataRow("rad(deg(30))", "(200 / pi) * ((10 / 9) * (30))")]
    [DataRow("deg(rad(30))", "(10 / 9) * ((200 / pi) * (30))")]
    [DataRow("grad(rad(30))", "((200 / pi) * (30))")]
    [DataRow("rad(grad(30))", "(200 / pi) * ((30))")]
    [DataRow("rad(30) + deg(45)", "(200 / pi) * (30) + (10 / 9) * (45)")]
    [DataRow("sin(rad(30))", "sin((200 / pi) * (30))")]
    [DataRow("cos( rad( 45 ) )", "cos( (200 / pi) * ( 45 ) )")]
    [DataRow("tan(rad(grad(90)))", "tan((200 / pi) * ((90)))")]
    [DataRow("rad(30) + rad(45)", "(200 / pi) * (30) + (200 / pi) * (45)")]
    [DataRow("rad(30) * grad(90)", "(200 / pi) * (30) * (90)")]
    [DataRow("rad(30)/rad(45)", "(200 / pi) * (30)/(200 / pi) * (45)")]
    public void ExpandTrigConversions_Gradians(string input, string expectedResult)
    {
        // Call ExpandTrigConversions in gradians mode
        var result = CalculateHelper.ExpandTrigConversions(input, CalculateEngine.TrigMode.Gradians);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResult, result);
    }
}
