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
 * Kajbity is a trademark of Williams Technologies Limited.
 *
 * http://www.kajabity.com
 */

namespace Kajabity.Tools.Csv.Tests
{
    [TestFixture]
    public class CsvWriterTest
    {
        private static readonly string CsvTestDataDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "Csv");
        private static readonly string CsvOutputDirectory = Path.GetTempPath();

        [OneTimeSetUp]
        public void SetUp()
        {
            if (!Directory.Exists(CsvOutputDirectory))
            {
                Console.WriteLine ($"Creating CSV output directory : {CsvOutputDirectory}" );
                Directory.CreateDirectory(CsvOutputDirectory);
            }
        }

        [SetUp]
        public void LogTestStart()
        {
            Console.WriteLine ($"Starting test: {TestContext.CurrentContext.Test.Name}");
        }

        [Test]
        public void TestCsvWriter()
        {
            var filename = Path.Combine(CsvTestDataDirectory, "mixed.csv");
            Console.WriteLine ("Loading " + filename);
            using var inStream = File.OpenRead(filename);
            var reader = new CsvReader(inStream);
            string[][] records = reader.ReadAll();

            var outName = Path.Combine(CsvOutputDirectory, "test-writer.csv");
            using var outStream = File.OpenWrite(outName);
            outStream.SetLength(0L);

            var writer = new CsvWriter(outStream);
            writer.WriteAll(records);
            outStream.Flush();
        }

        [Test]
        public void TestWriteRecord()
        {
            var filename = Path.Combine(CsvOutputDirectory, "test-write-record.csv");
            var record = new[] { "AAAA", "BBBB", "CCCC" };
            const int lenRecord = 14; // Strings, commas.

            // Create the temp file (or overwrite if already there).
            using (var stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                stream.SetLength(0);
            }

            // Check it's empty.
            var info = new FileInfo(filename);
            Assert.That(info.Length, Is.EqualTo(0), "File length not zero.");

            // Open for append
            using (var stream = File.OpenWrite(filename))
            {
                var writer = new CsvWriter(stream);
                writer.WriteRecord(record);
                stream.Flush();
            }

            // Check it's not empty.
            info = new FileInfo(filename);
            Assert.That(info.Length, Is.EqualTo(lenRecord), "File length not increased.");
        }

        [Test]
        public void TestWriteAlternateSeparator()
        {
            var filename = Path.Combine(CsvOutputDirectory, "test-write-alternate-separator.csv");
            var record = new[] { "AA,AA original separator", "BB|BB new separator", "CCCC" };

            Console.WriteLine ("Creating empty " + filename);
            using (var stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                stream.SetLength(0);
            }

            // Check it's empty.
            var info = new FileInfo(filename);
            Assert.That(info.Length, Is.EqualTo(0), "File length not zero.");

            // Open for append
            Console.WriteLine ("Writing " + filename);
            using (var stream = File.OpenWrite(filename))
            {
                var writer = new CsvWriter(stream);
                writer.Separator = '|';
                writer.WriteRecord(record);
                stream.Flush();
            }

            Console.WriteLine ("Loading " + filename);
            using var readStream = File.OpenRead(filename);
            var reader = new CsvReader(readStream);
            reader.Separator = '|';
            string[][] records = reader.ReadAll();

            Assert.That(records.Length, Is.EqualTo(1), "Should only be one record.");
            Console.WriteLine ("Read :" + TestUtils.ToString(records[0]));
            Assert.That(records[0].Length, Is.EqualTo(record.Length), $"Should be {record.Length} fields in record.");

            for (var fieldNo = 0; fieldNo < record.Length; fieldNo++)
            {
                Assert.That(records[0][fieldNo], Is.EqualTo(record[fieldNo]), $"Field {fieldNo} Should be {record[fieldNo]}");
            }
        }

        [Test]
        public void TestWriteAlternateQuote()
        {
            var filename = Path.Combine(CsvOutputDirectory, "test-write-alternate-quote.csv");
            string[][] recordsOut =
            [
                ["aaa", "bb*b", "ccc"],
                ["", "new" + Environment.NewLine + "line", "quoted"],
                ["with", "\"other\"", "quo\"\"te"]
            ];

            Console.WriteLine ("Creating " + filename);
            using (var stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                stream.SetLength(0);
                var writer = new CsvWriter(stream);
                writer.Quote = '*';
                writer.QuoteLimit = -1;
                writer.WriteAll(recordsOut);
                stream.Flush();
            }

            Console.WriteLine ("Loading " + filename);
            using var readStream = File.OpenRead(filename);
            var reader = new CsvReader(readStream);
            reader.Quote = '*';
            string[][] recordsIn = reader.ReadAll();

            var line = 0;
            foreach (var record in recordsIn)
            {
                Console.WriteLine (++line + ":" + TestUtils.ToString(record));
            }

            Assert.That(recordsIn.Length, Is.EqualTo(3), $"Wrong number of records in {filename}");

            var index = 0;
            Assert.That(recordsIn[index].Length, Is.EqualTo(3), $"Wrong number of items on record {index + 1}");
            Assert.That(TestUtils.CompareStringArray(recordsOut[index], recordsIn[index]), Is.True, $"contents of record {index + 1}");

            index++;
            Assert.That(recordsIn[index].Length, Is.EqualTo(3), $"Wrong number of items on record {index + 1}");
            Assert.That(TestUtils.CompareStringArray(recordsOut[index], recordsIn[index]), Is.True, $"contents of record {index + 1}");

            index++;
            Assert.That(recordsIn[index].Length, Is.EqualTo(3), $"Wrong number of items on record {index + 1}");
            Assert.That(TestUtils.CompareStringArray(recordsOut[index], recordsIn[index]), Is.True, $"contents of record {index + 1}");
        }
    }
}
