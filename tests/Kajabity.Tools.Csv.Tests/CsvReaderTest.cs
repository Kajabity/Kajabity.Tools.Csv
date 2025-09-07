/*
 * Copyright 2009-17 Williams Technologies Limited.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Kajabity is a trademark of Williams Technologies Limited.
 *
 * http://www.kajabity.com
 */


namespace Kajabity.Tools.Csv.Tests
{
    [TestFixture]
    public class CsvReaderTest
    {
        private static readonly string CsvTestDataDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "Csv");
        private static readonly string CsvOutputDirectory = Path.GetTempPath();

        private readonly string EmptyTestFile = Path.Combine(CsvTestDataDirectory, "empty.csv");
        private readonly string SimpleTestFile = Path.Combine(CsvTestDataDirectory, "simple.csv");
        private readonly string ThreeBlankLinesTestFile = Path.Combine(CsvTestDataDirectory, "three-blank-lines.csv");
        private readonly string EmptyFieldTestFile = Path.Combine(CsvTestDataDirectory, "empty-field.csv");
        private readonly string FieldNamesTestFile = Path.Combine(CsvTestDataDirectory, "field-names.csv");
        private readonly string QuotedTestFile = Path.Combine(CsvTestDataDirectory, "quoted.csv");
        private readonly string QuotedLineBreaksTestFile = Path.Combine(CsvTestDataDirectory, "quoted-linebreaks.csv");
        private readonly string SpacesTestFile = Path.Combine(CsvTestDataDirectory, "spaces.csv");
        private readonly string DifferentNumberFieldsTestFile = Path.Combine(CsvTestDataDirectory, "different-number-fields.csv");
        private readonly string MixedTestFile = Path.Combine(CsvTestDataDirectory, "mixed.csv");
        private readonly string UnixLineEndsTestFile = Path.Combine(CsvTestDataDirectory, "unix-line-ends.csv");
        private readonly string ErrorQuotesTestFile = Path.Combine(CsvTestDataDirectory, "error-quotes.csv");
        private readonly string DifferentQuotesFile = Path.Combine(CsvTestDataDirectory, "different-quotes.csv");

        [OneTimeSetUp]
        public void SetUp()
        {
            if (!Directory.Exists(CsvOutputDirectory))
            {
                Console.WriteLine ($"Creating CSV output directory : {CsvOutputDirectory}");
                Directory.CreateDirectory(CsvOutputDirectory);
            }
        }

        [SetUp]
        public void LogTestStart()
        {
            Console.WriteLine ($"Starting test: {TestContext.CurrentContext.Test.Name}");
        }

