using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TinyToml.Types;

namespace TinyToml.UnitTests;

[TestFixture]
public class ParserTests
{
    [Test]
    public void SmokeTest()
    {
        //Arrange
        var doc = Toml.Parse("key = true");

        //Act & Assert
        doc["key"].TryReadTomlBool(out var actual).Should().Be(true);
        actual.Should().Be(true);
    }

    [TestCase("bare",                      "bare")]
    [TestCase("\"basic string\"",          "basic string")]
    [TestCase("\"basic string \\u00B5 \"", "basic string µ ")]
    [TestCase("'lit. string'",             "lit. string")]
    public void KeysTest(string inputKeyName, string expected)
    {
        //Arrange
        var doc = Toml.Parse($"{inputKeyName} = \"true\"");

        //Act
        doc.Keys.Should().Contain(expected);
    }

    [Test]
    public void MaximumNestingTest()
    {
        //Arrange
        var docStr = @"
    [table-1.2.3.4.5.6.7.8.9.10.11.12]
    key = true
    [table-1]
    v = 3    
";
        //Act
        var doc = Toml.Parse(docStr);

        //Assert
        doc["table-1"]["2"]["3"]["4"]["5"]["6"]["7"]["8"]["9"]["10"]["11"]["12"]["key"]
            .TryReadTomlBool(out var actual);
        actual.Should().Be(true);

        doc["table-1"]["v"].TryReadTomlInteger(out var intVal);
        intVal.Should().Be(3);
    }

    [Test]
    public void MaximumNestingErrTest()
    {
        //Arrange
        var docStr = @"
    [table-1.2.3.4.5.6.7.8.9.10.11.12.13.14.15.16]
    key.list = [1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2,[1,2]]]]]]]]]]]]]]]]]]],3] 
    [table-1]
    v = 3    
