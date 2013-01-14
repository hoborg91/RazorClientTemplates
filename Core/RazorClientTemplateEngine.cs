using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc.Razor;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;

namespace RazorClientTemplates
{
    public class RazorClientTemplateEngine
    {
        public string RenderClientTemplate(string razorTemplate)
        {
            using(var writer = new StringWriter())
            using(var reader = new StringReader(razorTemplate))
            {
                RenderClientTemplate(reader, writer);
                return writer.GetStringBuilder().ToString();
            }
        }

        public virtual void RenderClientTemplate(TextReader razorTemplate, TextWriter output)
        {
            var host = new RazorEngineHost(new CSharpRazorCodeLanguage());
            var engine = new RazorTemplateEngine(host);

            var parserResults = engine.ParseTemplate(razorTemplate);

            if(parserResults.Success == false)
                // TODO: Less suck
                throw new RazorClientTemplateException("Template parse exception");

            RenderClientTemplate(parserResults.Document, output);
        }

        public virtual void RenderClientTemplate(Block document, TextWriter output)
        {
            output.Write("function (Model) { ");
            output.Write("var _buf = []; ");
            ParseSyntaxTreeNode(document, output);
            output.Write(" return _buf.join(''); };");
        }

        protected virtual void ParseSyntaxTreeNode(SyntaxTreeNode node, TextWriter output)
        {
            if (node == null) return;

            var blockNode = node as Block;
            if (blockNode != null)
            {
                if (VisitBlock(blockNode, output)) return;
            }

            var spanNode = node as Span;
            if (spanNode != null)
            {
                // Ignore the @ symbol - that's Razor's business
                if (spanNode.Kind == SpanKind.Transition)
                    return;

                // Explicitly ignore @model and @inherits spans as part
                // of the transition from static to dynamic typing
                if (spanNode.Kind == SpanKind.MetaCode) return;

                // Explicitly support these types of spans:
                if (VisitMarkupSpan(
                    spanNode,
                    output)) return;
                if (VisitCodeSpan(
                    spanNode,
                    output)) return;
            }

            // Emit a warning for any span that wasn't handled above
            Trace.WriteLine(string.Format("Ignoring {0}...", node));
        }

        protected virtual bool VisitBlock(Block block, TextWriter output)
        {
            if(block == null) return false;

            if (block.Type == BlockType.Helper || block.Type == BlockType.Directive)
                return false;

            foreach (var child in block.Children)
            {
                ParseSyntaxTreeNode(child, output);
            }

            return true;
        }

        protected virtual bool VisitMarkupSpan(Span markup, TextWriter output)
        {
            if(markup == null || markup.Kind != SpanKind.Markup) return false;

            var content = new StringBuilder(markup.Content);
            content.Replace("\"", "\\\"");
            content.Replace("'", "\\'");
            content.Replace("\r", "\\r");
            content.Replace("\n", "\\n");

            output.Write("_buf.push('");
            output.Write(content.ToString());
            output.Write("');");

            return true;
        }

        protected virtual bool VisitCodeSpan(Span code, TextWriter output)
        {
            if (code == null || code.Kind != SpanKind.Code) return false;

            if (code.Parent.Type==BlockType.Expression)
            {
                output.Write("_buf.push(");
                output.Write(code.Content);
                output.Write(");");
            }
            else
            {
                var codeContent = new StringBuilder(code.Content);
                codeContent.Replace("\r", string.Empty);
                codeContent.Replace("\n", string.Empty);

                var translatedCode = TranslateCodeBlock(codeContent.ToString());
                output.Write(translatedCode);
            }

            return true;
        }

        protected virtual string TranslateCodeBlock(string code)
        {
            var @foreach = Regex.Match(code, @"foreach *\( *(?<Type>[^ ]*) (?<Variable>[^ ]*) in (?<Enumerator>[^ )]*)\) *{");

            if (@foreach.Success == false)
                return code;

            var groups = @foreach.Groups;

            return string.Format("for(var __i=0; __i<{0}.length; __i++) {{ var {1} = {0}[__i]; ",
                                 groups["Enumerator"].Value, groups["Variable"].Value);
        }
    }
}
