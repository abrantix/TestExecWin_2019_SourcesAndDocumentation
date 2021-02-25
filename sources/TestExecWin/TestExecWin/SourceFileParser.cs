//------------------------------------------------------------------------------
//  Copyright(C) 2016  Gerald Fahrnholz
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program. If not, see <http://www.gnu.org/licenses/>.
//
//    Contact: http://www.gerald-fahrnholz.eu/impressum.php
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExecWin
{
    public class TokenInfo
    {
        public string token { get; set; } // description read as last from source file
        public bool exists { get; set; }
        public int startIdx { get; set; }
        public int endIdx { get; set; }
        public bool fatalError { get; set; }

        public TokenInfo()
        {
            token = "";
            exists = false;
            startIdx = -1;
            endIdx = -1;
            fatalError = false;
        }

        public void NotFound(int in_startIdx)
        {
            token = "";
            exists = false;
            startIdx = in_startIdx;
            endIdx = -1;
        }
    }

    public class MacroInfo
    {
        public string param1 { get; set; }
        public string param2 { get; set; }

        public MacroInfo(string in_param1, string in_param2)
        {
            param1 = in_param1;
            param2 = in_param2;
        }
    }

    public class ParseInfo
    {
        public string description { get; set; } // description read as last from source file
        public string FileFullPath { get; set; }
        public int lineIdx { get; set; }
        
        public bool BoostMacroDisabled { get; set; }
        public bool IsDataTestCase { get; set; }
        public Node<string> GroupNode { get; set; }

        public ParseInfo(string in_fileFullPath)
        {
            description = "";
            FileFullPath = in_fileFullPath;
            lineIdx = 0;
            GroupNode = new Node<string>(string.Empty);
            BoostMacroDisabled = false;
            IsDataTestCase = false;
            //testGroups = new System.Collections.Generic.List<string>(); // hierarchy of test groups
        }
        public int GetLineNum()
        {
            return lineIdx + 1;
        }
        public void AddTestGroup(string in_testGroup)
        {
            //testGroups.Add(in_testGroup);
            GroupNode.GetLeaf().Child = new Node<string>(in_testGroup);
        }
        public void RemoveLastTestGroup()
        {
            //testGroups.RemoveAt(testGroups.Count-1);
            GroupNode.GetLeaf().Parent.Child = null;
        }
        public void RemoveAllTestGroups()
        {
            //testGroups.Clear();
            GroupNode = new Node<string>(string.Empty);
        }
        public string GetTestGroupHierarchyString()
        {
            return GroupNode.GetPath();
        }

    }

    public class SourceFileParser
    {
        const string BoostDisabledDefaultString = "boost::unit_test::disabled()";
        private readonly IEventReceiver m_evenReceiver;
        private readonly HashSet<string> boostNameSpaces = new HashSet<string>();
        private readonly Dictionary<string, string> defines = new Dictionary<string, string>();
        private readonly List<string> boostDisabledStringPatterns = new List<string>();

        public SourceFileParser(IEventReceiver in_eventReceiver)
        {
            m_evenReceiver = in_eventReceiver;
        }

        /// [parser_scan_dir]
        public void ScanProjectDir(Project project)
        {
            WriteLine(3, "ScanProjectDir-Begin");

            // Recursively scan for .cpp files in project dir and all sub directories of project
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(project.SourceDirPath);
            var files = dir.GetFiles("*.cpp", System.IO.SearchOption.AllDirectories)
                            .Union(dir.GetFiles("*.h", System.IO.SearchOption.AllDirectories))
                            .Union(project.ProjectFiles.Where(x => x.Extension == ".cpp" || x.Extension == ".h" ));

                //flaky search for boost namespace (we try to catch *boost::unit_test::disabled() tests)
            boostNameSpaces.Clear();
            foreach (var file in files)
            {
                // Search for some macros and store them
                GrabDefinesAndBoostNamespaces(file);
            }

            GenerateBoostDisabledStringPatterns();

            foreach (var file in files)
            {
                // Search for test macros and store them within "projectInfo"
                ParseFile(project, file);
            }

            WriteLine(3, "ScanProjectDir-End");
        }
        /// [parser_scan_dir]

        private void GenerateBoostDisabledStringPatterns()
        {
            boostDisabledStringPatterns.Clear();
            boostDisabledStringPatterns.Add(BoostDisabledDefaultString);
            //generate patterns from namespace
            foreach (var boostNamespace in boostNameSpaces)
            {
                boostDisabledStringPatterns.Add($"{boostNamespace}::disabled()");
            }

            var namespacePatterns = boostDisabledStringPatterns.ToArray();

            //generate patterns from define (includes namespace)
            foreach (var keyValue in defines)
            {
                foreach (var namespacePattern in namespacePatterns)
                {
                    if(keyValue.Value.Contains(namespacePattern))
                    {
                        boostDisabledStringPatterns.Add(keyValue.Key);
                    }
                }
            }
        }

        private bool IsBoostMacroDisabled(string line)
        {
            return boostDisabledStringPatterns.Any(x => line.Contains(x));
        }

        private bool LineContainsMacro(string in_macro, string line)

        {
            int posStartMacro = line.IndexOf(in_macro);
            if (posStartMacro >= 0) // macro found
            {
                int posStartComment = line.IndexOf("//");
                if ((posStartComment >= 0) && (posStartComment < posStartMacro))
                {
                    WriteLine(3, "LineContainsMacro: macro " + in_macro + " within comment is ignored");
                    return false;
                }
                return true;
            }
            return false;
        }

        private enum CodeParseStatus
        { 
            Default,
            Slash1,
            Slash2,
            SlashStar,
            SlashStarStar,
            Quote,
            QuoteEscape
        }

        /// <summary>
        /// Remove comments but keeping line numbers
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string RemoveCommentsFromCode(string code)
        {
            var status = CodeParseStatus.Default;
            StringBuilder sb = new StringBuilder();
            //Remove "//" and /* .... */ from code 
            foreach (char c in code)
            {
                switch (status)
                {
                    case CodeParseStatus.Default:
                        switch (c)
                        {
                            case '/':
                                status = CodeParseStatus.Slash1;
                                break;

                            case '"':
                                sb.Append(c);
                                status = CodeParseStatus.Quote;
                                break;

                            case '\r':
                                sb.Append(c);
                                break;

                            case '\n':
                                sb.Append(c);
                                break;

                            default:
                                sb.Append(c);
                                break;
                        }
                        break;

                    case CodeParseStatus.Slash1:
                        switch (c)
                        {
                            case '/':
                                status = CodeParseStatus.Slash2;
                                break;

                            case '*':
                                status = CodeParseStatus.SlashStar;
                                break;

                            case '\r':
                                sb.Append(c);
                                break;

                            case '\n':
                                sb.Append(c);
                                break;

                            default:
                                sb.Append('/');
                                sb.Append(c);
                                status = CodeParseStatus.Default;
                                break;
                        }
                        break;

                    case CodeParseStatus.Slash2:
                        switch (c)
                        {
                            case '\r':
                                sb.Append(c);
                                break;
                            
                            case '\n':
                                sb.Append(c);
                                status = CodeParseStatus.Default;
                                break;

                            default:
                                break;
                        }
                        break;

                    case CodeParseStatus.SlashStar:
                        switch (c)
                        {
                            case '*':
                                status = CodeParseStatus.SlashStarStar;
                                break;

                            case '\r':
                                sb.Append(c);
                                break;

                            case '\n':
                                sb.Append(c);
                                break;

                            default:
                                break;
                        }
                        break;

                    case CodeParseStatus.SlashStarStar:
                        switch (c)
                        {
                            case '*':
                                break;

                            case '/':
                                status = CodeParseStatus.Default;
                                break;

                            case '\r':
                                sb.Append(c);
                                break;

                            case '\n':
                                sb.Append(c);
                                break;

                            default:
                                status = CodeParseStatus.SlashStar;
                                break;
                        }
                        break;

                    case CodeParseStatus.Quote:
                        switch (c)
                        {
                            case '"':
                                sb.Append(c);
                                status = CodeParseStatus.Default;
                                break;

                            case '\\':
                                status = CodeParseStatus.QuoteEscape;
                                break;

                            case '\r':
                                sb.Append(c);
                                break;

                            case '\n':
                                sb.Append(c);
                                break;

                            default:
                                sb.Append(c);
                                break;
                        }
                        break;

                    case CodeParseStatus.QuoteEscape:
                        sb.Append(c);
                        status = CodeParseStatus.Quote;
                        break;
                    
                    default:
                        throw new InvalidProgramException();
                }
            }

            return sb.ToString();
        }

        private void GrabDefinesAndBoostNamespaces(System.IO.FileInfo in_file)
        {
            var text = RemoveCommentsFromCode(System.IO.File.ReadAllText(in_file.FullName));
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("namespace "))
                {
                    if (trimmedLine.Contains("boost::unit_test"))
                    {
                        var boostNamespace = trimmedLine.Substring(trimmedLine.IndexOf("namespace "), trimmedLine.IndexOf("=")).Trim();
                        boostNameSpaces.Add(boostNamespace);
                    }
                }
                else if (trimmedLine.StartsWith("#define "))
                {
                    //skip "#define "
                    var keyValue = trimmedLine.Substring(8);
                    int spacer = keyValue.IndexOf(' ');
                    if (spacer == -1)
                    {
                        spacer = keyValue.IndexOf('\t');
                    }
                    if (spacer > 0)
                    {
                        var key = keyValue.Substring(0, spacer).Trim();
                        var value = keyValue.Substring(spacer).Trim();
                        if (!defines.ContainsKey(key))
                        {
                            defines.Add(key, value);
                        }
                    }
                    else
                    {
                        if (!defines.ContainsKey(keyValue.Trim()))
                        {
                            defines.Add(keyValue.Trim(), string.Empty);
                        }
                    }
                }
            }
        }

        /// [parser_read_file]
        private void ParseFile(Project project, System.IO.FileInfo in_file)
        {
            WriteLine(3, "ParseFile-Begin " + in_file.FullName);
            WriteLine(2, "File " + in_file.FullName);

            var text = RemoveCommentsFromCode(System.IO.File.ReadAllText(in_file.FullName));

            // Read whole source file into array of strings
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int numLines = lines.Length;

            // During parsing we store information about found description,
            // test suite and current line index within ParseInfo
            ParseInfo parseInfo = new ParseInfo(in_file.FullName);
            bool currentSuiteDisabled = false;
            while (parseInfo.lineIdx < numLines)
            {
                string line = lines[parseInfo.lineIdx];
                parseInfo.BoostMacroDisabled = IsBoostMacroDisabled(line);
                parseInfo.IsDataTestCase = false;
                if (LineContainsMacro("TTB_TEST_FUNC_DESC",line))
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "TTB_TEST_FUNC_DESC", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param2;
                        project.AppType = AppType.TTB;
                        parseInfo.RemoveAllTestGroups();
                        parseInfo.AddTestGroup(in_file.Name);
                        CreateTestFuncInfo(project, parseInfo, currentSuiteDisabled);
                    }
                }
                else if (LineContainsMacro("TTB_TEST_FUNC",line))
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "TTB_TEST_FUNC", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param1;
                        project.AppType = AppType.TTB;
                        parseInfo.RemoveAllTestGroups();
                        parseInfo.AddTestGroup(in_file.Name);
                        CreateTestFuncInfo(project, parseInfo, currentSuiteDisabled);
                    }
                }
                else if (LineContainsMacro("BOOST_AUTO_TEST_CASE_TEMPLATE", line))
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "BOOST_AUTO_TEST_CASE_TEMPLATE", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param1;
                        // Add "*" for correct call of test case
                        parseInfo.description += "*";
                        project.AppType = AppType.BOOST;
                        CreateTestFuncInfo(project, parseInfo, currentSuiteDisabled);
                    }
                }
                else if (LineContainsMacro("BOOST_AUTO_TEST_CASE",line))
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "BOOST_AUTO_TEST_CASE", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param1;
                        project.AppType = AppType.BOOST;
                        CreateTestFuncInfo(project, parseInfo, currentSuiteDisabled);
                    }
                }
                else if (LineContainsMacro("BOOST_DATA_TEST_CASE", line))
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "BOOST_DATA_TEST_CASE", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param1;
                        project.AppType = AppType.BOOST;
                        parseInfo.IsDataTestCase = true;
                        CreateTestFuncInfo(project, parseInfo, currentSuiteDisabled);
                    }
                }
                else if (LineContainsMacro("TTB_BOOST_TEST_CASE",line))
                /// ...
                /// [parser_read_file]
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "TTB_BOOST_TEST_CASE", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param1;
                        project.AppType = AppType.BOOST;
                        CreateTestFuncInfo(project, parseInfo, currentSuiteDisabled);
                    }
                }
                else if (LineContainsMacro("BOOST_FIXTURE_TEST_CASE", line))
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "BOOST_FIXTURE_TEST_CASE", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param1;
                        project.AppType = AppType.BOOST;
                        CreateTestFuncInfo(project, parseInfo, currentSuiteDisabled);
                    }
                }
                else if (LineContainsMacro("TTB_BOOST_FIXTURE_TEST_CASE", line))
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "TTB_BOOST_FIXTURE_TEST_CASE", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param1;
                        project.AppType = AppType.BOOST;
                        CreateTestFuncInfo(project, parseInfo, currentSuiteDisabled);
                    }
                }
                else if (LineContainsMacro("RUN_TEST_CASE",line))
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "TTB_BOOST_TEST_CASE", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param1;
                        project.AppType = AppType.BOOST;
                        CreateTestFuncInfo(project, parseInfo, currentSuiteDisabled);
                    }
                }
                else if(LineContainsMacro("BOOST_AUTO_TEST_SUITE_END",line))
                {
                    parseInfo.RemoveLastTestGroup();
                }
                else if (LineContainsMacro("BOOST_AUTO_TEST_SUITE",line))
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "BOOST_AUTO_TEST_SUITE", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param1;
                        parseInfo.AddTestGroup(parseInfo.description);
                        currentSuiteDisabled = parseInfo.BoostMacroDisabled;
                    }
               }
                else if (LineContainsMacro("BOOST_FIXTURE_TEST_SUITE",line))
                {
                    MacroInfo macro = ReadMacro(parseInfo,
                        "BOOST_FIXTURE_TEST_SUITE", line, lines);
                    if (macro != null)
                    {
                        parseInfo.description = macro.param1;
                        parseInfo.AddTestGroup(parseInfo.description);
                        currentSuiteDisabled = parseInfo.BoostMacroDisabled;
                    }
                }

                ++parseInfo.lineIdx;
            }

            WriteLine(3, "ParseFile-End projectInfo.appType=" + project.AppType);
        }

        private bool ReadNextToken(TokenInfo tokenInfo,  string line)
        {
            tokenInfo.token = "";

            // Skip leading white space
            int posStart = tokenInfo.startIdx;
            while (posStart < line.Length)
            {
                if ((line[posStart] == ' ') || (line[posStart] =='\t'))
                {
                    ++posStart;
                }
                else
                {
                    break;
                }
            }
            if (posStart >= line.Length)
            {
                tokenInfo.NotFound(posStart);
                return false;
            }

            // Start reading
            bool readString = (line[posStart] == '"');
            bool endOfStringReached = false;
            tokenInfo.token += line[posStart];
            tokenInfo.startIdx = posStart;
            int idx = posStart + 1;
            if ((line[posStart]==',') || (line[posStart] == ')'))
            {
                // token is already comolete
                tokenInfo.endIdx = posStart;
                tokenInfo.exists = true;
                return true;
            }
            else if (readString) // at least another '"' must be found
            {
                while ((idx < line.Length) && (!endOfStringReached))
                {
                    tokenInfo.token += line[idx];
                    endOfStringReached = line[idx] == '"';
                    ++idx;
                }
                if (endOfStringReached)
                {
                    tokenInfo.endIdx = idx-1;
                    tokenInfo.exists = true;
                    return true;
                }
                else // (idx >= line.Length)
                {
                    WriteLine(1, "ERROR: unsupported syntax: string not closed");
                    tokenInfo.NotFound(posStart);
                    tokenInfo.fatalError = true;
                    return false;
                }
            }
            else // read until " ", ",", ")" or end of line is found
            {
                string endChars = " ,)";
                while ((idx < line.Length) && (endChars.IndexOf(line[idx], 0) < 0))
                {
                    tokenInfo.token += line[idx];
                    ++idx;
                }
                tokenInfo.endIdx = idx - 1;
                tokenInfo.exists = true;
                return true;
            }
        }

        private string GetContextInfo(ParseInfo parseInfo)
        {
            return " [" + parseInfo.FileFullPath + " line " + (parseInfo.lineIdx + 1) + "]";
        }

        // Reads all macros with format: MACRO(), MACRO(param), MACRO(param,param), MACRO( "witin string, you can usse,  ()",  param)
        // White space is automatically skipped, strings containing arbitrary chars are parsed as a single parameter,
        // macro is terminated with closing brace ")", macro may have line breaks (up to 3 lines).  
        private MacroInfo ReadMacro(ParseInfo parseInfo, string in_macro, string in_line, string[] in_lines)
        {
            WriteLine(3, "ReadMacro line " + parseInfo.lineIdx + ": " + in_line);

            string line = in_line;
            int posStart = line.IndexOf(in_macro) + 1;
            posStart = line.IndexOf("(", posStart);
            if (posStart < 0) // give up
            {
                WriteLine(1, "WARNING: unsupported syntax: '( 'not found" + GetContextInfo(parseInfo));
                return null;
            }

            TokenInfo tokenInfo = new TokenInfo();
            tokenInfo.startIdx = posStart + 1;

            // now start reading  after the opening brace "("
            int maxNumLinesAllowedForMacro = 3;
            bool endReached = false;
            int paramIdx = 0;
            string[] param = new string[] { "", "" };
            while (!endReached && !tokenInfo.fatalError && (maxNumLinesAllowedForMacro) > 0)
            {
                if (ReadNextToken(tokenInfo, line))
                {
                    WriteLine(3, "ReadMacro token= " + tokenInfo.token);
                    if ((tokenInfo.token != ",") && (tokenInfo.token != ")"))
                    {
                        if (paramIdx <= 1)
                        {
                            param[paramIdx] = tokenInfo.token;
                            ++paramIdx;
                        }
                        else
                        {
                            WriteLine(3, "ReadMacro unexpected param is ignored: " + tokenInfo.token + GetContextInfo(parseInfo));
                        }
                    }
                    else
                    {
                        endReached = tokenInfo.token == ")";
                    }
                    tokenInfo.startIdx = tokenInfo.endIdx + 1;
                }
                else
                {
                    if (!tokenInfo.fatalError) // check also next line
                    {
                        --maxNumLinesAllowedForMacro;
                        if (maxNumLinesAllowedForMacro > 0)
                        {
                            ++parseInfo.lineIdx;
                            if (parseInfo.lineIdx >= in_lines.Length)
                            {
                                WriteLine(1, "WARNING: unsupported syntax: end of file reached" + GetContextInfo(parseInfo));
                                return null;
                            }
                            string nextLine = in_lines[parseInfo.lineIdx];
                            line = line + nextLine;
                        }
                    }
                }
            }
            if (!endReached)
            {
                WriteLine(1, "WARNING: unsupported syntax: end of macro not found" + GetContextInfo(parseInfo));
                return null;
            }
            return new MacroInfo(param[0],param[1]);
        }

        /// [parser_func_info]
        private void CreateTestFuncInfo(Project project, ParseInfo parseInfo, bool currentSuiteDisabled)
        {
            //Create test group path(s) if not existent
            bool isRoot = true;
            var currentParseNode = parseInfo.GroupNode;
            var currentTargetNode = project.TestGroups;
            while (true)
            {
                //Root may have different namings
                if (isRoot || parseInfo.GroupNode.Value == project.TestGroups.Value.Name)
                {
                    isRoot = false;
                    if (currentParseNode.HasChild())
                    {
                        var targetNode = currentTargetNode.Childs.FirstOrDefault(x => x.Value.Name == currentParseNode.Child.Value);
                        if (targetNode != null)
                        {
                            //target node exists - use as new seek point
                            currentTargetNode = targetNode;
                        }
                        else
                        {
                            //target not doesn't exist - create new
                            var newGroupEntry = new TestGroupEntry(project.AppType == AppType.BOOST, currentParseNode.Child.Value);
                            currentTargetNode = currentTargetNode.AddChildNode(new NodeList<TestGroupEntry>(newGroupEntry));
                            newGroupEntry.NodeList = currentTargetNode;
                        }
                        currentParseNode = currentParseNode.Child;
                    }
                    else
                    {
                        //This is the end...
                        break;
                    }
                }
            }

            // Store the found test function description
            TestFuncEntry tf = new TestFuncEntry(project.AppType == AppType.BOOST, currentTargetNode.Value, parseInfo.BoostMacroDisabled || currentSuiteDisabled);
            tf.TestFunction = parseInfo.description;
            tf.FileFullPath = parseInfo.FileFullPath;
            tf.IsDataTestCase = parseInfo.IsDataTestCase;
            tf.LineNum = parseInfo.GetLineNum();
            currentTargetNode.Value.testFuncs.Add(tf);

            WriteLine(3, "CreateTestFuncInfo: " + tf.TestFunction + " within group: " + tf.TestGroup.NodeList.GetPath()
                + " file: " + tf.FileFullPath + " line: " + tf.LineNum);
        }
        /// [parser_func_info]

        private void WriteLine(int in_outputLevel, String in_info)
        {
            m_evenReceiver?.WriteLine(in_outputLevel, in_info);
        }

    }
}