        [Test]
        public void TestCsvEmptyFile()
        {
            Console.WriteLine  ("Loading " + EmptyTestFile);
            using var fileStream = File.OpenRead(EmptyTestFile);
            var reader = new CsvReader(fileStream);

            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine  (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(records.Length, Is.EqualTo(1), "Wrong number of record in " + EmptyTestFile);
            Assert.That(records[0].Length, Is.EqualTo(1), "Wrong number of items on the first record");
            Assert.That(records[0][0].Length, Is.EqualTo(0), "Should be an empty string");
        }

        [Test]
        public void TestCsvSimpleFile()
        {
            Console.WriteLine  ("Loading " + SimpleTestFile);
            using var fileStream = File.OpenRead(SimpleTestFile);
            var reader = new CsvReader(fileStream);

            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine  (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(records.Length, Is.EqualTo(2), $"Wrong number of records in {SimpleTestFile}");
            Assert.That(TestUtils.CompareStringArray(new[] { "aaa", "bbb", "ccc" }, records[0]), Is.True, "the first record");
            Assert.That(TestUtils.CompareStringArray(new[] { "xxx", "yyy", "zzz" }, records[1]), Is.True, "the second record");
        }

        [Test]
        public void TestCsvThreeBlankLinesFile()
        {
            Console.WriteLine ($"Loading {ThreeBlankLinesTestFile}");
            using var fileStream = File.OpenRead(ThreeBlankLinesTestFile);
            var reader = new CsvReader(fileStream);

            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(records.Length, Is.EqualTo(3), $"Wrong number of records in {ThreeBlankLinesTestFile}");
            Assert.That(records[0].Length, Is.EqualTo(1), $"Wrong number of items on the first record in {ThreeBlankLinesTestFile}");
            Assert.That(records[0][0].Length, Is.EqualTo(0), $"Should be an empty string in {ThreeBlankLinesTestFile}");
            Assert.That(records[1].Length, Is.EqualTo(1), $"Wrong number of items on the second record in {ThreeBlankLinesTestFile}");
            Assert.That(records[1][0].Length, Is.EqualTo(0), $"Should be an empty string in {ThreeBlankLinesTestFile}");
            Assert.That(records[2].Length, Is.EqualTo(1), $"Wrong number of items on the third record in {ThreeBlankLinesTestFile}");
            Assert.That(records[2][0].Length, Is.EqualTo(0), $"Should be an empty string in {ThreeBlankLinesTestFile}");
        }

        [Test]
        public void TestCsvEmptyFieldFile()
        {
            Console.WriteLine ($"Loading {EmptyFieldTestFile}");
            using var fileStream = File.OpenRead(EmptyFieldTestFile);
            var reader = new CsvReader(fileStream);

            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(records.Length, Is.EqualTo(4), $"Wrong number of records in {EmptyFieldTestFile}");

            var index = 1;
            Assert.That(records[index - 1].Length, Is.EqualTo(3), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] { "aaa", "bbb", "ccc" }, records[index - 1]), Is.True, $"contents of record {index}");

            index++;
            Assert.That(records[index - 1].Length, Is.EqualTo(3), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] { "", "eee", "fff" }, records[index - 1]), Is.True, $"contents of record {index}");

            index++;
            Assert.That(records[index - 1].Length, Is.EqualTo(3), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] { "ggg", "", "jjj" }, records[index - 1]), Is.True, $"contents of record {index}");

            index++;
            Assert.That(records[index - 1].Length, Is.EqualTo(3), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] { "xxx", "yyy", "" }, records[index - 1]), Is.True, $"contents of record {index}");
        }

        [Test]
        public void TestCsvQuotedFile()
        {
            Console.WriteLine ($"Loading {QuotedTestFile}");
            using var fileStream = File.OpenRead(QuotedTestFile);
            var reader = new CsvReader(fileStream);

            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(records.Length, Is.EqualTo(2), $"Wrong number of records in {QuotedTestFile}");

            var index = 1;
            Assert.That(records[index - 1].Length, Is.EqualTo(2), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] { "2lines, 2 fields, With, commas", "With \"Quotes\"" }, records[index - 1]), Is.True, $"contents of record {index}");

            index++;
            Assert.That(records[index - 1].Length, Is.EqualTo(2), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(["With	Tabs", "Quotes\" and \"	\"TABS AND,commas"], records[index - 1]), Is.True, $"contents of record {index}");
        }

        [Test]
        public void TestCsvQuotedLineBreaksFile()
        {
            Console.WriteLine  ("Loading " + QuotedLineBreaksTestFile);
            using var fileStream = File.OpenRead(QuotedLineBreaksTestFile);
            var reader = new CsvReader(fileStream);

            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine  (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(records.Length, Is.EqualTo(1), $"Wrong number of records in {QuotedLineBreaksTestFile}");

            var index = 1;

            Assert.That(records[index - 1].Length, Is.EqualTo(3), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(
            new[]
            {
                "A longer entry with some new\n" +
                "lines\n" +
                "even\n" +
                "\n" +
                "a blank one.",
                                "",
                                "Quotes\n" +
                "\" and \n" +
                "\"\t\"TABS \n" +
                "AND,commas"
            }, records[index - 1]), Is.True, $"contents of record {index}");
        }

        [Test]
        public void TestCsvFieldNamesFile()
        {
            Console.WriteLine ($"Loading {FieldNamesTestFile}");
            using var fileStream = File.OpenRead(FieldNamesTestFile);
            var reader = new CsvReader(fileStream);

            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(records.Length, Is.EqualTo(3), $"Wrong number of records in {FieldNamesTestFile}");
            Assert.That(TestUtils.CompareStringArray(new[] { "Title", "Forename", "Last Name", "Age" }, records[0]), Is.True, $"the first record in {FieldNamesTestFile}");
            Assert.That(TestUtils.CompareStringArray(new[] { "Mr.", "John", "Smith", "21" }, records[1]), Is.True, $"the second record in {FieldNamesTestFile}");
            Assert.That(TestUtils.CompareStringArray(new[] { "Mrs.", "Jane", "Doe-Jones", "42" }, records[2]), Is.True, $"the third record in {FieldNamesTestFile}");
        }

        [Test]
        public void TestCsvSpacesFile()
        {
            Console.WriteLine ($"Loading {SpacesTestFile}");
            using var fileStream = File.OpenRead(SpacesTestFile);
            var reader = new CsvReader(fileStream);

            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(records.Length, Is.EqualTo(1), $"Wrong number of records in {SpacesTestFile}");
            Assert.That(TestUtils.CompareStringArray(new[] { "trailing ", " leading", " both " }, records[0]), Is.True, $"the first record in {SpacesTestFile}");
        }

        [Test]
        public void TestCsvDifferentNumberFieldsFile()
        {
            Console.WriteLine ($"Loading {DifferentNumberFieldsTestFile}");
            using var fileStream = File.OpenRead(DifferentNumberFieldsTestFile);
            var reader = new CsvReader(fileStream);

            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(records.Length, Is.EqualTo(4), $"Wrong number of records in {DifferentNumberFieldsTestFile}");

            var index = 1;
            Assert.That(records[index - 1].Length, Is.EqualTo(3), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] { "A", "B", "C" }, records[index - 1]), Is.True, $"contents of record {index}");

            index++;
            Assert.That(records[index - 1].Length, Is.EqualTo(4), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] { "a", "b", "c", "d" }, records[index - 1]), Is.True, $"contents of record {index}");

            index++;
            Assert.That(records[index - 1].Length, Is.EqualTo(2), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] { "9", "8" }, records[index - 1]), Is.True, $"contents of record {index}");

            index++;
            Assert.That(records[index - 1].Length, Is.EqualTo(5), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] { "1", "2", "3", "4", "5" }, records[index - 1]), Is.True, $"contents of record {index}");
        }

        [Test]
        public void TestCsvReadAll()
        {
            Console.WriteLine ($"Loading {MixedTestFile}");
            using var fileStream = File.OpenRead(MixedTestFile);
            var reader = new CsvReader(fileStream);

            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine (++line + ":" + TestUtils.ToString(record));
            }
        }

        [Test]
        public void TestCsvReadFieldAndRecord()
        {
            Console.WriteLine ($"Loading {MixedTestFile}");
            using var fileStream = File.OpenRead(MixedTestFile);
            var reader = new CsvReader(fileStream);

            Console.WriteLine ($"Line 1, Field 1: \"{reader.ReadField()}\"");
            Console.WriteLine ($"Rest of Line 1: \"{TestUtils.ToString(reader.ReadRecord())}\"");
            Console.WriteLine ("Rest of File: ");

            string[][] records = reader.ReadAll();
            var line = 1;
            foreach (var record in records)
            {
                Console.WriteLine (++line + ":" + TestUtils.ToString(record));
            }

            Console.WriteLine ("Done.");
        }

        [Test]
        public void TestCsvDifferentQuotesFile()
        {
            Console.WriteLine ($"Loading {DifferentQuotesFile}");
            using var fileStream = File.OpenRead(DifferentQuotesFile);
            var reader = new CsvReader(fileStream);

            reader.Quote = '*';
            string[][] records = reader.ReadAll();
            var line = 0;

            foreach (var record in records)
            {
                Console.WriteLine (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(records.Length, Is.EqualTo(3), "Wrong number of records in " + DifferentQuotesFile);

            var index = 1;
            Assert.That(records[index - 1].Length, Is.EqualTo(3), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] {"aaa", "bbb", "ccc"}, records[index - 1]), Is.True, $"contents of record {index}");

            index++;
            Assert.That(records[index - 1].Length, Is.EqualTo(3), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] {"", "new\nline", "quoted"}, records[index - 1]), Is.True, $"contents of record {index}");

            index++;
            Assert.That(records[index - 1].Length, Is.EqualTo(3), $"Wrong number of items on record {index}");
            Assert.That(TestUtils.CompareStringArray(new[] {"with", "\"other\"", "quo\"\"te"}, records[index - 1]), Is.True, $"contents of record {index}");
        }
    }
}
