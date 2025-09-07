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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Kajabity.Tools.Csv
{
    /// <summary>
    /// This class reads CSV formatted data from an input stream.  It can read individual fields in a row,
    /// a full row or the whole file.  Data can only be read once - so if the first field in a row is read,
    /// it won't be part of the row if that is read next.
    ///
    /// The term Row is used rather than line because quoted fields can include line breaks (real, not
    /// escaped) so that one row may be spread across multiple lines.
    /// </summary>
    public class CsvReader
    {
        //	---------------------------------------------------------------------
        #region The State Machine (ATNP)
        //	---------------------------------------------------------------------

        //	All the states.
        private enum State
        {
            Start,
            Field,
            Quoted,
            DoubleQuote,
            SkipTrailingSpace,
            EndField,
            EndLine,
            EndFile
        }

        /// <summary>
        /// Used in debug and error reporting.
        /// </summary>
        private static readonly string[] StateNames =
        {
            "Start", "Field", "Quoted", "Double Quote", "SkipTrailingSpace", "End of Field", "End of Line", "End of File"
        };

        //	The different types of matcher used.
        private enum Match
        {
            None,
            EOF,
            Separator,
            LineFeed,
            DoubleQuote,
            WhiteSpace,
            Any
        }

        //	Actions performed when a character is matched.
        private enum Action
        {
            None,
            SaveField,
            SaveLine,
            AppendToField,
            AppendLineFeedToField
        }

        /// <summary>
        /// The State Machine - an array of states, each an array of transitions, and each of those
        /// an array of integers grouped in threes - { match condition, next state, action to perform }.
        /// </summary>
        private static readonly (Match match, State nextState, Action action)[][] States = new (Match, State, Action)[][]
        {
            new[] { // Start
                (Match.Separator, State.EndField, Action.SaveField),
                (Match.DoubleQuote, State.Quoted, Action.None),
                (Match.LineFeed, State.EndLine, Action.SaveLine),
                (Match.EOF, State.EndFile, Action.SaveLine),
                (Match.Any, State.Field, Action.AppendToField),
            },
            new[] { // Field
                (Match.Separator, State.EndField, Action.SaveField),
                (Match.LineFeed, State.EndLine, Action.SaveLine),
                (Match.EOF, State.EndFile, Action.SaveLine),
                (Match.Any, State.Field, Action.AppendToField),
            },
            new[] { // Quoted
                (Match.DoubleQuote, State.DoubleQuote, Action.None),
                (Match.EOF, State.EndFile, Action.SaveLine),
                (Match.LineFeed, State.Quoted, Action.AppendLineFeedToField),
                (Match.Any, State.Quoted, Action.AppendToField),
            },
            new[] { // DoubleQuote
                (Match.DoubleQuote, State.Quoted, Action.AppendToField),
                (Match.EOF, State.EndFile, Action.SaveLine),
                (Match.Separator, State.EndField, Action.SaveField),
                (Match.LineFeed, State.EndLine, Action.SaveLine),
            },
            new[] { // SkipTrailingSpace
                (Match.EOF, State.EndFile, Action.SaveLine),
                (Match.Separator, State.EndField, Action.SaveField),
                (Match.LineFeed, State.EndLine, Action.SaveLine),
                (Match.WhiteSpace, State.SkipTrailingSpace, Action.None),
            },
            new[] { // EndField
                (Match.None, State.Start, Action.None),
            },
            new[] { // EndLine
                (Match.None, State.Start, Action.None),
            },
            // EndFile: no transitions
        };

        #endregion

        //	---------------------------------------------------------------------
        //  Constants
        //	---------------------------------------------------------------------

        /// <summary>
        /// The size of the buffer used to read the input data.
        /// </summary>
        private const int BufferSize = 1000;

        //	---------------------------------------------------------------------
        //  The result.
        //	---------------------------------------------------------------------

        private StringBuilder fieldBuilder = new StringBuilder();
        private List<string> fieldList = new List<string>();
        private List<string[]> rowList = new List<string[]>();

        //	---------------------------------------------------------------------
        //  Options
        //	---------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the separator character used in the CSV stream -
        /// default value is a comma (',').
        /// </summary>
        public int Separator = CsvConstants.DEFAULT_SEPARATOR_CHAR;

        /// <summary>
        /// Gets or sets the quote character used in the CSV stream - default
        /// value is a double quote ('"').
        /// </summary>
        public int Quote = CsvConstants.DEFAULT_QUOTE_CHAR;

        //	---------------------------------------------------------------------
        //  Working data.
        //	---------------------------------------------------------------------

        /// <summary>
        /// The starting state for the parser engine.
        /// </summary>
        private State state = State.Start;

        /// <summary>
        /// The input stream that characters are read and parsed from.
        /// </summary>
        private TextReader inStream = null;

        /// <summary>
        /// Stores the next character after it's been peeked until it is removed by NextChar().
        /// </summary>
        private int savedChar;

        /// <summary>
        /// A flag to indicate whether or not there is a savedChar.
        /// </summary>
        private bool saved = false;

        /// <summary>
        /// A variable to hold on to the 2nd LineFeed character - if there is one.
        /// </summary>
        private int ExtraLinefeedChar = 0;

        //  ---------------------------------------------------------------------
        //  Constructors.
        //  ---------------------------------------------------------------------

        /// <summary>
        /// Construct a inStream.
        /// </summary>
        /// <param name="stream">The input stream to read from.</param>
        public CsvReader(Stream stream)
        {
            inStream = new StreamReader(stream);
        }

        /// <summary>
        /// Construct a CsvReader with a TextReader (e.g. StreamReader) allowing the caller
        /// to set the encoding, buffer size, etc.
        /// </summary>
        /// <param name="reader">an instance of a TextReader where the CSV data will be loaded from.</param>
        public CsvReader( TextReader reader )
        {
            inStream = reader;
        }

        //  ---------------------------------------------------------------------
        //  Methods.
        //  ---------------------------------------------------------------------

        /// <summary>
        /// Reads the next field on the current line - or null after the end of the line.  The
        /// field will not be part of the next ReadLine or ReadFile.
        /// </summary>
        /// <returns>the next field or null after the end of the record.</returns>
        public string ReadField()
        {
            // Check we haven't passed the end of the line/file.
            if (state > State.EndField)
            {
                return null;
            }

            // Parse the next field.
            Parse(State.EndField);

            // Return and remove the last field.
            string field = fieldList[fieldList.Count - 1];
            //Debug.WriteLine( "ReadField: \"" + field + "\"" );
            fieldList.RemoveAt(fieldList.Count - 1);
            return field;
        }

        /// <summary>
        /// Read to the end of the current record, if any.
        /// </summary>
        /// <returns>The current record - or null if at end of file.</returns>
        public string[] ReadRecord()
        {
            // Check we haven't passed the end of the file.
            if (state > State.EndLine)
            {
                return null;
            }

            // Parse to the end of the current line.
            Parse(State.EndLine);

            // Return and remove the last row.
            string[] record = rowList[rowList.Count - 1];
            rowList.RemoveAt(rowList.Count - 1);
            return record;
        }

        /// <summary>
        /// Reads all fields and records from the CSV input stream from the current location.
        /// </summary>
        /// <returns>an array of string arrays, each row representing a row of values from the CSV file - or null if
        /// already at the end of the file.</returns>
        public string[][] ReadAll()
        {
            // Check we haven't passed the end of the file.
            if (state == State.EndFile)
            {
                return null;
            }

            // Parse to the end of the file.
            Parse(State.EndFile);

            // Return and remove the last field.
            string[][] records = rowList.ToArray();
            rowList.Clear();
            return records;
        }

        /// <summary>
        /// Parse the input CSV stream from the current position until the final state is
        /// reached.  Intended to allow parsing to End of Field, End of Record or End of File.
        /// </summary>
        /// <param name="finalSate">Specify where the parser should stop (or pause)
        /// by indicating which state to finish on.</param>
        /// <exception cref="T:CsvParseException">Thrown when an unexpected/invalid character
        /// is encountered in the input stream.</exception>
        private void Parse(State finalSate)
        {
            if (finalSate >= State.EndFile)
            {
                finalSate = State.EndFile;
            }

            bool lambda = false;
            int ch = -1;
            do
            {
                bool matched = false;

                if (lambda)
                {
                    lambda = false;
                }
                else
                {
                    ch = NextChar();
                }


                foreach (var transition in States[(int)state])
                {
                    if (Matches(transition.match, ch))
                    {
                        //Debug.WriteLine( StateNames[(int)state] + ", " + ch + (ch > 20 ? " (" + (char)ch + ")" : "") );
                        matched = true;

                        if (transition.match == Match.None)
                        {
                            lambda = true;
                        }

                        DoAction(transition.action, ch);

                        state = transition.nextState;
                        break;
                    }
                }

                if (!matched)
                {
                    throw new CsvParseException("Unexpected character at state " + StateNames[(int)state] + ": <<<" + (char)ch + ">>>");
                }
            }
            while (state < finalSate);
        }

        /// <summary>
        /// Tests if the current character matches a test (one of the MATCH_* tests).
        /// </summary>
        /// <param name="match">The number of the MATCH_* test to try.</param>
        /// <param name="ch">The character to test.</param>
        /// <returns></returns>
        private bool Matches(Match match, int ch)
        {
            ExtraLinefeedChar = 0;

            switch (match)
            {
                case Match.None:
                    return true;

                case Match.Separator:
                    return ch == Separator;

                case Match.EOF:
                    return ch == -1;

                case Match.LineFeed:
                    if (ch == '\r')
                    {
                        if (PeekChar() == '\n')
                        {
                            ExtraLinefeedChar = '\n';
                            saved = false;
                        }
                        return true;
                    }
                    if (ch == '\n')
                    {
                        return true;
                    }
                    return false;

                case Match.DoubleQuote:
                    return ch == Quote;

                case Match.WhiteSpace:
                    return ch == ' ' || ch == '\t' || ch == '\v';

                case Match.Any:
                    return true;

                default:  // Allows the match char to be the 'MATCH' parameter.
                          //return ch == match;
                    return false;
            }
        }

        /// <summary>
        /// Performs the action associated with a state transition.
        /// </summary>
        /// <param name="action">The number of the action to perform.</param>
        /// <param name="ch">The character matched in the state.</param>
        private void DoAction(Action action, int ch)
        {
            switch (action)
            {
                case Action.None:
                    break;

                case Action.SaveField:  // Append the field to the fieldList as a String.
                                        //Debug.WriteLine( "ACTION_SaveField: \"" + fieldBuilder.ToString() + "\"" );
                    fieldList.Add(fieldBuilder.ToString());
                    fieldBuilder.Length = 0;
                    break;

                case Action.SaveLine:   // Append the line to the rowList as an array of strings.
                                        //Debug.Write( "ACTION_SaveLine: \"" + fieldBuilder.ToString() + "\"" );
                    fieldList.Add(fieldBuilder.ToString());
                    fieldBuilder.Length = 0;

                    //Debug.WriteLine( " - " + fieldList.Count + " fields" );
                    rowList.Add(fieldList.ToArray());
                    fieldList.Clear();
                    break;

                case Action.AppendToField:
                    fieldBuilder.Append((char)ch);
                    break;

                case Action.AppendLineFeedToField:
                    fieldBuilder.Append((char)ch);
                    if (ExtraLinefeedChar > 0)
                    {
                        fieldBuilder.Append((char)ExtraLinefeedChar);
                    }    
                    break;
            }
        }

        /// <summary>
        /// Returns and removes the next character from the input stream - including any that have been peeked and pushed back.
        /// </summary>
        /// <returns>The next character from the stream.</returns>
        private int NextChar()
        {
            if (saved)
            {
                saved = false;
                return savedChar;
            }

            return inStream.Read();
        }

        /// <summary>
        /// Retuns but doesn't remove the next character from the stream.
        /// This character will be returned every time this method is called until it is returned by NextChar().
        /// </summary>
        /// <returns>The next character from the stream.</returns>
        private int PeekChar()
        {
            if (saved)
            {
                return savedChar;
            }

            saved = true;
            return savedChar = inStream.Read();
        }
    }
}