";
        //TODO: Get proper error: Nesting too deep ...
        //Act
        Action act = () => Toml.Parse(docStr);

        //Assert
        act.Should().Throw<ArgumentException>();
        try
        {
            var doc = Toml.Parse(docStr);
            Assert.Fail("Should throw exception: Nesting is to deep, this is currently not supported.");

            //Assert
            /*doc["table-1"]["2"]["3"]["4"]["5"]["6"]["7"]["8"]["9"]["10"]["11"]["12"]["13"]["14"]["15"]["16"]["key"]
                ["list"]
                .TryReadTomlArray(out var actual);
            actual.Count.Should().Be(4);

            doc["table-1"]["v"].TryReadTomlInteger(out var intVal);
            intVal.Should().Be(3);*/
        }
        catch (ArgumentException)
        {
            //expected
        }
    }

    [Test]
    public void BareKeyBasicStringValue()
    {
        //Arrange
        var doc = Toml.Parse("key = \"true\"");

        //Act & Assert
        doc["key"].TryReadTomlString(out var actual).Should().Be(true);
        actual.Should().Be("true");
    }

    [TestCase("value=+100",   100)]
    [TestCase("value=+1_000", 1000)]
    [TestCase("value=-100",   -100)]
    [TestCase("value=-1_000", -1000)]
    [TestCase("value=-0",     0)]
    [TestCase("value=+0",     0)]
    [TestCase("value=0x0",    0)]
    [TestCase("value=0x1",    1)]
    [TestCase("value=0o1",    1)]
    [TestCase("value=0b1",    1)]
    public void Integers(string toml, long expected)
    {
        Toml.Parse(toml)["value"].TryReadTomlInteger(out var value).Should().BeTrue();
        value.Should().Be(expected);
    }

    [TestCase("value=[+100, +0, -0, -100, 1]",    new long[] {100, 0, 0, -100, 1})]
    [TestCase("value=[0x64, +0, -0, -100, 0b1]",  new long[] {100, 0, 0, -100, 1})]
    [TestCase("value=[0o144, +0, -0, -100, 0b1]", new long[] {100, 0, 0, -100, 1})]
    public void IntegerArray(string toml, params long[] expected)
    {
        Toml.Parse(toml)["value"].TryReadTomlArray(out var array).Should().BeTrue();
        for (var i = 0; i < array.Count; i++)
        {
            var value = array[i];
            value.TryReadTomlInteger(out var intValue).Should().BeTrue();
            intValue.Should().Be(expected[i]);
        }
    }

    [Test]
    public void DottedKeyBasicStringValue()
    {
        //Arrange
        var doc = Toml.Parse("key.open = \"true\"");

        //Act & Assert
        doc["key"].TryReadTomlTable(out var actual).Should().Be(true);
        actual["open"].TryReadTomlString(out var strValue).Should().BeTrue();
        strValue.Should().Be("true");
    }

    [Test]
    public void MultilineLiteralString()
    {
        //Arrange
        var doc = Toml.Parse("key = '''true'''");

        //Act & Assert
        doc["key"].TryReadTomlString(out var strValue).Should().BeTrue();
        strValue.Should().Be("true");
    }

    [Test]
    public void MultipleDottedKeyBasicStringValue()
    {
        //Arrange
        var doc = Toml.Parse(@"
                                        [prev-table]
                                        props = {topic = 'prev-parsing', id = 3}                                              
                                        key.first.open = ""true""
                                        key.second.open = ""false""                                                                                                                                                     
                                        name = ""prev-test"" 
                                        params = [{id = 1, name = 'prev-pName'}]     

                                        [table]
                                        props = {topic = 'parsing', id = 2}                                              
                                        key.first.open = ""true""
                                        key.second.open = ""false""                                                                          
                                        params = [{id = 1, name = 'pName'}]                                        
                                        name = ""test""                                      
 "
                            );

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual["key"].TryReadTomlTable(out var keys).Should().Be(true);

        keys["first"]["open"].TryReadTomlString(out var first).Should().BeTrue();
        first.Should().Be("true");

        keys["second"]["open"].TryReadTomlString(out var second).Should().BeTrue();
        second.Should().Be("false");

        actual["name"].TryReadTomlString(out var name).Should().BeTrue();
        name.Should().Be("test");
    }

    [Test]
    public void BareKeyLiteralStringValue()
    {
        //Arrange
        var doc = Toml.Parse("key = 'true'");

        //Act & Assert
        doc["key"].TryReadTomlString(out var actual).Should().Be(true);
        actual.Should().Be("true");
    }

    [Test]
    public void BareKeyMultiLineStringValue()
    {
        //Arrange
        var doc = Toml.Parse("key = \"\"\"true\"\"\"");

        //Act & Assert
        doc["key"].TryReadTomlString(out var actual).Should().Be(true);
        actual.Should().Be("true");
    }

    [Test]
    public void BareKeyEmptyBasicStringValue()
    {
        //Arrange
        var doc = Toml.Parse("key = \"\" ");

        //Act & Assert
        doc["key"].TryReadTomlString(out var actual).Should().Be(true);
        actual.Should().Be(string.Empty);
    }

    [Test]
    public void TableBareKeyBasicStringValue()
    {
        //Arrange
        var text = @"
                        [table]
                        key = 'myValue'
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(1);
        actual["key"].TryReadTomlString(out var inner).Should().Be(true);
        inner.Should().Be("myValue");
    }

    [Test]
    public void CommentsIgnore()
    {
        //Arrange
        var text = @"
                        [table]         # some info on the table
                        key = 'myValue' # my comments
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(1);
        actual["key"].TryReadTomlString(out var inner).Should().Be(true);
        inner.Should().Be("myValue");
    }

    [Test]
    public void TableDottedKeyBasicStringValue()
    {
        //Arrange
        var text = @"
                        [table.sub-table]
                        key = 'myValue'
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(1);

        actual["sub-table"].TryReadTomlTable(out var subTable).Should().Be(true);
        subTable.Should().HaveCount(1);

        subTable["key"].TryReadTomlString(out var inner).Should().Be(true);
        inner.Should().Be("myValue");
    }

    [Test]
    public void TableArray()
    {
        //Arrange
        var text = @"                        
                        [[table.myList]]
                        key = 'myValue'
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(1);

        actual["myList"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(1);

        values[0]["key"].TryReadTomlString(out var inner).Should().BeTrue();
        inner.Should().Be("myValue");
    }

    [Test]
    public void TableArrayWithMultipleElements()
    {
        //Arrange
        var text = @"
                        [table]
                        name = 'something'                        
                        [[table.myList]]
                        key = 'myValue'
                        tst = 'ok'
                        [[table.myList]]
                        key = 'otherValue'
                        tst = 'nok'
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(2); //name and myList

        actual["myList"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(2);

        values[1]["key"].TryReadTomlString(out var inner).Should().BeTrue();
        inner.Should().Be("otherValue");

        values[1]["tst"].TryReadTomlString(out inner).Should().BeTrue();
        inner.Should().Be("nok");
    }

    [Test]
    public void TableArrayNestedElements()
    {
        //In nested arrays of tables, each double-bracketed sub-table will belong to the most recently defined table element.
        // Normal sub-tables (not arrays) likewise belong to the most recently defined table element.

        //Arrange
        var text = @"
                        [[fruit]]
                            name = 'apple'                        
                            
                            [fruit.physical]  # sub-table, note: I think this is a super weird construct
                                color = 'red'
                                shape = 'round'
                            [[fruit.variety]]  # nested array of tables
                                name = 'red delicious'
                            [[fruit.variety]]  
                                name = 'granny smith'
                        
                        [[fruit]]
                            name = 'banana'

                            [[fruit.variety]]  
                                name = 'plantain'
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["fruit"].TryReadTomlArray(out var actual).Should().Be(true);
        actual.Should().HaveCount(2); //name and myList

        actual[0].TryReadTomlTable(out var values).Should().BeTrue(actual[0].TomlType.ToString());
        values.Should().HaveCount(3);
    }

    [Test]
    public void InlineArray()
    {
        //Arrange
        var text = @"
                        [table]
                        name = 'something'
                        myList = [ { key = 'myValue'}, { key = 'otherValue'} ]                        
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(2); //name and myList

        actual["myList"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(2);

        values[1]["key"].TryReadTomlString(out var inner).Should().BeTrue();
        inner.Should().Be("otherValue");
    }

    [Test]
    public void NestedInlineTableArrays()
    {
        //Arrange
        var text = @"
                        [table]
                        name = 'something'
                        myList = [ { key = 'myValue'}, { innerTable.key2 = 'otherValue'} ]                        
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(2); //name and myList

        actual["myList"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(2);

        values[1]["innerTable"].TryReadTomlTable(out var inner).Should().BeTrue();
        ((TomlString) inner["key2"]).Value.Should().Be("otherValue");
    }

    [Test]
    public void TableArraysInTableArrays()
    {
        //Arrange
        var text = @"
                        [table]
                        name = 'something'
                        myList = [ { key = [ {elem1 = '1'}, {elem2 = '2'}] }, { innerTable.key2 = 'otherValue'} ]                        
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(2); //name and myList

        actual["myList"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(2);

        values[0]["key"].TryReadTomlArray(out var inner).Should().BeTrue();
        inner[1].TryReadTomlTable(out var innerTbl).Should().BeTrue();
        ((TomlString) innerTbl["elem2"]).Value.Should().Be("2");
    }

    [Test]
    public void ValueArrays()
    {
        //Arrange
        var text = @"
                        [table]
                        name = 'something'
                        myList = [ 0x01,2,3,4 ]                        
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(2);

        actual["myList"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(4);

        values[0].TryReadTomlInteger(out var inner).Should().BeTrue();
        inner.Should().Be(1);
    }

    [TestCase("myList = [ [0b1,2], [3,0o4] ]")]
    [TestCase("myList = [[0b1,2], [3,0o4] ]")]
    [TestCase("myList = [[0b1,2], [3,0o4]]")]
    [TestCase("myList = [ [0b1,2], [3,0o4]]")]
    public void NestedValueArrays(string arr)
    {
        //Arrange
        var text = @"
                        [table]
                        name = 'something'                                            
                        " + arr;
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(2);

        actual["myList"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(2);

        values[0].TryReadTomlArray(out var inner).Should().BeTrue();
        inner.Should().HaveCount(2);
        inner[0].TryReadTomlInteger(out var integer).Should().BeTrue();
        integer.Should().Be(1);
    }

    [Test]
    public void MultipleInlineArrays()
    {
        //Arrange
        var text = @"
                        [table-1]
                        name = 'something1'
                        myList = [ { key = 'myValue1'}, { key = 'otherValue1'} ]
                        [table-2]
                        name = 'something2'
                        myList = [ { key = 'myValue2'}, { key = 'otherValue2'} ]                        
                        ";
        var doc = Toml.Parse(text);

        //Table 1
        doc["table-1"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(2); //name and myList

        actual["myList"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(2);

        values[1]["key"].TryReadTomlString(out var inner).Should().BeTrue();
        inner.Should().Be("otherValue1");

        //Table 2
        doc["table-2"].TryReadTomlTable(out actual).Should().Be(true);
        actual.Should().HaveCount(2); //name and myList

        actual["myList"].TryReadTomlArray(out values).Should().BeTrue();
        values.Should().HaveCount(2);

        values[1]["key"].TryReadTomlString(out inner).Should().BeTrue();
        inner.Should().Be("otherValue2");
    }

    [Test]
    public void MixedSingleLineArray()
    {
        //Arrange
        var text = @"
            contributors = [ ""Foo Bar <foo@example.com>"", 1, { name = ""Baz Qux"", email = ""bazqux@example.com"", url = ""https://example.com/bazqux"" }, 3.14 ]                        
                        ";

        //Act
        var doc = Toml.Parse(text);

        //Assert
        doc["contributors"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(4);

        values[0].TryReadTomlString(out var inner).Should().BeTrue();
        inner.Should().Be("Foo Bar <foo@example.com>");

        values[1].TryReadTomlInteger(out var innerInt).Should().BeTrue();
        innerInt.Should().Be(1);
    }

    [Test]
    public void MixedMultilineArray()
    {
        //Arrange
        var text = @"
                       contributors = [
                          ""Foo Bar <foo@example.com>"",
                          1,
                          { name = ""Baz Qux"", email = ""bazqux@example.com"", url = ""https://example.com/bazqux"" },
                          3.14
                        ]                        
                        ";
        var doc = Toml.Parse(text);

        doc["contributors"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(4);

        values[0].TryReadTomlString(out var inner).Should().BeTrue();
        inner.Should().Be("Foo Bar <foo@example.com>");


        values[1].TryReadTomlInteger(out var innerInt).Should().BeTrue();
        innerInt.Should().Be(1);
    }

    [Test]
    public void MultiLineSimpleArrayWithTrailingCommaAndComments()
    {
        //Arrange
        var text = @"
                       integers3 =   [
                                       1,
                                       2, # this is ok
                                     ]                   
                        ";
        var doc = Toml.Parse(text);

        doc["integers3"].TryReadTomlArray(out var values).Should().BeTrue();
        values.Should().HaveCount(2);

        values[0].TryReadTomlInteger(out var inner).Should().BeTrue();
        inner.Should().Be(1);

        values[1].TryReadTomlInteger(out var innerInt).Should().BeTrue();
        innerInt.Should().Be(2);
    }

    [Test]
    public void InlineTable()
    {
        var text = @"
                        [table]
                        name = 'something'
                        my_props = { product_groupId = 'pgid', display_as = 'loc'}                        
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(2);

        actual["my_props"].TryReadTomlTable(out var inner).Should().BeTrue();
        inner.Should().HaveCount(2);

        inner["product_groupId"].TryReadTomlString(out var a).Should().BeTrue();
        a.Should().Be("pgid");

        inner["display_as"].TryReadTomlString(out var b).Should().BeTrue();
        b.Should().Be("loc");
    }

    [Test]
    public void NoErrorsInValidDocument()
    {
        //Arrange
        var toml =
            File.ReadAllText(Path.Join(TestContext.CurrentContext.TestDirectory, "customer_reports.toml"));

        //Act
        var doc = Toml.Parse(toml);

        //Assert
        //no errors
        doc.Count.Should().Be(7);

        foreach (var key in doc.Keys)
        {
            doc[key].TryReadTomlTable(out var table).Should().BeTrue(key);
            table["sql"].TryReadTomlString(out var sql).Should().BeTrue(key);
            sql.Should().NotBeNullOrWhiteSpace(key);
        }
    }

    [Test]
    public void UnixLineEndingsAndCommentAfterTableDeclaration()
    {
        var text = "[table] #end comment \n name = \"\"\"\nsomething\n\nother\n\"\"\"\n";
        text += "    [[table.something]] #comment\n name=1#eol\n";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["table"].TryReadTomlTable(out var actual).Should().Be(true);
        actual.Should().HaveCount(2);
    }

    [Test]
    public void CannotReuseArrayAsTable()
    {
        var toml = @"
                        # INVALID TOML DOC
                        [[fruit]]
                          name = ""apple""

                          [[fruit.variety]]
                            name = ""red delicious""

                          # INVALID: This table conflicts with the previous array of tables
                          [fruit.variety]
                            name = ""granny smith""
                        ";
        Action parseAction = () => Toml.Parse(toml);

        //Act & Assert
        parseAction.Should().Throw<Exception>().WithMessage("*Cannot redefine 'variety' as a Table*");
    }

    [Test]
    public void CannotReuseTableAsArray()
    {
        var toml = @"
                        # INVALID TOML DOC
                        [[fruit]]
                          name = ""apple""

                          [fruit.variety]
                            name = ""granny smith""
                          
                          # INVALID: This Array conflicts with the previous Table
                          [[fruit.variety]]
                            name = ""red delicious""                                                    
                        ";
        Action parseAction = () => Toml.Parse(toml);

        //Act & Assert
        parseAction.Should().Throw<Exception>().WithMessage("*Cannot redefine 'variety' as an Array*");
    }

    [Test]
    public void NumberUnderscoreSeparator()
    {
        var text = @"                        
                        number = 12_34_56                                                
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["number"].TryReadTomlInteger(out var number).Should().BeTrue();
        number.Should().Be(123456);
    }

    [Test]
    public void ErrorWhenNumberEndsWithUnderscore()
    {
        var text = @"                        
                        number = 12_34_56_                                                
                        ";
        Action parser = () => Toml.Parse(text);

        //Act & Assert
        parser.Should().Throw<Exception>().WithMessage("Error: Expect DecDigits after '_'. at '', Line: 3, Column: 25");
    }

    [Test]
    public void ErrorWhenTooManyDigits()
    {
        var text = @"                        
                        number = 123456781234567812345678123456781234567812345678123456781234567812345678                                                
                        ";
        Action parser = () => Toml.Parse(text);
        //Act & Assert
        parser.Should().Throw<Exception>().WithMessage("Error: Could not parse number, too many digits the maximum number of digits is 64*");
    }

    [Test]
    public void QuotedStringEquivalence()
    {
        //Arrange
        var text = @"
                            ""test"" = 1
                            test = 1 # should give error 
                        ";

        Action parser = () => Toml.Parse(text);

        //Act & Assert
        parser.Should().Throw<Exception>().WithMessage("An item with the same key has already been added. Key: test");

        //TODO: same goes for [test] && ["test"] && ['test']
    }
}