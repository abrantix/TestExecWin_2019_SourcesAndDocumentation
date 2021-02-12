\ingroup GrpHiddenPages
\author Gerald Fahrnholz

\defgroup GrpParser Class SourceFileParser - look for test macros within files of project dir
@{
\ingroup GrpDesign

Overview:
- \ref parser_scan                   "Scan project dir for .cpp files"
- \ref parser_read_file              "Parsing a source file"
- \ref parser_info_test_case         "Storing info for each test case"

\anchor parser_scan 
<h2>Scan project dir for .cpp files</h2>
\snippet TestExecWin/SourceFileParser.cs parser_scan_dir

\anchor parser_read_file 
<h2>Parsing a source file</h2>
During parsing a hard coded set of test macros is searched. Depending on the type
of found macro the relevant test case information is extracted at different
positions within the macro:
\snippet TestExecWin/SourceFileParser.cs parser_read_file

\anchor parser_info_test_case 
<h2>Storing info for each test case</h2>
\snippet TestExecWin/SourceFileParser.cs parser_func_info
@}
