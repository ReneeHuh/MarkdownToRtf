using Html2Markdown.Replacement;
using Markdig;
using Markdig.Extensions.Mathematics;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Linq;
using System.Text;

namespace MarkdownToRtf
{
    public static class MarkdownToRtfConverter
    {
        private static string Header1 = (@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat{\fonttbl{\f0\fnil\fcharset0 Calibri;}{\f1\fnil\fcharset0 Courier New;}{\f2\fnil\fcharset2 Symbol;}}");
        private static string Header2 = @"{\*\generator Riched20 10.0.26100}\viewkind4\uc1 ";
        private static string Footer = @"}";

        private static string ParagraphStart = "\\pard";
        private static string ParagraphEnd = "\\par";

        private static int[] HeadingSizes = { 36, 35, 34, 33, 32, 31 };
        private static string FontSizeControlChar = $"\\fs";
        private static string FontSizeH1 = $"{FontSizeControlChar}{HeadingSizes[0]}";
        private static string FontSizeH2 = $"{FontSizeControlChar}{HeadingSizes[1]}";
        private static string FontSizeH3 = $"{FontSizeControlChar}{HeadingSizes[2]}";
        private static string FontSizeH4 = $"{FontSizeControlChar}{HeadingSizes[3]}";
        private static string FontSizeH5 = $"{FontSizeControlChar}{HeadingSizes[4]}";
        private static string FontSizeH6 = $"{FontSizeControlChar}{HeadingSizes[5]}";

        private static string FontSizeDefault = "\\fs25 ";
        private static string FontSizeStandardCode = "\\fs25 ";

        private static string LineSpacing1 = "\\sl240";
        private static string LineSpacing1_15 = "\\sl276";
        private static string LineSpacing1_5 = "\\sl360";
        private static string LineSpacing2 = "\\sl480";
        private static string LineSpacingMultiplier1 = "\\slmult1";

        private static string FontStandard = "\\f0 ";
        private static string FontStandardEnd = "\\f ";
        private static string FontCode = "\\f1 ";

        private static string StandardParagraphStart = $"{ParagraphStart}{LineSpacing1_15}{LineSpacingMultiplier1}{FontStandard}{FontSizeDefault}";
        private static string StandardParagraphEnd = $"{ParagraphEnd}";
        private static string EmptyParagraph = $"{ParagraphStart}{LineSpacing1_15}{LineSpacingMultiplier1}{ParagraphEnd}";
        private static string CodeParagraphStart = $"{ParagraphStart}{LineSpacing1_15}{LineSpacingMultiplier1}{FontCode}{FontSizeStandardCode}";
        private static string CodeParagraphEnd = $"{ParagraphEnd}";

        private static string Bold = "\\b ";
        private static string BoldEnd = "\\b0 ";
        private static string Italic = "\\i ";
        private static string ItalicEnd = "\\i0 ";
        private static string Underline = "\\ul ";
        private static string UnderlineEnd = "\\ul0 ";
        private static string StrikeThrough = "\\strike ";
        private static string StrikeThroughEnd = "\\strike0 ";

        private static string LineBreak = "\\line";

        private static string UnsupportedBlock(string text) => $"/* Unsupported block type: {text} *";

        private static int LastIndentLevel = 0;

        public static bool UseEmptyParagraph = true;

        public static string ConvertText(string markdown)
        {
            return Markdig.Markdown.ToPlainText(markdown);
        }
        public static string ConvertRtf(string markdown, bool includeHeaderAndFooter = true)
        {
            // 1) Parse Markdown into a syntax tree
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var document = Markdig.Markdown.Parse(markdown, pipeline);

            // 2) Start building the RTF string
            var rtfBuilder = new StringBuilder();

            // Basic RTF header
            if (includeHeaderAndFooter is true)
            {
                rtfBuilder.AppendLine(Header1);
                rtfBuilder.AppendLine(Header2);
            }

            // 3) Walk the document blocks
            foreach (var block in document)
            {
                bool byPass = false;
                switch (block)
                {
                    case HeadingBlock headingBlock:
                        rtfBuilder.Append($"{StandardParagraphStart}");
                        ConvertHeadingBlock(rtfBuilder, headingBlock);
                        rtfBuilder.AppendLine($"{StandardParagraphEnd}");
                        break;

                    case ParagraphBlock paragraphBlock:
                        rtfBuilder.Append($"{StandardParagraphStart}");
                        ConvertParagraphBlock(rtfBuilder, paragraphBlock);
                        rtfBuilder.AppendLine($"{StandardParagraphEnd}");
                        break;

                    case QuoteBlock quoteBlock:
                        rtfBuilder.AppendLine($"{ParagraphStart}");
                        rtfBuilder.Append($"{StandardParagraphStart}");
                        ConvertQuoteBlock(rtfBuilder, quoteBlock);
                        rtfBuilder.AppendLine($"{StandardParagraphEnd}");
                        break;

                    case ListBlock listBlock:
                        //rtfBuilder.AppendLine($"{AlternateParagraph}");
                        //rtfBuilder.Append($"{ParagraphStart}");
                        LastIndentLevel = -1;
                        ConvertListBlock(rtfBuilder, listBlock);
                        //rtfBuilder.AppendLine($"{StandardParagraphEnd}");
                        //rtfBuilder.AppendLine($"{AlternateParagraph}");
                        break;

                    case ThematicBreakBlock thematicBreakBlock:
                        rtfBuilder.Append($"{StandardParagraphStart}");
                        ConvertThematicBreakBlock(rtfBuilder, thematicBreakBlock);
                        rtfBuilder.AppendLine($"{StandardParagraphEnd}");
                        break;

                    case CodeBlock codeBlock:
                        rtfBuilder.Append($"{CodeParagraphStart}");
                        ConvertCodeBlock(rtfBuilder, codeBlock);
                        rtfBuilder.AppendLine($"{StandardParagraphEnd}");
                        byPass = true;
                        break;
                  
                    case HtmlBlock htmlBlock:
                        rtfBuilder.Append($"{CodeParagraphStart}");
                        ConvertHtmlBlock(rtfBuilder, htmlBlock);
                        rtfBuilder.AppendLine($"{StandardParagraphEnd}");
                        byPass = true;
                        break;

                    case LinkReferenceDefinitionGroup linkReferenceDefinitionGroup:
                        break;

                    case Table tableBlock:
                        ConvertTableBlock(rtfBuilder, tableBlock);
                        break;

                    case BlankLineBlock blankLineBlock:

                        break;

                    //case MathBlock mathBlock:
                    //    break;
                    //case FootnoteBlock footnoteBlock:
                    //    break;

                    default:
                        // Default handling for unrecognized block types
                        rtfBuilder.Append($"{StandardParagraphStart}");
                        rtfBuilder.Append(UnsupportedBlock(block.GetType().Name));
                        rtfBuilder.Append($"{StandardParagraphEnd}");
                        break;
                }

              
                if (UseEmptyParagraph is true && byPass is false )
                {
                    rtfBuilder.AppendLine($"{EmptyParagraph}");
                }
                
            }

            // Close the RTF document
            if (includeHeaderAndFooter is true )
            {
                rtfBuilder.AppendLine(Footer);
            }

            return rtfBuilder.ToString();
        }


        #region Blocks

        private static void ConvertHeadingBlock(StringBuilder rtf, HeadingBlock headingBlock)
        {
            // Map heading level to font size (completely arbitrary example)
            // RTF uses \fsN in half-points (e.g., \fs32 => 16pt)
            int[] headingSizes = HeadingSizes;
            int headingLevel = headingBlock.Level; // 1-based

            // Get a font size for the heading level, clamp if needed
            int fontSize = headingSizes[Math.Min(headingLevel, headingSizes.Length) - 1];

            rtf.Append($"{Bold}{FontSizeControlChar}{fontSize} ");

            // Heading text:
            ConvertInline(rtf, headingBlock.Inline);

            // End bold, new line
            rtf.AppendLine($"{BoldEnd}{FontSizeDefault} ");
        }

        private static void ConvertParagraphBlock(StringBuilder rtf, ParagraphBlock paragraphBlock)
        {
            // Convert inlines inside this paragraph
            ConvertInline(rtf, paragraphBlock.Inline);
        }

        private static void ConvertQuoteBlock(StringBuilder rtf, QuoteBlock quoteBlock)
        {
            rtf.Append(@"\fi-360\li1080\tx360\ ");

            // Process each block inside the quote block
            foreach (var block in quoteBlock)
            {
                switch (block)
                {
                    case HeadingBlock headingBlock:
                        ConvertHeadingBlock(rtf, headingBlock);
                        break;
                    case ParagraphBlock paragraphBlock:
                        ConvertParagraphBlock(rtf, paragraphBlock);
                        break;
                    case QuoteBlock nestedQuoteBlock:
                        ConvertQuoteBlock(rtf, nestedQuoteBlock);
                        break;
                    case ListBlock listBlock:
                        LastIndentLevel = -1;
                        ConvertListBlock(rtf, listBlock);
                        break;
                    case CodeBlock codeBlock:
                        ConvertCodeBlock(rtf, codeBlock);
                        break;
                    case ThematicBreakBlock thematicBreakBlock:
                        ConvertThematicBreakBlock(rtf, thematicBreakBlock);
                        break;
                    case HtmlBlock htmlBlock:
                        ConvertHtmlBlock(rtf, htmlBlock);
                        break;
                    case Table tableBlock:
                        ConvertTableBlock(rtf, tableBlock);
                        break;
                    case LinkReferenceDefinitionGroup linkReferenceDefinitionGroup:
                        break;
                    default:
                        // Add a comment for debugging unknown block types
                        rtf.Append($"{ParagraphStart}\\sl276\\slmult1\\f0{FontSizeDefault} "); // Standard paragraph formatting
                        rtf.Append(UnsupportedBlock(block.GetType().Name));
                        rtf.AppendLine($"{ParagraphEnd}"); // End paragraph
                        break;
                }
            }

            // Add an extra paragraph break after the quote block
            //todo what is this???
            //rtf.AppendLine(@"\par");
        }

        private static void ConvertListBlock(StringBuilder rtf, ListBlock listBlock, int level = 0)
        {
            /*
             * Ordered
                \pard{\pntext\f0 1.\tab}{\*\pn\pnlvlbody\pnf0\pnindent0\pnstart1\pndec{\pntxta.}}
                \fi-360\li720\sl276\slmult1 First item \par
                {\pntext\f0 2.\tab}Second item \par
                {\pntext\f0 3.\tab}Third item \par
                {\pntext\f0 4.\tab}Fourth item \par
             * Unordered
                \pard{\pntext\f3\'B7\tab}{\*\pn\pnlvlblt\pnf3\pnindent0{\pntxtb\'B7}}\fi-360\li720\sl276\slmult1 First item \par
                {\pntext\f0\'B7\tab}Second item \par
                {\pntext\f0\'B7\tab}Third item \par
                {\pntext\f0\'B7\tab}Fourth item \par
             */
            int levelIndent = 720 + (level * 360);
            bool isOrdered = listBlock.IsOrdered;

            foreach (ListItemBlock item in listBlock)
            {
                bool SameLevel = LastIndentLevel == level;
                string start = "";
                string end = $"{ParagraphEnd}";
                if (isOrdered is true)
                {
                    // e.g., "1. ", "2. ", etc.
                    start += $"{{\\pntext\\f0 {item.Order}.\\tab}}";

                    if (SameLevel is false)
                    {
                        start += $"{{\\*\\pn\\pnlvlbody\\pnf0\\pnindent0\\pnstart1\\pndec{{\\pntxta.}}}}";
                        start += Environment.NewLine;
                        start += $"\\fi-360\\li{levelIndent}\\sl276\\slmult1";
                    }
                }
                else
                {
                    // or just a bullet symbol, e.g. \bullet
                    start += $"{{\\pntext\\f0\\'B7\\tab}}";

                    if (SameLevel is false)
                    {
                        start += $"{{\\*\\pn\\pnlvlblt\\pnf3\\pnindent0{{\\pntxtb\\'B7}}}}\\fi-360\\li{levelIndent}\\sl276\\slmult1";
                    }
                }

                // Convert each sub-block inside this list item
                foreach (var subItem in item)
                {
                    if (subItem is HeadingBlock heading)
                    {
                        rtf.Append(start);

                        ConvertHeadingBlock(rtf, heading);

                        rtf.Append(end);
                        LastIndentLevel = level;
                    }
                    else if (subItem is ParagraphBlock subParagraph)
                    {
                        rtf.Append(start);

                        ConvertParagraphBlock(rtf, subParagraph);

                        rtf.Append(end);

                        LastIndentLevel = level;
                    }
                    else if (subItem is QuoteBlock quote)
                    {
                        rtf.Append(start);

                        ConvertQuoteBlock(rtf, quote);

                        rtf.Append(end);

                        LastIndentLevel = level;
                    }
                    else if (subItem is CodeBlock code)
                    {
                        rtf.Append(start);

                        ConvertCodeBlock(rtf, code);

                        rtf.Append(end);

                        LastIndentLevel = level;
                    }
                    else if (subItem is ThematicBreakBlock thematicBreak)
                    {
                        rtf.Append(start);

                        ConvertThematicBreakBlock(rtf, thematicBreak);

                        rtf.Append(end);

                        LastIndentLevel = level;
                    }
                    else if (subItem is HtmlBlock html)
                    {
                        rtf.Append(start);

                        ConvertHtmlBlock(rtf, html);

                        rtf.Append(end);

                        LastIndentLevel = level;
                    }
                    else if (subItem is Table table)
                    {
                        rtf.Append(start);

                        ConvertTableBlock(rtf, table);

                        rtf.Append(end);

                        LastIndentLevel = level;
                    }
                    else if (subItem is LinkReferenceDefinitionGroup linkGroup)
                    {

                    }

                    // End list item


                    if (subItem is ListBlock nestedList)
                    {
                        ConvertListBlock(rtf, nestedList, level + 1);
                    }

                }

            }
        }

        private static void ConvertThematicBreakBlock(StringBuilder rtf, ThematicBreakBlock thematicBreakBlock)
        {
            //***
            //ThematicBreak (Node) represents a thematic break, such as a scene change in a story, a transition to another topic, or a new document.

            rtf.AppendLine($"{LineSpacing1_15} {LineSpacingMultiplier1} \\qc {Bold} {FontSizeH5} * * *\\cf1 {BoldEnd}");

        }

        private static void ConvertCodeBlock(StringBuilder rtf, CodeBlock codeBlock)
        {
            // Start code block with specific formatting
            //rtf.Append($"{LineSpacing1_15} {LineSpacingMultiplier1} {FontCode} {FontSizeStandardCode}");  // Using f1 for monospace font

            // Check if it's a fenced code block and handle language info
            if (codeBlock is FencedCodeBlock fencedCodeBlock && !string.IsNullOrEmpty(fencedCodeBlock.Info))
            {
                string language = fencedCodeBlock.Info.Trim();
                rtf.AppendLine($"{Bold}Language: {language} {BoldEnd} {LineBreak}");
            }

            // Process each line of code
            var lines = codeBlock.Lines.Lines;
            if (lines is not null)
            {
                var lenght = lines.Length;

                int lastValidLineIndex = lenght - 1;
                //find last valid line.
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    var line = lines[i];
                    string content = line.ToString();
                    if (string.IsNullOrWhiteSpace(content) == false)
                    {
                        lastValidLineIndex = i;
                        break;
                    }
                }

                for (int i = 0; i <= lastValidLineIndex; i++)
                {
                    var line = lines[i];
                    string content = line.ToString();

                    // Escape RTF special characters (backslash, curly braces)
                    content = content.Replace(@"\", @"\\").Replace("{", @"\{").Replace("}", @"\}");

                    // Handle tab characters
                    content = content.Replace("\t", @"\tab ");

                    // Add the line to RTF
                    rtf.Append(content);

                    // Add line break for all except the last line
                    if (i < lines.Length - 1)
                    {
                        rtf.AppendLine($"{LineBreak}");
                    }
                }
            }

        }
         
        private static void ConvertHtmlBlock(StringBuilder rtf, HtmlBlock htmlBlock)
        {
            // Start HTML block with specific formatting (similar to code block)
            //rtf.Append($"{LineSpacing1_15} {LineSpacingMultiplier1} {FontCode} {FontSizeStandardCode}");  // Using f1 for monospace font

            // Add a label to indicate this is HTML content
            rtf.AppendLine($"{Bold}HTML:{BoldEnd} {LineBreak}");

            // Process each line of HTML
            var lines = htmlBlock.Lines.Lines;
            if (lines is not null)
            {
                var length = lines.Length;

                int lastValidLineIndex = length - 1;
                // Find last valid line
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    var line = lines[i];
                    string content = line.ToString();
                    if (string.IsNullOrWhiteSpace(content) == false)
                    {
                        lastValidLineIndex = i;
                        break;
                    }
                }

                for (int i = 0; i <= lastValidLineIndex; i++)
                {
                    var line = lines[i];
                    string content = line.ToString();

                    // Escape RTF special characters (backslash, curly braces)
                    content = content.Replace(@"\", @"\\").Replace("{", @"\{").Replace("}", @"\}");

                    // Handle tab characters
                    content = content.Replace("\t", @"\tab ");

                    // Add the line to RTF
                    rtf.Append(content);

                    // Add line break for all except the last line
                    if (i < lines.Length - 1)
                    {
                        rtf.AppendLine($"{LineBreak}");
                    }
                }
            }
        }

        private static int GetMaxColumnCount(Table tableBlock)
        {
            int maxColumns = 0;
            foreach (var rowObj in tableBlock)
            {
                if (rowObj is TableRow row)
                {
                    maxColumns = Math.Max(maxColumns, row.Count);
                }
            }
            return maxColumns;
        }
        private static void ConvertTableBlock(StringBuilder rtf, Table tableBlock)
        {
            /*
                \pard\fs22
                \trowd
                \cell 1,1\cell 1,2\cell\row
                \trowd
                \cell 2,1\cell 2,2\cell\row
                \par

                \trowd\trgaph10\trpaddl10\trpaddr10\trpaddfl3\trpaddfr3
                \cellx2000\cellx4000\cellx6000 
                \pard\intbl\nowidctlpar\sl240\slmult1 Column 1\cell Column 2\cell Column 3\cell\row\trowd\trgaph10\trpaddl10\trpaddr10\trpaddfl3\trpaddfr3
                \cellx2000\cellx4000\cellx6000 
                \pard\intbl\nowidctlpar\sl240\slmult1 Data 1\cell Data 2\cell Data 3\cell\row\trowd\trgaph10\trpaddl10\trpaddr10\trpaddfl3\trpaddfr3
                \cellx2000\cellx4000\cellx6000 
                \pard\intbl\nowidctlpar\sl240\slmult1 Data 4\cell Data 5\cell Data 6\cell\row 
            */
            // Start the RTF table structure
            rtf.AppendLine($"{ParagraphEnd}");

            int columnCount = GetMaxColumnCount(tableBlock);
            int rowWidth = 2000;

            var total = 5 * 2000;
            rowWidth = total / columnCount;


            // Iterate through each row in the table
            foreach (var rowObj in tableBlock)
            {
                if (rowObj is TableRow row)
                {
                    // Start the row

                    rtf.AppendLine(@"\trowd\trgaph10\trpaddl10\trpaddr10\trpaddfl3\trpaddfr3");
                    var lengths = row.Count;
                    for (int i = 1; i <= lengths; i++)
                    {
                        rtf.Append(@"\cellx");
                        int offset = i * rowWidth;
                        rtf.Append(offset);
                    }
                    rtf.Append(" ");
                    rtf.AppendLine();

                    //rtf.AppendLine(@"\cellx2000\cellx4000\cellx6000 ");
                    rtf.Append(@"\pard\intbl\nowidctlpar\sl240\slmult1");

                    // Iterate through each cell in the row
                    foreach (var collumObj in row)
                    {
                        if (collumObj is TableCell cell)
                        {
                            rtf.Append(@" ");
                            foreach (var item in cell)
                            {
                                switch (item)
                                {
                                    case HeadingBlock headingBlock:
                                        ConvertInline(rtf, headingBlock.Inline);
                                        break;

                                    case ParagraphBlock paragraphBlock:
                                        ConvertInline(rtf, paragraphBlock.Inline);
                                        break;

                                    case QuoteBlock quoteBlock:
                                        ConvertQuoteBlock(rtf, quoteBlock);
                                        break;

                                    case ListBlock listBlock:
                                        ConvertListBlock(rtf, listBlock);
                                        break;

                                    case CodeBlock codeBlock:
                                        ConvertCodeBlock(rtf, codeBlock);
                                        break;

                                    case ThematicBreakBlock thematicBreakBlock:
                                        ConvertThematicBreakBlock(rtf, thematicBreakBlock);
                                        break;

                                    case HtmlBlock htmlBlock:
                                        ConvertHtmlBlock(rtf, htmlBlock);
                                        break;


                                    default:
                                        // Unhandled block type; extend as needed
                                        rtf.Append("Unsupported block type: ");
                                        rtf.Append(item.GetType().Name); // Insert the name of the unrecognized block type
                                        break;
                                }
                            }

                            rtf.Append(@"\cell");
                        }
                    }
                    rtf.Append(@"\row");

                }
            }
            rtf.AppendLine($" ");
        }
         
        #endregion

        #region Inline Handlers

        /// <summary>
        /// Recursively handles inlines (bold, italic, underline, etc.) in a Markdig Inline container.
        /// </summary>
        private static void ConvertInline(StringBuilder rtf, ContainerInline containerInline, string prefix = "")
        {
            foreach (var inline in containerInline)
            {
                switch (inline)
                {
                    case AutolinkInline autolinkInline:
                        string href = autolinkInline.IsEmail ? $"mailto:{autolinkInline.Url}" : autolinkInline.Url;

                        // Start the hyperlink field
                        rtf.Append(@"{\field{\*\fldinst{HYPERLINK ");
                        rtf.Append($"\"{EscapeRtf(href)}\"");
                        rtf.Append(@"}}{\fldrslt ");

                        // Add the visible text (the URL itself)
                        rtf.Append(@"\ul ");
                        rtf.Append(EscapeRtf(autolinkInline.Url));
                        rtf.Append(@"\ulnone");

                        // Close the field
                        rtf.Append(@"} }");
                        break;

                    case LinkInline linkInline:
                        // A link might show as underlined text + possibly a hidden URL
                        // This is just a simplistic representation
                        //{\field{\*\fldinst{HYPERLINK "http://www.example.com"}}{\fldrslt Click Here}}
                        if (false)
                        {
                            rtf.Append(@"\ul ");
                            rtf.Append(EscapeRtf(prefix));
                            rtf.Append(EscapeRtf(linkInline.Title ?? linkInline.Url));
                            rtf.Append(@"\ulnone ");
                        }
                        else
                        {
                            var title = "URL Link";
                            var url = "";

                            if (linkInline.Url is not null)
                            {
                                url = title = EscapeRtf(linkInline.Url);
                            }


                            if (linkInline.Title is not null && string.IsNullOrWhiteSpace(linkInline.Title) is false)
                            {
                                title = EscapeRtf(linkInline.Title);
                            }
                            else if (string.IsNullOrWhiteSpace(linkInline.Title) is true)
                            {
                                //    Inline child = linkInline.LastChild ;
                                //    title = EscapeRtf(linkInline.Url);
                            }
                            else
                            {
                                // do nothing

                            }
                            rtf.Append($"{{\\field{{\\*\\fldinst{{ HYPERLINK \"{EscapeRtf(url)}\"}} }} {{\\fldrslt {title}}} }}");
                        }
                        break;

                    case CodeInline codeInline:
                        // For code inline, you might do a monospace font or something else
                        rtf.Append($"{FontCode}"); // e.g., a monospace font
                        rtf.Append(EscapeRtf(codeInline.Content));
                        rtf.Append($"{FontStandard}");
                        break;

                    case DelimiterInline delimiterInline:
                        rtf.Append(EscapeRtf(delimiterInline.ToLiteral()));
                        break;

                    case EmphasisInline emphasisInline:
                        HandleEmphasis(rtf, emphasisInline);
                        break;

                    case HtmlEntityInline htmlEntityInline:
                        // Could try to interpret inline HTML, or just skip/escape
                        rtf.Append(EscapeRtf(htmlEntityInline.ToString()));
                        break;

                    case HtmlInline htmlInline:
                        // Could try to interpret inline HTML, or just skip/escape
                        rtf.Append(EscapeRtf(htmlInline.Tag));
                        break;

                    case LineBreakInline lineBreakInline:
                        // Soft line break or hard line break?
                        // For simplicity, just do a line break.
                        rtf.Append($"{LineBreak} {Environment.NewLine}");
                        break;
 
                    case TaskList taskList:
                        // Handle TaskList with the specific box characters
                        if (taskList.Checked)
                        {
                            // White Square Containing Black Small Square (U+25A3)
                            rtf.Append("\\u9635? ");
                        }
                        else
                        {
                            // White Square with Rounded Corners (U+25A2)
                            rtf.Append("\\u9634? ");
                        }
                        break;

                    case LiteralInline literalInline:
                        rtf.Append(EscapeRtf(literalInline.Content.ToString()));
                        break;

                    //case LinkReferenceDefinitionGroup linkReferenceDefinitionGroup:
                    //    break;

                    default:
                        // Not handled; no-op
                        rtf.Append(UnsupportedBlock(inline.GetType().Name));
                        break;
                }
            }
        }

        /// <summary>
        /// Handles emphasis inlines (e.g., *italic*, **bold**, ***bold+italic***, etc.).
        /// We also interpret underscores as underline in this example.
        /// </summary>
        private static void HandleEmphasis(StringBuilder rtf, EmphasisInline emphasisInline)
        {
            // Markdig uses DelimiterChar = '*' for bold/italic, '_' is also possible
            bool isItalic = (emphasisInline.DelimiterChar == '*' && emphasisInline.DelimiterCount == 1)
                            || (emphasisInline.DelimiterChar == '_' && emphasisInline.DelimiterCount == 1);

            bool isBold = (emphasisInline.DelimiterChar == '*' && emphasisInline.DelimiterCount == 2);
            bool isUnderline = (emphasisInline.DelimiterChar == '_' && emphasisInline.DelimiterCount == 2);
            bool isStrikeThrough = (emphasisInline.DelimiterChar == '~' && emphasisInline.DelimiterCount == 2);

            // For triple *** or ___, Markdig generally splits it into nested emphasis inlines
            // but you could handle combined styles if desired.

            // Start tags
            if (isBold) rtf.Append($"{Bold}");
            if (isItalic) rtf.Append($"{Italic}");
            if (isUnderline) rtf.Append($"{Underline}");
            if (isStrikeThrough) rtf.Append($"{StrikeThrough}");

            // Recursively process the content inside the emphasis
            ConvertInline(rtf, emphasisInline);

            // End tags (reverse order)
            if (isBold) rtf.Append($"{BoldEnd}");
            if (isItalic) rtf.Append($"{ItalicEnd}");
            if (isUnderline) rtf.Append($"{UnderlineEnd}");
            if (isStrikeThrough) rtf.Append($"{StrikeThroughEnd}");
           
        }

        /// <summary>
        /// RTF is sensitive to certain special characters. Escape them here.
        /// </summary>
        private static string EscapeRtf(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Replace backslash, curly braces
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '{':
                        sb.Append("\\{");
                        break;
                    case '}':
                        sb.Append("\\}");
                        break;
                    // Convert newline to \line or \par if desired, 
                    // but let's do it in the inline logic instead
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}